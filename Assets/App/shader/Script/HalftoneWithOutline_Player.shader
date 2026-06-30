Shader "Custom/URP/HalftoneWithOutline_Player"
{
    Properties
    {
        [Header(Texture)]
        _MainTex  ("Texture", 2D) = "white" {}

        [Header(Normal Map)]
        _NormalMap      ("Normal Map", 2D)                        = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 2))          = 1.0
        // ノーマルマップによる凹凸陰影の強さ（ドットサイズには影響しない）
        _NormalShading  ("Normal Shading Intensity", Range(0, 2)) = 1.0

        [Header(PBR Maps)]
        _MetalnessMap   ("Metalness Map", 2D)            = "black" {}
        _RoughnessMap   ("Roughness Map", 2D)            = "white" {}
        _MetalnessScale ("Metalness Scale", Range(0, 1)) = 1.0
        _RoughnessScale ("Roughness Scale", Range(0, 1)) = 1.0

        [Header(Occlusion)]
        _OcclusionMap      ("Occlusion Map", 2D)               = "white" {}
        // AOは環境光（アンビエント）にのみ適用する
        _OcclusionStrength ("Occlusion Strength", Range(0, 1)) = 1.0

        [Header(Emission)]
        _EmissionMap         ("Emission Map", 2D)                 = "black" {}
        [HDR]
        _EmissionColor       ("Emission Color", Color)            = (0,0,0,1)
        _EmissionIntensity   ("Emission Intensity", Range(0, 10)) = 5.0
        _EmissionMaskMap     ("Emission Mask Map", 2D)            = "white" {}
        // マスクをスクロールさせて発光アニメーションを作る（_Time.yで乗算）
        _EmissionMaskScrollX ("Mask Scroll X", Float)             = 0.0
        _EmissionMaskScrollY ("Mask Scroll Y", Float)             = 0.0

        [Header(Rim Light)]
        _RimColor      ("Rim Color", Color)                  = (1,1,1,1)
        // リムは頂点法線ベースのNdotVで判定（ノーマルマップの干渉を防ぐ）
        _RimThreshold  ("Rim Threshold", Range(0, 1))        = 0.2
        _RimSmoothness ("Rim Smoothness", Range(0.001, 0.2)) = 0.05
        _RimIntensity  ("Rim Intensity", Range(0, 2))        = 1.0

        [Header(Halftone)]
        _DotFreq          ("Dot Frequency (dots per UV)", Float)        = 20.0
        // 影側（NdotL=0）のドットサイズ
        _DotMin           ("Dot Size (shadow areas)", Range(0.00, 5.0)) = 0.01
        // 光側（NdotL=1）のドットサイズ
        _DotMax           ("Dot Size (lit areas)",    Range(0.0, 2.0))  = 1.5
        _Angle            ("Grid Angle (deg)", Range(-90, 90))          = 45.0
        _DotThreshold     ("Dot Threshold",   Range(-1, 1))             = 0.0
        _DotSmoothness    ("Dot Smoothness",  Range(-0.5, 1))           = -0.2
        // カメラ正面を向いている面ほどドットを大きく（影っぽく）する強さ
        _ViewDotInfluence ("View Angle Dot Influence", Range(0, 1))     = 0.3

        [Header(BaseColor)]
        _BgColor ("Base Color", Color) = (1,1,1,1)
        _Color   ("Dot Color", Color)  = (0,0,0,1)

        [Header(Outline)]
        _OutlineColor    ("Outline Color", Color)              = (0,0,0,1)
        _OutlineWidth    ("Outline Width", Range(0, 10))       = 1.0
        // カメラがこの距離以内 → アウトライン最大幅（_OutlineWidthそのまま）
        _OutlineNearDist ("Outline Near Distance", Float)      = 5.0
        // カメラがこの距離以上 → アウトライン幅 × _OutlineMinScale
        _OutlineFarDist  ("Outline Far Distance", Float)       = 20.0
        // 遠距離でのアウトライン幅の最小倍率（0=完全に消える、0.5=半分）
        _OutlineMinScale ("Outline Min Scale", Range(0, 1))    = 0.2
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            // Geometryキューにすることで DepthNormals pass（SSAO・アウトライン検出）に参加できる
            "Queue"          = "Geometry"
        }

        // ══════════════════════════════════════════════════════════════════
        // Pass 1: Inverted Hull Outline
        //
        // UV2 にベイクした Smooth Normals を押し出し方向として使う。
        //   - 通常の法線はハードエッジ頂点で分裂しているためエッジに隙間が生じる
        //   - Smooth Normals は同一座標の法線を平均化しているので隙間が生じない
        //
        // クリップ空間で押し出すことでカメラ距離に関わらず画面上のピクセル幅が一定。
        // アスペクト比補正を行うことで非正方形画面でも正円のアウトラインになる。
        // さらにカメラ距離によるスケールを乗算して遠距離では幅を細くする。
        //
        // Cull Front で表面を非表示にし、膨らんだ裏面だけをアウトラインとして描画する。
        // Pass 2（HalftoneLit）が後から上書きすることで最終的なアウトラインが見える。
        // ══════════════════════════════════════════════════════════════════
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front  // 表面を非表示にして、膨らんだ裏面だけアウトラインとして見せる
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   vert_outline
            #pragma fragment frag_outline

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ── SRPBatcher 要件 ──────────────────────────────────────────
            // 全 Pass で CBUFFER の中身が完全に一致している必要がある
            // このPassで使わない変数も全て宣言する
            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _OutlineNearDist;
                float  _OutlineFarDist;
                float  _OutlineMinScale;
                float4 _MainTex_ST;
                float4 _Color;
                float4 _NormalMap_ST;
                float  _NormalStrength;
                float  _NormalShading;
                float4 _MetalnessMap_ST;
                float4 _RoughnessMap_ST;
                float  _MetalnessScale;
                float  _RoughnessScale;
                float4 _OcclusionMap_ST;
                float  _OcclusionStrength;
                float4 _EmissionMap_ST;
                float4 _EmissionColor;
                float  _EmissionIntensity;
                float4 _EmissionMaskMap_ST;
                float  _EmissionMaskScrollX;
                float  _EmissionMaskScrollY;
                float4 _RimColor;
                float  _RimThreshold;
                float  _RimSmoothness;
                float  _RimIntensity;
                float  _DotFreq;
                float  _DotMin;
                float  _DotMax;
                float  _Angle;
                float  _DotThreshold;
                float  _DotSmoothness;
                float  _ViewDotInfluence;
                float4 _BgColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                // UV2 にベイクした Smooth Normals（オブジェクト空間）
                // SmoothNormalBaker.cs で書き込まれる
                float3 smoothNormal : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 smoothNormal : TEXCOORD0; 
            };

            Varyings vert_outline(Attributes IN)
            {
                Varyings OUT;

                float3 outlineNormal;
                if (length(IN.smoothNormal) > 0.01)
                    outlineNormal = normalize(IN.smoothNormal);
                else
                    outlineNormal = IN.normalOS;

                float3 worldPos  = TransformObjectToWorld(IN.positionOS.xyz);
                float  camDist   = distance(worldPos, GetCameraPositionWS());
                float  distScale = lerp(
                    1.0,
                    _OutlineMinScale,
                    smoothstep(_OutlineNearDist, _OutlineFarDist, camDist)
                );

                float4 clipPos = TransformObjectToHClip(IN.positionOS.xyz);

                // オブジェクト空間 → ワールド空間
                float3 normalWS = TransformObjectToWorldNormal(outlineNormal, true);

                // ワールド空間 → ビュー空間（カメラ基準座標）
                // ビュー空間のXYがそのまま画面のXYに対応する
                float3 normalVS = mul((float3x3)UNITY_MATRIX_V, normalWS);

                // XYがゼロに近い場合の対策：
                // 3D正規化してからXYを取る（Z成分を含めてnormalizeすることで方向が安定する）
                float3 normalVS_n = normalize(normalVS);
                float  aspect   = _ScreenParams.x / _ScreenParams.y; 
                float2 snXY = float2(normalVS_n.x / aspect, -normalVS_n.y);

                // XYの長さが極端に小さい場合はZ方向（カメラ正面）なのでスキップ
                float xyLen = length(snXY);
                float2 offset = (xyLen > 0.01)
                    ? normalize(snXY) * _OutlineWidth * 0.01 * distScale
                    : float2(0, 0);

                clipPos.xy += offset * clipPos.w;

                OUT.positionHCS  = clipPos;
                OUT.smoothNormal = outlineNormal;
                return OUT;
            }

            float4 frag_outline(Varyings IN) : SV_Target
            {
                // アウトラインは単色塗りつぶし
                return _OutlineColor;

            }
            ENDHLSL
        }

        // ══════════════════════════════════════════════════════════════════
        // Pass 2: HalftoneLit（メイン描画）
        //
        // ライティング計算の流れ：
        //   1. メインライトの方向・色を取得
        //   2. 頂点法線ベースの NdotL → ドットサイズ計算
        //   3. ノーマルマップ法線ベースの NdotL → Burley拡散・GGXスペキュラー
        //   4. 環境光（SH）× AO → アンビエント
        //   5. リムライト（頂点NdotVのみ、ノーマルマップの干渉を防ぐ）
        //   6. Emission + スクロールマスク
        //   7. ハーフトーンドットのUV空間グリッド計算 → ベースカラーとドットカラーを合成
        // ══════════════════════════════════════════════════════════════════
        Pass
        {
            Name "HalftoneLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            // シャドウ・追加ライト用のマルチコンパイルキーワード
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // テクスチャ宣言（サンプラーとセット）
            TEXTURE2D(_MainTex);          SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap);        SAMPLER(sampler_NormalMap);
            TEXTURE2D(_MetalnessMap);     SAMPLER(sampler_MetalnessMap);
            TEXTURE2D(_RoughnessMap);     SAMPLER(sampler_RoughnessMap);
            TEXTURE2D(_OcclusionMap);     SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_EmissionMap);      SAMPLER(sampler_EmissionMap);
            TEXTURE2D(_EmissionMaskMap);  SAMPLER(sampler_EmissionMaskMap);

            // SRPBatcher 要件：Pass 1 の CBUFFER と完全に一致させる
            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _OutlineNearDist;
                float  _OutlineFarDist;
                float  _OutlineMinScale;
                float4 _MainTex_ST;
                float4 _Color;
                float4 _NormalMap_ST;
                float  _NormalStrength;
                float  _NormalShading;
                float4 _MetalnessMap_ST;
                float4 _RoughnessMap_ST;
                float  _MetalnessScale;
                float  _RoughnessScale;
                float4 _OcclusionMap_ST;
                float  _OcclusionStrength;
                float4 _EmissionMap_ST;
                float4 _EmissionColor;
                float  _EmissionIntensity;
                float4 _EmissionMaskMap_ST;
                float  _EmissionMaskScrollX;
                float  _EmissionMaskScrollY;
                float4 _RimColor;
                float  _RimThreshold;
                float  _RimSmoothness;
                float  _RimIntensity;
                float  _DotFreq;
                float  _DotMin;
                float  _DotMax;
                float  _Angle;
                float  _DotThreshold;
                float  _DotSmoothness;
                float  _ViewDotInfluence;
                float4 _BgColor;
            CBUFFER_END

            // 頂点シェーダー入力
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;    // TBN行列の構築に必要
                float2 uv         : TEXCOORD0;
            };

            // フラグメントシェーダー入力（頂点シェーダーの出力）
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;    // 頂点法線（ワールド空間）
                float3 tangentWS   : TEXCOORD2;    // 接線（TBN用）
                float3 bitangentWS : TEXCOORD3;    // 従法線（TBN用）
                float3 positionWS  : TEXCOORD4;    // ワールド座標（ライティング・リム計算用）
                float4 shadowCoord : TEXCOORD5;    // シャドウマップのサンプリング座標
            };

            // ── ユーティリティ関数 ─────────────────────────────────────

            // UV回転（ハーフトーングリッドの角度調整用）
            float2 Rot(float2 p, float deg)
            {
                float r = deg * (PI / 180.0);
                float s = sin(r), c = cos(r);
                return float2(c * p.x - s * p.y, s * p.x + c * p.y);
            }

            // GGXスペキュラー分布（D項 × Smith幾何減衰G項）
            // roughness が低いほど鋭いハイライト、高いほどぼんやりしたハイライト
            float GGX_Specular(float3 normal, float3 lightDir, float3 viewDir, float roughness)
            {
                float3 halfDir = normalize(lightDir + viewDir);
                float  NdotH   = saturate(dot(normal, halfDir));
                float  NdotL   = saturate(dot(normal, lightDir));
                float  NdotV   = saturate(dot(normal, viewDir));

                float alpha  = roughness * roughness;
                float alpha2 = alpha * alpha;

                // GGX法線分布関数（D項）
                float denom = (NdotH * NdotH) * (alpha2 - 1.0) + 1.0;
                float D     = alpha2 / (PI * denom * denom);

                // Smith幾何減衰（G項）：マイクロファセットの自己遮蔽
                float k  = (roughness + 1.0) * (roughness + 1.0) / 8.0;
                float GL = NdotL / (NdotL * (1.0 - k) + k);
                float GV = NdotV / (NdotV * (1.0 - k) + k);
                float G  = GL * GV;

                return (D * G) / max(4.0 * NdotL * NdotV, 0.001);
            }

            // Schlickフレネル近似（F項）
            // 視線が面と平行になるほど反射率が増す
            float3 FresnelSchlick(float3 F0, float3 viewDir, float3 halfDir)
            {
                float cosTheta = saturate(dot(viewDir, halfDir));
                return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
            }

            // Burleyディフューズ（Disney PBR）
            // 標準的なランバートより端に向かって明るくなる特性がある
            float BurleyDiffuse(float NdotL, float NdotV, float roughness)
            {
                float FD90 = 0.5 + 2.0 * roughness * NdotL * NdotL;
                float FdV  = 1.0 + (FD90 - 1.0) * pow(1.0 - NdotV, 5.0);
                float FdL  = 1.0 + (FD90 - 1.0) * pow(1.0 - NdotL, 5.0);
                return FdV * FdL;
            }

            // ── 頂点シェーダー ─────────────────────────────────────────
            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // URP組み込み関数でオブジェクト→クリップ空間・ワールド空間に変換
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                // TBN行列の各ベクトルをワールド空間に変換
                VertexNormalInputs   nrmInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionHCS  = posInputs.positionCS;
                OUT.positionWS   = posInputs.positionWS;
                OUT.normalWS     = nrmInputs.normalWS;
                OUT.tangentWS    = nrmInputs.tangentWS;
                OUT.bitangentWS  = nrmInputs.bitangentWS;
                OUT.uv           = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.shadowCoord  = GetShadowCoord(posInputs);
                return OUT;
            }

            // ── フラグメントシェーダー ─────────────────────────────────
            float4 frag(Varyings IN) : SV_Target
            {
                // ── テクスチャサンプル ────────────────────────────────
                float4 baseTex   = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float  metalness = SAMPLE_TEXTURE2D(_MetalnessMap, sampler_MetalnessMap,
                                       TRANSFORM_TEX(IN.uv, _MetalnessMap)).r * _MetalnessScale;
                float  roughness = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap,
                                       TRANSFORM_TEX(IN.uv, _RoughnessMap)).r * _RoughnessScale;
                roughness = max(roughness, 0.04);  // 0除算防止のための最小値

                // AO（環境光にのみ適用するためここで取得）
                float rawOcclusion = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap,
                                         TRANSFORM_TEX(IN.uv, _OcclusionMap)).r;
                float occlusion    = lerp(1.0, rawOcclusion, _OcclusionStrength);

                // Emission：マスクをスクロールさせてアニメーション
                float2 maskUV = TRANSFORM_TEX(IN.uv, _EmissionMaskMap)
                              + float2(_EmissionMaskScrollX, _EmissionMaskScrollY) * _Time.y;
                float  emMask  = SAMPLE_TEXTURE2D(_EmissionMaskMap, sampler_EmissionMaskMap, maskUV).r;
                float3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap,
                                      TRANSFORM_TEX(IN.uv, _EmissionMap)).rgb
                                * _EmissionColor.rgb * _EmissionIntensity * emMask;

                // ── 法線の2系統を構築 ─────────────────────────────────
                // 頂点法線：ドットサイズ計算・リムライトに使用（ノーマルマップの干渉を防ぐ）
                float3 vertexNormalWS = normalize(IN.normalWS);

                // ノーマルマップ法線：陰影・PBRライティングに使用
                float2 normalUV = TRANSFORM_TEX(IN.uv, _NormalMap);
                float3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, normalUV));
                normalTS.xy    *= _NormalStrength;
                normalTS.z      = sqrt(saturate(1.0 - dot(normalTS.xy, normalTS.xy)));

                // TBN行列でタンジェント空間 → ワールド空間に変換
                float3x3 TBN       = float3x3(normalize(IN.tangentWS), normalize(IN.bitangentWS), vertexNormalWS);
                float3 normalMapWS = normalize(mul(normalTS, TBN));

                float3 viewDir = normalize(GetCameraPositionWS() - IN.positionWS);

                // ── メインライトの取得 ────────────────────────────────
                Light  mainLight  = GetMainLight(IN.shadowCoord);
                float3 lightDir   = normalize(mainLight.direction);
                float3 lightColor = mainLight.color;

                // ── NdotL / NdotV の2系統 ─────────────────────────────
                // vertexNdotL → ドットサイズ・リムの計算に使用
                // normalNdotL → PBRライティングの計算に使用
                float vertexNdotL = dot(vertexNormalWS, lightDir);
                float vertexNdotV = saturate(dot(vertexNormalWS, viewDir));
                float normalNdotL = saturate(dot(normalMapWS, lightDir));
                float NdotV       = saturate(dot(normalMapWS, viewDir));

                // ── 追加ライト（ポイントライト・スポットライトなど）─────
                float3 additionalSpecular = float3(0, 0, 0);
                uint additionalLightCount = GetAdditionalLightsCount();
                for (uint i = 0u; i < additionalLightCount; i++)
                {
                    Light  addLight = GetAdditionalLight(i, IN.positionWS);
                    float3 addDir   = normalize(addLight.direction);
                    float  atten    = addLight.distanceAttenuation * addLight.shadowAttenuation;

                    // 追加ライトの寄与を累積
                    vertexNdotL += dot(vertexNormalWS, addDir) * atten;
                    normalNdotL += saturate(dot(normalMapWS, addDir)) * atten;

                    float  addSpec = GGX_Specular(normalMapWS, addDir, viewDir, roughness);
                    additionalSpecular += addLight.color * addSpec * atten;
                }
                vertexNdotL = saturate(vertexNdotL);
                normalNdotL = saturate(normalNdotL);

                // ── ノーマルマップによる凹凸陰影 ─────────────────────
                // 頂点NdotLとノーマルマップNdotLの差分をテクスチャ色に乗せる
                // ドットサイズは頂点法線で決まるので、法線マップは見た目だけに影響する
                float normalDiff    = saturate(dot(vertexNormalWS, lightDir)) - normalNdotL;
                float normalShading = 1.0 + clamp(normalDiff * _NormalShading, -1.0, 1.0);
                float3 shadedTex    = baseTex.rgb * normalShading;

                // ── 環境光（Spherical Harmonics）× AO ────────────────
                float3 ambient = SampleSH(normalMapWS) * occlusion;

                // ── Burleyディフューズ ────────────────────────────────
                float  burley = BurleyDiffuse(normalNdotL, NdotV, roughness);
                float3 kD     = (1.0 - metalness) * shadedTex * burley;

                // ── GGXスペキュラー（メイン + 追加ライト）────────────
                // F0：非金属は0.04固定、金属はアルベドカラーを使う
                float3 F0      = lerp(float3(0.04, 0.04, 0.04), baseTex.rgb, metalness);
                float3 halfDir = normalize(lightDir + viewDir);
                float3 F       = FresnelSchlick(F0, viewDir, halfDir);

                float  specularIntensity = GGX_Specular(normalMapWS, lightDir, viewDir, roughness);
                float3 specular          = lightColor * F * specularIntensity * normalNdotL;
                specular                += additionalSpecular * F;  // 追加ライトのスペキュラーを加算

                // ライティング合計（拡散 + スペキュラー + 環境光）
                float3 litColor = kD + specular + ambient * baseTex.rgb * (1.0 - metalness);

                // ── リムライト ────────────────────────────────────────
                // 頂点NdotVのみ使用（ノーマルマップNdotVを使うとリム範囲が広がりすぎる）
                // ライト方向も頂点法線で判定し、影側にはリムが出ないようにする
                float rim     = 1.0 - vertexNdotV;
                float rimMask = smoothstep(1.0 - _RimThreshold, 1.0 - _RimThreshold + _RimSmoothness, rim);
                rimMask      *= saturate(dot(vertexNormalWS, lightDir));  // 影側のリムを抑制
                litColor     += _RimColor.rgb * rimMask * _RimIntensity;

                // Emissionを加算（ライティングの影響を受けない自発光）
                litColor += emission;

                // ── ハーフトーンドット計算 ────────────────────────────
                // ドットの大きさをvertexNdotLで決める（影 → 大きい、光 → 小さい）
                // ViewDotInfluenceでカメラ正面方向の影響を加える
                float dotInput = saturate(vertexNdotL - vertexNdotV * _ViewDotInfluence);
                float t = smoothstep(
                    _DotThreshold - _DotSmoothness,
                    _DotThreshold + _DotSmoothness + 1.0,
                    dotInput
                );
                float dotSize = lerp(_DotMin, _DotMax, t);

                // UV空間でグリッドを作り、セル中心からの距離でドットマスクを生成
                // スクリーン空間ではなくUV空間で計算することでカメラ距離に依存しない
                float2 p    = Rot(IN.uv * _DotFreq, _Angle);  // グリッドを角度回転
                float2 cell = p - floor(p) - 0.5;             // セル内の相対座標（-0.5〜0.5）
                float  dist = length(cell);                    // セル中心からの距離
                float  mask = 1.0 - smoothstep(dotSize * 0.5 - 0.02, dotSize * 0.5 + 0.02, dist);
                mask = saturate(mask);

                // ── 最終カラー合成 ────────────────────────────────────
                // マスクが1 → ドットカラー、マスクが0 → ベースカラー
                float3 bg    = litColor * _BgColor.rgb;
                float3 dotC  = litColor * _Color.rgb;
                float3 rgb   = lerp(bg, dotC, mask);
                float  alpha = lerp(_BgColor.a, _Color.a, mask);

                return float4(rgb, alpha);
            }
            ENDHLSL
        }

        // ShadowCaster・DepthNormalsはURP/Litの実装を流用
        // DepthNormalsを維持することでSSAOとアウトライン検出の両方に対応できる
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }

    FallBack "Universal Render Pipeline/Lit"
}

