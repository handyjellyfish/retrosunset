Shader "Custom/Ring"
{
    Properties
    {
        _Color("Color", Color) = (0,0,0,1)
        _Feather("Feather", Range(0, 1)) = 0
        _Radius("Radius", Range(0, 0.5)) = 0.5
        _HoleRadius("Hole Radius", Range(0, 0.5)) = 0

    }
    SubShader
    {
        // No culling or depth
        //Cull Off ZWrite On ZTest Always
        Tags { "RenderType"="Transparent" }
        
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha

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
             
            fixed4 _Color;
            fixed _Radius;
            fixed _HoleRadius;
 
            fixed4 frag (v2f i) : SV_Target
            {
                float dist = length((i.uv - 0.5));
                
                clip(_Radius - dist);
                clip(dist - _HoleRadius);
                
                float4 c = _Color;
                c.a = 1 - smoothstep(_HoleRadius, _Radius, dist);
                return c;
            }
            ENDCG
        }
    }
}