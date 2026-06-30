Shader "Custom/URP/HalftoneLit"
{
    Properties
    {
        [Header(Texture)]
        _MainTex  ("Texture", 2D)     = "white" {}

        [Header(Normal Map)]
        _NormalMap      ("Normal Map", 2D)                    = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 2))      = 1.0
        _NormalShading  ("Normal Shading Intensity", Range(0, 2)) = 1.0

        [Header(PBR Maps)]
        _MetalnessMap   ("Metalness Map", 2D)              = "black" {}
        _RoughnessMap   ("Roughness Map", 2D)              = "white" {}
        _MetalnessScale ("Metalness Scale", Range(0, 1))   = 1.0
        _RoughnessScale ("Roughness Scale", Range(0, 1))   = 1.0

        [Header(Occlusion)]
        _OcclusionMap      ("Occlusion Map", 2D)               = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0, 1)) = 1.0

        [Header(Emission)]
        _EmissionMap         ("Emission Map", 2D)              = "black" {}
        [HDR]
        _EmissionColor       ("Emission Color", Color)         = (0,0,0,1)
        _EmissionIntensity   ("Emission Intensity", Range(0, 10)) = 5.0
        
        _EmissionMaskMap     ("Emission Mask Map", 2D)         = "white" {}
        _EmissionMaskScrollX ("Mask Scroll X", Float)          = 0.0
        _EmissionMaskScrollY ("Mask Scroll Y", Float)          = 0.0

        [Header(Rim Light)]
        _RimColor      ("Rim Color", Color)                  = (1,1,1,1)
        _RimThreshold  ("Rim Threshold", Range(0, 1))        = 0.2
        _RimSmoothness ("Rim Smoothness", Range(0.001, 0.2)) = 0.05
        _RimIntensity  ("Rim Intensity", Range(0, 2))        = 1.0

        [Header(Halftone)]
        _DotFreq          ("Dot Frequency (dots per UV)", Float)        = 20.0
        _DotMin           ("Dot Size (shadow areas)", Range(0.00, 5.0))  = 0.01
        _DotMax           ("Dot Size (lit areas)",    Range(0.0, 2.0))   = 1.5
        _Angle            ("Grid Angle (deg)", Range(-90, 90))           = 45.0
        _DotThreshold     ("Dot Threshold",   Range(-1, 1))              = 0.0
        _DotSmoothness    ("Dot Smoothness",  Range(-0.5, 1))            = -0.2
        _ViewDotInfluence ("View Angle Dot Influence", Range(0, 1))     = 0.3

        [Header(BaseColor)]
        _BgColor  ("Base Color", Color) = (1,1,1,1)
        _Color    ("Dot Color", Color)  = (0,0,0,1)

        [Header(Light Direction)]
        [Toggle(USE_CAMERA_DIRECTION)]
        _UseCameraDirection ("Use Camera Direction", Float)        = 0
        _CameraLightOffsetX ("Camera Light Offset X", Range(-1,1)) = 0
        _CameraLightOffsetY ("Camera Light Offset Y", Range(-1,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }

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

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma shader_feature USE_CAMERA_DIRECTION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);          SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap);        SAMPLER(sampler_NormalMap);
            TEXTURE2D(_MetalnessMap);     SAMPLER(sampler_MetalnessMap);
            TEXTURE2D(_RoughnessMap);     SAMPLER(sampler_RoughnessMap);
            TEXTURE2D(_OcclusionMap);     SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_EmissionMap);      SAMPLER(sampler_EmissionMap);
            TEXTURE2D(_EmissionMaskMap);  SAMPLER(sampler_EmissionMaskMap);

            CBUFFER_START(UnityPerMaterial)
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
                // ── 変更点②: SRPバッチ対応のため定数バッファに変数を追加 ──
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
                float  _CameraLightOffsetX;
                float  _CameraLightOffsetY;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 tangentWS   : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
                float3 positionWS  : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
            };

            float2 Rot(float2 p, float deg)
            {
                float r = deg * (PI / 180.0);
                float s = sin(r), c = cos(r);
                return float2(c * p.x - s * p.y, s * p.x + c * p.y);
            }

            // GGXスペキュラー（D × G）
            float GGX_Specular(float3 normal, float3 lightDir, float3 viewDir, float roughness)
            {
                float3 halfDir = normalize(lightDir + viewDir);
                float  NdotH   = saturate(dot(normal, halfDir));
                float  NdotL   = saturate(dot(normal, lightDir));
                float  NdotV   = saturate(dot(normal, viewDir));

                float alpha  = roughness * roughness;
                float alpha2 = alpha * alpha;
                float denom  = (NdotH * NdotH) * (alpha2 - 1.0) + 1.0;
                float D      = alpha2 / (PI * denom * denom);

                float k  = (roughness + 1.0) * (roughness + 1.0) / 8.0;
                float GL = NdotL / (NdotL * (1.0 - k) + k);
                float GV = NdotV / (NdotV * (1.0 - k) + k);
                float G  = GL * GV;

                return (D * G) / max(4.0 * NdotL * NdotV, 0.001);
            }

            // Schlickフレネル（F）
            float3 FresnelSchlick(float3 F0, float3 viewDir, float3 halfDir)
            {
                float cosTheta = saturate(dot(viewDir, halfDir));
                return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
            }

            // Burleyディフューズ（Disney PBR）
            float BurleyDiffuse(float NdotL, float NdotV, float roughness)
            {
                float FD90 = 0.5 + 2.0 * roughness * NdotL * NdotL;
                float FdV  = 1.0 + (FD90 - 1.0) * pow(1.0 - NdotV, 5.0);
                float FdL  = 1.0 + (FD90 - 1.0) * pow(1.0 - NdotL, 5.0);
                return FdV * FdL;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
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

            float4 frag(Varyings IN) : SV_Target
            {
                // ── テクスチャサンプル ────────────────────────────
                float4 baseTex   = SAMPLE_TEXTURE2D(_MainTex,      sampler_MainTex,      IN.uv);
                float  metalness = SAMPLE_TEXTURE2D(_MetalnessMap, sampler_MetalnessMap,
                                       TRANSFORM_TEX(IN.uv, _MetalnessMap)).r * _MetalnessScale;
                float  roughness = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap,
                                       TRANSFORM_TEX(IN.uv, _RoughnessMap)).r * _RoughnessScale;
                roughness        = max(roughness, 0.04);

                // Occlusion（環境光にのみ適用）
                float rawOcclusion = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap,
                                         TRANSFORM_TEX(IN.uv, _OcclusionMap)).r;
                float occlusion    = lerp(1.0, rawOcclusion, _OcclusionStrength);

                // ── Emission × Mask（スクロールあり）─────────────
                float2 maskUV  = TRANSFORM_TEX(IN.uv, _EmissionMaskMap)
                               + float2(_EmissionMaskScrollX, _EmissionMaskScrollY) * _Time.y;
                float  emMask  = SAMPLE_TEXTURE2D(_EmissionMaskMap, sampler_EmissionMaskMap, maskUV).r;
                
                // ── 変更点③: _EmissionIntensity を計算に掛け合わせる ──
                float3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap,
                                      TRANSFORM_TEX(IN.uv, _EmissionMap)).rgb
                                * _EmissionColor.rgb
                                * _EmissionIntensity
                                * emMask;

                float4 dotColor = _Color;

                // ── 頂点法線（ドットサイズ・リム専用） ───────────
                float3 vertexNormalWS = normalize(IN.normalWS);

                // ── ノーマルマップ法線（陰影・PBR専用） ──────────
                float2 normalUV = TRANSFORM_TEX(IN.uv, _NormalMap);
                float3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, normalUV));
                normalTS.xy    *= _NormalStrength;
                normalTS.z      = sqrt(saturate(1.0 - dot(normalTS.xy, normalTS.xy)));

                float3x3 TBN = float3x3(
                    normalize(IN.tangentWS),
                    normalize(IN.bitangentWS),
                    vertexNormalWS
                );
                float3 normalMapWS = normalize(mul(normalTS, TBN));

                float3 viewDir = normalize(GetCameraPositionWS() - IN.positionWS);

                // ── ライト方向取得 ────────────────────────────────
                float3 lightDir   = float3(0, 1, 0);
                float3 lightColor = float3(1, 1, 1);

                #if defined(USE_CAMERA_DIRECTION)
                    float3 camRight = normalize(UNITY_MATRIX_V[0].xyz);
                    float3 camUp    = normalize(UNITY_MATRIX_V[1].xyz);
                    lightDir = normalize(
                        viewDir
                        + camRight * _CameraLightOffsetX
                        + camUp    * _CameraLightOffsetY
                    );
                #else
                    Light mainLight = GetMainLight(IN.shadowCoord);
                    lightDir   = normalize(mainLight.direction);
                    lightColor = mainLight.color;
                #endif

                // ── NdotL / NdotV 2系統 ───────────────────────────
                float vertexNdotL = dot(vertexNormalWS, lightDir);
                float vertexNdotV = saturate(dot(vertexNormalWS, viewDir));
                float normalNdotL = saturate(dot(normalMapWS, lightDir));
                float NdotV       = saturate(dot(normalMapWS, viewDir));

                // 追加ライト
                float3 additionalSpecular = float3(0, 0, 0);
                #if !defined(USE_CAMERA_DIRECTION)
                    uint additionalLightCount = GetAdditionalLightsCount();
                    for (uint i = 0u; i < additionalLightCount; i++)
                    {
                        Light  addLight = GetAdditionalLight(i, IN.positionWS);
                        float3 addDir   = normalize(addLight.direction);
                        float  atten    = addLight.distanceAttenuation * addLight.shadowAttenuation;

                        vertexNdotL += dot(vertexNormalWS, addDir) * atten;
                        normalNdotL += saturate(dot(normalMapWS, addDir)) * atten;

                        float  addSpec = GGX_Specular(normalMapWS, addDir, viewDir, roughness);
                        additionalSpecular += addLight.color * addSpec * atten;
                    }
                    vertexNdotL = saturate(vertexNdotL);
                    normalNdotL = saturate(normalNdotL);
                #endif

                // ── ノーマルマップ差分でテクスチャ陰影を計算 ─────────
                float normalDiff    = saturate(dot(vertexNormalWS, lightDir)) - normalNdotL;
                float normalShading = 1.0 + clamp(normalDiff * _NormalShading, -1.0, 1.0);
                float3 shadedTex    = baseTex.rgb * normalShading;

                // ── 環境光 × Occlusion ────────────────────────────
                float3 ambient = SampleSH(normalMapWS) * occlusion;

                // ── Burleyディフューズ ────────────────────────────
                float burley = BurleyDiffuse(normalNdotL, NdotV, roughness);
                float3 kD    = (1.0 - metalness) * shadedTex * burley;

                // ── PBRスペキュラー ───────────────────────────────
                float3 F0      = lerp(float3(0.04, 0.04, 0.04), baseTex.rgb, metalness);
                float3 halfDir = normalize(lightDir + viewDir);
                float3 F       = FresnelSchlick(F0, viewDir, halfDir);

                float  specularIntensity = GGX_Specular(normalMapWS, lightDir, viewDir, roughness);
                float3 specular          = lightColor * F * specularIntensity * normalNdotL;
                specular                += additionalSpecular * F;

                float3 litColor = kD + specular + ambient * baseTex.rgb * (1.0 - metalness);

                // ── リムライト ────────────────────────────────────
                float rim     = 1.0 - vertexNdotV;
                float rimMask = smoothstep(1.0 - _RimThreshold,
                                           1.0 - _RimThreshold + _RimSmoothness, rim);
                rimMask      *= saturate(dot(vertexNormalWS, lightDir));
                litColor     += _RimColor.rgb * rimMask * _RimIntensity;

                // Emission加算（マスク適用済み）
                litColor += emission;

                // ── ドットサイズ計算 ──────────────────────────────
                float dotInput = saturate(vertexNdotL - vertexNdotV * _ViewDotInfluence);
                float t = smoothstep(
                    _DotThreshold - _DotSmoothness,
                    _DotThreshold + _DotSmoothness + 1.0,
                    dotInput
                );
                float dotSize = lerp(_DotMin, _DotMax, t);

                // ── グリッド計算 ──────────────────────────────────
                float2 p    = Rot(IN.uv * _DotFreq, _Angle);
                float2 cell = p - floor(p) - 0.5;

                float dist = length(cell);
                float mask = 1.0 - smoothstep(dotSize * 0.5 - 0.02,
                                              dotSize * 0.5 + 0.02,
                                              dist);
                mask = saturate(mask);

                // ── カラー合成 ────────────────────────────────────
                float3 bg   = litColor * _BgColor.rgb;
                float3 dotC = litColor * dotColor.rgb;
                float3 rgb   = lerp(bg, dotC, mask);

                float alpha = lerp(_BgColor.a, dotColor.a, mask);

                return float4(rgb, alpha);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }

    FallBack "Universal Render Pipeline/Lit"
}