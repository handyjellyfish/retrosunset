Shader "ImageEffects/Eighties"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _AberrationX ("AberrationX", Range(-0.05, 0.05)) = 0
        _AberrationY ("AberrationY", Range(-0.05, 0.05)) = 0
        _Desaturation ("Desaturation", Range(0.0, 1)) = 0
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;

            float _AberrationX;
            float _AberrationY;
            float _Desaturation;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed2 ab = fixed2(_AberrationX, _AberrationY);

                fixed4 col = tex2D(_MainTex, i.uv);
                float chromR = tex2D(_MainTex, i.uv + ab).r;
                float chromB = tex2D(_MainTex, i.uv - ab).b;
                fixed4 output = fixed4(chromR, col.g, chromB, 1);
                
                fixed lum = Luminance(output.rgb);
                output.rgb = lerp(output.rgb, fixed3(lum,lum,lum), _Desaturation);

                return output;
            }
            ENDCG
        }
    }
}
