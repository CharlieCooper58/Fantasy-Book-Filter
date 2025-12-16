Shader "Hidden/ColorClampShader"
{
    Properties
    {
        _BlitTexture ("Texture", 2D) = "white" {}
        _BlueNoiseTexture ("Blue Noise Texture", 2D) = "white"{}
        _RClamp("Red Clamp", int) = 16
        _GClamp("Green Clamp", int) = 16
        _BClamp("Blue Clamp", int) = 16

        _HueClamp("Hue Clamp", int) = 10
        _SatClamp("Sat Clamp", int) = 10
        _ValueClamp("Value Clamp", int) = 10
        _DitherSpread("Dither Spread", Float) = 0
        _HueShift("Hue Shift", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }

        Pass
        {
            Name "ColorClamp"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment fp

            #pragma shader_feature USE_SAME_CLAMP_FOR_ALL_CHANNELS
            #pragma shader_feature USE_HSV
            #pragma shader_feature PRESERVE_LOWER_VALUES
            #pragma shader_feature DO_DITHER

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"


            int _RClamp;
            int _GClamp;
            int _BClamp;
            int _HueClamp;
            int _SatClamp;
            int _ValueClamp;
            float _DitherSpread;
            float _HueShift;
            Texture2D _BlueNoiseTexture;

            static const int bayer2[2 * 2] = {
                0, 2,
                3, 1
            };

            static const int bayer4[4 * 4] = {
                0, 8, 2, 10,
                12, 4, 14, 6,
                3, 11, 1, 9,
                15, 7, 13, 5
            };

            static const int bayer8[8 * 8] = {
                0, 32, 8, 40, 2, 34, 10, 42,
                48, 16, 56, 24, 50, 18, 58, 26,
                12, 44,  4, 36, 14, 46,  6, 38,
                60, 28, 52, 20, 62, 30, 54, 22,
                3, 35, 11, 43,  1, 33,  9, 41,
                51, 19, 59, 27, 49, 17, 57, 25,
                15, 47,  7, 39, 13, 45,  5, 37,
                63, 31, 55, 23, 61, 29, 53, 21
            };

            float GetBayer2(int x, int y)
            {
                return float(bayer2[(x % 2) + (y % 2) * 2]) * (1.0f / 4.0f) - 0.5f;
            }

            float GetBayer4(int x, int y)
            {
                return float(bayer4[(x % 4) + (y % 4) * 4]) * (1.0f / 16.0f) - 0.5f;
            }

            float GetBayer8(int x, int y)
            {
                return float(bayer8[(x % 8) + (y % 8) * 8]) * (1.0f / 64.0f) - 0.5f;
            }

            float3 hsv2rgb(float3 input)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(input.xxx + K.xyz) * 6.0 - K.www);

                return input.z * lerp(K.xxx, saturate(p - K.xxx), input.y);
            }

            float3 rgb2hsv(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }
            float ClampToRange(float input, int clampAmount, float2 coord, float ditherAmount)
            {

                float cc = 1;
                float thresh = 0.5;
                #ifdef DO_DITHER
                    // the blue noise texture we're using is 256x256
                    coord = frac(coord/1024);
                    thresh += ditherAmount * (SAMPLE_TEXTURE2D(_BlueNoiseTexture, sampler_PointClamp, coord).r - 0.5);
                #endif
                #ifdef PRESERVE_LOWER_VALUES
                    if(input < 0.0001){
                        return input;
                    }
                    cc = clamp(exp2(ceil(-log2(clampAmount * input))), 1, 4096);
                #endif
                float prod = cc * clampAmount * input;
                float base = floor(prod);
                float rem = frac(prod);
                if(rem > thresh) base += 1;
                return base/(cc*clampAmount);
                //return floor(cc * clampAmount * input+thresh) / (cc * clampAmount);
                /*
                float scaledCol = color * (n_colors - 1);
                float roundedDown = floor(scaledCol);
                float remainder = frac(scaledCol);
                float thresh = 0.5 + _DitherSpread *
                                    (SAMPLE_TEXTURE2D(
                                        _BlueNoiseTexture,
                                        sampler_PointClamp,
                                        coord).r
                                        - 0.5);
                if(remainder > thresh) roundedDown += 1;
                return roundedDown/(n_colors - 1) */
            }
            float4 fp(Varyings i) : SV_Target
            {
                float3 c = SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointClamp, i.texcoord).rgb;
                float3 endColor;
                float2 coord = float2(i.texcoord.x * _BlitTexture_TexelSize.z, i.texcoord.y * _BlitTexture_TexelSize.w);

                #ifdef USE_SAME_CLAMP_FOR_ALL_CHANNELS
                    _GClamp = _BClamp = _RClamp;
                #endif
                #ifdef USE_HSV
                    c = rgb2hsv(c);
                    endColor[0] = frac(_HueShift + ClampToRange(c[0], _HueClamp, coord, 0));
                    endColor[1] = ClampToRange(c[1], _SatClamp, coord, _DitherSpread);
                    endColor[2] = ClampToRange(c[2], _ValueClamp,  coord, _DitherSpread);

                    return float4(hsv2rgb(endColor), 1);
                #else
                    endColor[0] = ClampToRange(c[0], _RClamp, coord, _DitherSpread);
                    endColor[1] = ClampToRange(c[1], _GClamp, coord, _DitherSpread);
                    endColor[2] = ClampToRange(c[2], _BClamp, coord, _DitherSpread);
                    return float4(endColor, 1);
                #endif
            }

            ENDHLSL
        }
    }
}
