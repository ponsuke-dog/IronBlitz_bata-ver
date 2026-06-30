Shader "Custom/OutlineTest"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "TestPass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.pos = TransformObjectToHClip(v.pos.xyz);
                o.uv  = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // å≥âÊëúÇ…ê‘Ç50%çáê¨ Å® âÊñ Ç™ê‘Ç≠Ç»ÇÍÇŒRendererFeatureÇÕìÆÇ¢ÇƒÇ¢ÇÈ
                half4 src = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                return lerp(src, half4(1,0,0,1), 0.5);
            }
            ENDHLSL
        }
    }
}
