Shader "Custom/CRTMonitor"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        [MainTexture] _BaseMap("Albedo", 2D) = "white"{}
        _EmissionColor("Emission", Color) = (0,0,0)
        _TintColor("Tint", Color) = (1,1,1,1)
        _TintStrength("TintStrength", Range(0,1)) = 0.5
        _Lines("Lines", Float) = 200
        _ScrollSpeed("ScrollSpeed", Range(-50.0, 50.0)) = 2
        _ZOffset("ZOffset", Range(0.0, 0.03)) = 0.005
        _BlockSize("BlockSize", Float) = 32
        _GlitchAmount("GlitchAmount", Range(0, 1)) = 0.1
        _GlitchFrequency("GlitchFrequency", Range(0.1, 2.0)) = 1
        _GlitchDuration("GlitchDuration", Range(0, 2)) = 0.5
    }

    SubShader
    {
        // SubShader Tags define when and under which conditions a SubShader block or
        // a pass is executed.
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes //appdata->Attributes
            {
                float4 positionOS   : POSITION;
                float4 tangentOS    : TANGENT;
                float3 normalOS     : NORMAL;
                float2 texcoord     : TEXCOORD0;
            };

            struct Varyings //v2f->Varyings
            {
                float4 positionHCS  : SV_POSITION; //HCSはHomogenousClippingSpaceの略
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                float3 normalOS     : TEXCOORD2;
                float3 normalWS     : TEXCOORD3;
                half4 tangentWS     : TEXCOORD4;
                half3 bitangentWS   : TEXCOORD5;

            };

            

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.uv = IN.texcoord;

                VertexNormalInputs tbn = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                OUT.normalWS = tbn.normalWS;
                OUT.tangentWS = float4(tbn.tangentWS, IN.tangentOS.w);
                OUT.bitangentWS = tbn.bitangentWS;
                // Returning the output.
                return OUT;
            }
            
            

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            float4 _TintColor;
            float _TintStrength;
            float _Lines;
            float _ScrollSpeed;
            float _ZOffset;
            float _BlockSize;
            float _GlitchAmount;
            float _GlitchFrequency;
            float _GlitchDuration;

            float random (float2 pt) {
                const float a = 12.9898;
                const float b = 78.233;
                const float c = 43758.543123;
                return frac(sin(dot(pt, float2(a, b))) * c );
            }

            // 2D Noise based on Morgan McGuire @morgan3d
            // https://www.shadertoy.com/view/4dS3Wd
            float noise (float2 st) {
                float2 i = floor(st);
                float2 f = frac(st);

                // Four corners in 2D of a tile
                float a = random(i);
                float b = random(i + float2(1.0, 0.0));
                float c = random(i + float2(0.0, 1.0));
                float d = random(i + float2(1.0, 1.0));

                // Smooth Interpolation

                // Cubic Hermine Curve.  Same as SmoothStep()
                float2 u = f*f*(3.0-2.0*f);
                // u = smoothstep(0.,1.,f);

                // Mix 4 coorners percentages
                return lerp(a, b, u.x) +
                        (c - a)* u.y * (1.0 - u.x) +
                        (d - b) * u.x * u.y;
            }

            // The fragment shader definition.fixed型はHLSLで使えないので変換
            half4 frag (Varyings IN) : SV_Target
            {
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float3x3 tbn = float3x3(IN.tangentWS.xyz, IN.bitangentWS, IN.normalWS);
                float3 viewDirTS = mul(tbn, viewDirWS);
                
                float glitchTime = floor(_Time.y / _GlitchFrequency);
                float glitchSeed = random(float2(glitchTime, 5.13));
                float localTime = frac(_Time.y / _GlitchFrequency);
                float glitchActive = step(0.3, glitchSeed) * step(localTime, _GlitchDuration);
                float2 block = floor(IN.uv * _BlockSize) / _BlockSize;
                float offsetU = (random(float2(block.y, glitchTime)) * 2.0 - 1.0) * _GlitchAmount * glitchActive;
                float2 offsetUV = IN.uv - viewDirTS.xy * _ZOffset;
                offsetUV.x += offsetU;
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, offsetUV);
                float2 glitchUV = offsetUV;
                float glitchR = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, glitchUV + float2(offsetU * 0.5, 0)).r;
                float glitchG = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, glitchUV).g;
                float glitchB = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, glitchUV + float2(offsetU * 0.5, 0)).b;

                float3 glitchColor = float3(glitchR, glitchG, glitchB);

                // Defining the color variable and returning it.
                float n = noise(offsetUV);

                float3 srcColor = lerp(tex.rgb * 2.0, tex.rgb, n);
                float3 grayScale = (srcColor.r + srcColor.g + srcColor.b) / 3.0;
                float3 tinted = grayScale * _TintColor.rgb;
                float3 tintedColor = lerp(srcColor, tinted, _TintStrength);

                float scanline = saturate(smoothstep(0.1, 0.2, frac(offsetUV.y * _Lines + _Time.y * _ScrollSpeed)) - 0.3);
                float3 finalColor = lerp(srcColor.rgb*0.5, tintedColor, scanline);
                float sweep = smoothstep(0.0, 0.6, sin(offsetUV.y * 4 + _Time.y * 3) * 0.5 + 0.5);
                finalColor *= lerp(0.5, 1.0, sweep);
                float freq = 640.0 + noise(offsetUV * 8.0) * 8.0;          
                float pixelLine = floor(frac(offsetUV.x * freq) * 3.0);
                float3 pixelColor =
                pixelLine == 0 ? float3(0.8, 0.0, 0.0):
                pixelLine == 1 ? float3(0.0, 0.8, 0.0):
                float3(0.0, 0.0, 0.8);

                return half4(finalColor + glitchColor - pixelColor, 1.0);
            }
            
            ENDHLSL
        }
    }
}
