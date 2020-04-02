// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/FlatShader"
{
    Properties
    {
        // _MainTex ("Texture", 2D) = "white" {}
        _Albedo ("Albedo", Color) = (1, 1, 1)
        _WireframeColor ("Wireframe Colour", Color) = (0, 0, 0)
		_WireframeSmoothing ("Wireframe Smoothing", Range(0, 10)) = 1
		_WireframeThickness ("Wireframe Thickness", Range(0, 10)) = 1
    }
    SubShader
    {
        // No culling or depth
        // Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma target 4.0

            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geo

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;

                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float2 baryCoords : TEXCOORD3;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul (unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.uv;
                return o;
            }

            //sampler2D _MainTex;
            float3 _Albedo;

            float3 _WireframeColor;
            float _WireframeSmoothing;
            float _WireframeThickness;

            float3 frag (v2f i) : SV_Target
            {
                float minNormal = min(i.normal.x, min(i.normal.y, i.normal.z));
                float3 col = _Albedo * (i.normal.x * i.normal.y * i.normal.z * 0.5 + 0.5);
                
                float3 barys;
                barys.xy = i.baryCoords;
                barys.z = 1 - barys.x - barys.y;

                float3 deltas = fwidth(barys);
                float3 thickness = deltas * _WireframeThickness;
                float3 smoothing = deltas * _WireframeSmoothing;
                
                barys = smoothstep(thickness, thickness + smoothing, barys);

                float minBary = min(barys.x, min(barys.y, barys.z));
                //float delta = abs(ddx(minBary)) + abs(ddy(minBary));

                //minBary = smoothstep(0, delta, minBary);
                return lerp(_WireframeColor, col, minBary);
            }

            [maxvertexcount(3)]
            void geo(triangle v2f i[3], inout TriangleStream<v2f> stream) 
            {
                float3 p0 = i[0].worldPos.xyz;
                float3 p1 = i[1].worldPos.xyz;
                float3 p2 = i[2].worldPos.xyz;

                float3 triNormal = normalize(cross(p1-p0, p2 - p0));
                
                i[0].normal = triNormal;
                i[1].normal = triNormal;
                i[2].normal = triNormal;

                i[0].baryCoords = float2(1, 0);
                i[1].baryCoords = float2(0, 1);
                i[2].baryCoords = float2(0, 0);

                stream.Append(i[0]);
                stream.Append(i[1]);
                stream.Append(i[2]);
            }

            ENDCG
        }
    }
}
