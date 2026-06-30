Shader "Custom/PostEffectOutline"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)

        _Depth1   ("Depth dist 1",   Range(0,1)) = 0.6
        _Depth1_1 ("Depth dist v2",  Range(0,1)) = 0.6
        _Depth2   ("Depth dist 2",   Range(0,1)) = 0.5
        _Depth2_1 ("Depth dist v5",  Range(0,1)) = 0.5
        _Depth2_2 ("Depth dist v8",  Range(0,1)) = 0.5
        _Depth3   ("Depth dist 3",   Range(0,1)) = 0.5
        _Depth3_1 ("Depth dist v10", Range(0,1)) = 0.5
        _Depth3_2 ("Depth dist v13", Range(0,1)) = 0.4

        _Normal1     ("Normal dist 1",  Range(0,1)) = 0.8
        _Normal1_1   ("Normal dist v2", Range(0,1)) = 0.7
        _Normal2     ("Normal 1-v2",    Range(0,1)) = 0.5
        _Normal2_1   ("Normal v2-v2",   Range(0,1)) = 0.1
        _Normal2_2   ("Normal 1-2",     Range(0,1)) = 0.1
        _NormalCutOff("Normal CutOff",  Range(0,1)) = 0.04
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "OutlinePass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Blit.hlsl は使わず Core.hlsl のみ
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            // カメラ描画結果テクスチャ（Blitter が _MainTex にセットする）
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            float4 _OutlineColor;
            float _Depth1, _Depth1_1;
            float _Depth2, _Depth2_1, _Depth2_2;
            float _Depth3, _Depth3_1, _Depth3_2;
            float _Normal1, _Normal1_1;
            float _Normal2, _Normal2_1, _Normal2_2;
            float _NormalCutOff;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            float2 UVOff(float2 uv, float x, float y)
            {
                return uv + _MainTex_TexelSize.xy * float2(x, y);
            }

            float3 GetNormal(float2 uv)
            {
                return SampleSceneNormals(uv);
            }

            float GetDepth(float2 uv)
            {
                return SampleSceneDepth(uv);
            }

            // 輪郭エッジ
            void CompareNormal1(inout float n, float3 base, float2 uv1, float2 uv2, float mul)
            {
                float3 n1 = GetNormal(uv1);
                float3 n2 = GetNormal(uv2);
                float d1 = distance(base, n1);
                float d2 = distance(base, n2);
                float3 far = d1 > d2 ? n1 : n2;
                float nd = dot(n1, n2) < dot(base, far) ? max(d1, d2) : 0;
                n += smoothstep(_NormalCutOff, 1.0, nd * mul);
            }

            // 面境界エッジ
            void CompareNormal2(inout float n, float3 base, float2 uv1, float2 uv2, float mul)
            {
                float3 n1 = GetNormal(uv1);
                float3 n2 = GetNormal(uv2);
                float nd = dot(base, n1) > dot(base, n2) ? distance(n1, n2) : 0;
                n += smoothstep(_NormalCutOff, 1.0, nd * mul);
            }

            // 深度エッジ
            void CompareDepth(inout float d, float base, float2 uv, float mul)
            {
                float nd = GetDepth(uv);
                d += max((base - nd) / max(nd, 0.0001) * mul, 0);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv    = i.uv;
                float3 baseN = GetNormal(uv);
                float  baseD = GetDepth(uv);

                float dDiff = 0;
                float nDiff = 0;

                // --- 法線エッジ（輪郭） ---
                CompareNormal1(nDiff, baseN, UVOff(uv, 1,0),  UVOff(uv,-1, 0), _Normal1);
                CompareNormal1(nDiff, baseN, UVOff(uv, 0,1),  UVOff(uv, 0,-1), _Normal1);

                // --- 法線エッジ（面境界） ---
                CompareNormal2(nDiff, baseN, UVOff(uv, 1,0),  UVOff(uv,-1, 0), _Normal1_1);
                CompareNormal2(nDiff, baseN, UVOff(uv, 0,1),  UVOff(uv, 0,-1), _Normal1_1);
                CompareNormal2(nDiff, baseN, UVOff(uv, 1,0),  UVOff(uv,-1, 1), _Normal2);
                CompareNormal2(nDiff, baseN, UVOff(uv, 1,0),  UVOff(uv,-1,-1), _Normal2);
                CompareNormal2(nDiff, baseN, UVOff(uv, 0,1),  UVOff(uv, 1,-1), _Normal2);
                CompareNormal2(nDiff, baseN, UVOff(uv, 0,1),  UVOff(uv,-1,-1), _Normal2);
                CompareNormal2(nDiff, baseN, UVOff(uv, 1,1),  UVOff(uv,-1, 0), _Normal2);
                CompareNormal2(nDiff, baseN, UVOff(uv, 1,-1), UVOff(uv,-1, 0), _Normal2);
                CompareNormal2(nDiff, baseN, UVOff(uv, 1,1),  UVOff(uv, 0,-1), _Normal2);
                CompareNormal2(nDiff, baseN, UVOff(uv,-1,1),  UVOff(uv, 0,-1), _Normal2);
                CompareNormal2(nDiff, baseN, UVOff(uv, 1,1),  UVOff(uv,-1,-1), _Normal2_1);
                CompareNormal2(nDiff, baseN, UVOff(uv,-1,1),  UVOff(uv, 1,-1), _Normal2_1);
                CompareNormal2(nDiff, baseN, UVOff(uv, 1,0),  UVOff(uv,-2, 0), _Normal2_2);
                CompareNormal2(nDiff, baseN, UVOff(uv, 0,1),  UVOff(uv, 0,-2), _Normal2_2);
                CompareNormal2(nDiff, baseN, UVOff(uv,-1,0),  UVOff(uv, 2, 0), _Normal2_2);
                CompareNormal2(nDiff, baseN, UVOff(uv, 0,-1), UVOff(uv, 0, 2), _Normal2_2);

                // --- 深度エッジ 距離1 ---
                CompareDepth(dDiff, baseD, UVOff(uv, 1, 0), _Depth1);
                CompareDepth(dDiff, baseD, UVOff(uv,-1, 0), _Depth1);
                CompareDepth(dDiff, baseD, UVOff(uv, 0, 1), _Depth1);
                CompareDepth(dDiff, baseD, UVOff(uv, 0,-1), _Depth1);
                // 距離√2
                CompareDepth(dDiff, baseD, UVOff(uv, 1, 1), _Depth1_1);
                CompareDepth(dDiff, baseD, UVOff(uv, 1,-1), _Depth1_1);
                CompareDepth(dDiff, baseD, UVOff(uv,-1, 1), _Depth1_1);
                CompareDepth(dDiff, baseD, UVOff(uv,-1,-1), _Depth1_1);
                // 距離2
                CompareDepth(dDiff, baseD, UVOff(uv, 2, 0), _Depth2);
                CompareDepth(dDiff, baseD, UVOff(uv,-2, 0), _Depth2);
                CompareDepth(dDiff, baseD, UVOff(uv, 0, 2), _Depth2);
                CompareDepth(dDiff, baseD, UVOff(uv, 0,-2), _Depth2);
                // 距離√5
                CompareDepth(dDiff, baseD, UVOff(uv, 2, 1), _Depth2_1);
                CompareDepth(dDiff, baseD, UVOff(uv, 2,-1), _Depth2_1);
                CompareDepth(dDiff, baseD, UVOff(uv,-2, 1), _Depth2_1);
                CompareDepth(dDiff, baseD, UVOff(uv,-2,-1), _Depth2_1);
                CompareDepth(dDiff, baseD, UVOff(uv, 1, 2), _Depth2_1);
                CompareDepth(dDiff, baseD, UVOff(uv,-1, 2), _Depth2_1);
                CompareDepth(dDiff, baseD, UVOff(uv, 1,-2), _Depth2_1);
                CompareDepth(dDiff, baseD, UVOff(uv,-1,-2), _Depth2_1);
                // 距離√8
                CompareDepth(dDiff, baseD, UVOff(uv, 2, 2), _Depth2_2);
                CompareDepth(dDiff, baseD, UVOff(uv, 2,-2), _Depth2_2);
                CompareDepth(dDiff, baseD, UVOff(uv,-2, 2), _Depth2_2);
                CompareDepth(dDiff, baseD, UVOff(uv,-2,-2), _Depth2_2);
                // 距離3
                CompareDepth(dDiff, baseD, UVOff(uv, 3, 0), _Depth3);
                CompareDepth(dDiff, baseD, UVOff(uv,-3, 0), _Depth3);
                CompareDepth(dDiff, baseD, UVOff(uv, 0, 3), _Depth3);
                CompareDepth(dDiff, baseD, UVOff(uv, 0,-3), _Depth3);
                // 距離√10
                CompareDepth(dDiff, baseD, UVOff(uv, 1, 3), _Depth3_1);
                CompareDepth(dDiff, baseD, UVOff(uv,-1, 3), _Depth3_1);
                CompareDepth(dDiff, baseD, UVOff(uv, 1,-3), _Depth3_1);
                CompareDepth(dDiff, baseD, UVOff(uv,-1,-3), _Depth3_1);
                CompareDepth(dDiff, baseD, UVOff(uv, 3, 1), _Depth3_1);
                CompareDepth(dDiff, baseD, UVOff(uv, 3,-1), _Depth3_1);
                CompareDepth(dDiff, baseD, UVOff(uv,-3, 1), _Depth3_1);
                CompareDepth(dDiff, baseD, UVOff(uv,-3,-1), _Depth3_1);
                // 距離√13
                CompareDepth(dDiff, baseD, UVOff(uv, 2, 3), _Depth3_2);
                CompareDepth(dDiff, baseD, UVOff(uv,-2, 3), _Depth3_2);
                CompareDepth(dDiff, baseD, UVOff(uv, 2,-3), _Depth3_2);
                CompareDepth(dDiff, baseD, UVOff(uv,-2,-3), _Depth3_2);
                CompareDepth(dDiff, baseD, UVOff(uv, 3, 2), _Depth3_2);
                CompareDepth(dDiff, baseD, UVOff(uv, 3,-2), _Depth3_2);
                CompareDepth(dDiff, baseD, UVOff(uv,-3, 2), _Depth3_2);
                CompareDepth(dDiff, baseD, UVOff(uv,-3,-2), _Depth3_2);

                float outline = saturate(max(nDiff, dDiff));
                half4 src = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                return lerp(src, _OutlineColor, outline);
            }
            ENDHLSL
        }
    }
}
