Shader "Custom/Sun"
{
    Properties
    {
        // _MainTex ("Texture", 2D) = "white" {}
        _ColorTop ("Top Color", Color) = (0, 0, 0)
        _ColorBottom ("Bottom Colour", Color) = (0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // // make fog work
            // #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                //UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float2 unit : TEXCOORD1;
            };

            //sampler2D _MainTex;
            //float4 _MainTex_ST;
            fixed4 _ColorTop;
            fixed4 _ColorBottom;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul (unity_ObjectToWorld, v.vertex).xyz;
                o.unit = (v.vertex.xy);
                // UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // // sample the texture
                // fixed4 col = tex2D(_MainTex, i.uv);
                // // apply fog
                // UNITY_APPLY_FOG(i.fogCoord, col);
                clip(i.worldPos.y + 0.5);
                

                fixed4 col = lerp(_ColorBottom, _ColorTop, smoothstep(-0.08, 0.05, i.unit.y)); //_ColorTop * smoothstep(0.42, 0.52, i.unit.y) + _ColorBottom * smoothstep(0.42, 0.52, 1-i.unit.y);
                col.a = smoothstep(1, 0.2, pow(i.unit.x*i.unit.x + i.unit.y*i.unit.y, 0.5)*1.8);
                return col;
            }
            ENDCG
        }
    }
}
