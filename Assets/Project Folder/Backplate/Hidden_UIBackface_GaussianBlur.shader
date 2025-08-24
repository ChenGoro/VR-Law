Shader "Hidden/UIBackface/GaussianBlur"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _TexelSize ("Texel Size (1/w,1/h)", Vector) = (0,0,0,0)
        _Sigma ("Sigma", Float) = 2.0
        _Direction ("Direction (1,0 or 0,1)", Vector) = (1,0,0,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "GaussianSeparable"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_MainTex);
            SAMPLER(sampler_MainTex);

            float2 _TexelSize;   // set from C# to the blurred RT's 1/width,1/height
            float  _Sigma;       // gaussian sigma (controls strength)
            float2 _Direction;   // (1,0) for horizontal, (0,1) for vertical

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionHCS: SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            float gauss(float x, float s) { return exp(-0.5 * (x*x) / (s*s)); }

            half4 frag (Varyings i) : SV_Target
            {
                // 9 taps (radius = 4). This is a true 1D Gaussian convolution.
                const int R = 4;

                float2 stepUV = _Direction * _TexelSize;

                float  w0   = gauss(0.0, _Sigma);
                float3 col  = w0 * SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, i.uv).rgb;
                float  norm = w0;

                [unroll] for (int k = 1; k <= R; k++)
                {
                    float  wk = gauss((float)k, _Sigma);
                    float2 off = stepUV * (float)k;

                    float3 c1 = SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, i.uv + off).rgb;
                    float3 c2 = SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, i.uv - off).rgb;

                    col  += wk * (c1 + c2);
                    norm += 2.0 * wk;
                }

                col /= max(norm, 1e-6);
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
