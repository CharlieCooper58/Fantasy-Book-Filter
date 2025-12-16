Shader "Hidden/PointClampBlitShader" {
    Properties {
        _BlitTexture ("Texture", 2D) = "white" {}
    }

    SubShader {
        HLSLINCLUDE
    
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        // The Blit.hlsl file provides the vertex shader (Vert),
        // the input structure (Attributes), and the output structure (Varyings)
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        struct VertexData {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

        struct v2f {
            float2 uv : TEXCOORD0;
            float4 vertex : SV_POSITION;
        };

        v2f vp(VertexData v) {
            v2f o;
            o.vertex = float4(v.vertex.xy, 0, 1); // pass-through
            o.uv = v.uv;
            return o;
        };
        ENDHLSL

        Pass {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment fp
            

            float4 fp(Varyings i) : SV_Target {
                float4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointClamp, i.texcoord);

                return saturate(col);
            }
            ENDHLSL
        }
    }
}