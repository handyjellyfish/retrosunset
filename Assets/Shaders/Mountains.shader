Shader "Custom/Mountains"
{
    Properties
    {
        // _MainTex ("Texture", 2D) = "white" {}
        _Albedo ("Albedo", Color) = (1, 1, 1)
        _CubeMap ("Cubemap", Cube) = "" {}
        _ReflectionPower ("ReflectionPower", Range(0,10)) = 1
        _WireframeColor1 ("Wireframe Colour 1", Color) = (0, 0, 0)
        _WireframeColor2 ("Wireframe Colour 2", Color) = (0, 0, 0)
        
        _WireframeSmoothing ("Wireframe Smoothing", Range(0, 10)) = 1
        _WireframeThickness ("Wireframe Thickness", Range(0, 10)) = 1
    }
    
    SubShader
    {
        // No culling or depth
        // Cull Off ZWrite Off ZTest Always
        Tags {"Queue"="Transparent" }
        
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha // standard alpha blending
            ZWrite On

            CGPROGRAM
            #pragma target 4.0

            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geo

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2g
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
            };

            struct g2f 
            {
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                fixed3 col : COLOR;
                fixed3 worldPos : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                float3 baryCoords : TEXCOORD2;
            };

            samplerCUBE _CubeMap;

            fixed4 _Albedo;
            
            fixed3 _WireframeColor1;
            fixed3 _WireframeColor2;
            fixed _WireframeSmoothing;
            fixed _WireframeThickness;

            fixed _ReflectionPower;

            v2g vert (appdata v)
            {
                v2g o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul (unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex).xyz);
                return o;
            }

            [maxvertexcount(3)]
            void geo(triangle v2g i[3], inout TriangleStream<g2f> stream) 
            {
                float3 p0 = i[0].worldPos.xyz;
                float3 p1 = i[1].worldPos.xyz;
                float3 p2 = i[2].worldPos.xyz;

                float3 v0 = p0 - p1;
                float3 v1 = p1 - p2;
                float3 v2 = p2 - p0;

                g2f f0, f1, f2;

                f0.vertex = i[0].vertex;
                f1.vertex = i[1].vertex;
                f2.vertex = i[2].vertex;

                f0.worldPos = i[0].worldPos;
                f1.worldPos = i[1].worldPos;
                f2.worldPos = i[2].worldPos;

                f0.viewDir = i[0].viewDir;
                f1.viewDir = i[1].viewDir;
                f2.viewDir = i[2].viewDir;
                
//------------------------------------------------------------------------------------------------------
// gradient colour based on y position

                f0.col = lerp(_WireframeColor1, _WireframeColor2, p0.y);
                f1.col = lerp(_WireframeColor1, _WireframeColor2, p1.y);
                f2.col = lerp(_WireframeColor1, _WireframeColor2, p2.y);

//------------------------------------------------------------------------------------------------------
// calculate the triangle normal and apply to all vertices

                float3 normal = normalize(cross(v0, v2));
                
                f0.normal = normal;
                f1.normal = normal;
                f2.normal = normal;

//------------------------------------------------------------------------------------------------------
// add in barymetric coordinates for the wireframe removing the diagonal edge of the triangle

                float l0 = length(v0);
                float l1 = length(v1);
                float l2 = length(v2);

                fixed3 edge = lerp(fixed3(1, 0, 0), fixed3(0,0,1), l0 > l1 && l0 > l2); 
                
                f0.baryCoords = float3(1, 0, 0) + edge;
                f1.baryCoords = float3(0, 1, 0) + edge;
                f2.baryCoords = float3(0, 0, 1) + edge;

//------------------------------------------------------------------------------------------------------

                stream.Append(f0);
                stream.Append(f1);
                stream.Append(f2);
            }

            fixed4 frag (g2f i) : SV_Target
            {
                fixed3 col = _Albedo.xyz * (1 - max(i.normal.x, max(i.normal.y, i.normal.z)) * 0.5f);//
                
                fixed rim = (1.0 - saturate(dot(i.viewDir, i.normal))) * _ReflectionPower;
                fixed3 reflection = texCUBE(_CubeMap, i.worldPos).rgb * rim;
                
                col = saturate(col + reflection);

                float3 deltas = fwidth(i.baryCoords);
                
                fixed3 thickness = deltas * _WireframeThickness;
                fixed3 smoothing = deltas * _WireframeSmoothing;
                
                fixed3 barys = smoothstep(thickness, thickness + smoothing, i.baryCoords);
                fixed minBary = min(barys.x, min(barys.y, barys.z));
                
                fixed4 fullCol;
                fullCol.rgb = lerp(i.col, col, minBary);
                fullCol.a = _Albedo.a;

                return fullCol;
            }

            ENDCG
        }
    }
}
