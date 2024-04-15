Shader "Unlit/trail"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

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
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 texUV = float2(
                    i.uv.x * 2.5,
                    i.uv.y * 0.5
                );

                // sample the texture
                float tex = tex2D(_MainTex, texUV).r;
                float texAlpha = tex * 0.8 + 0.3;
                float dist = 1 - abs(i.uv.y - 0.5) * 2;

                float finalAlpha = dist * i.color.a * texAlpha;

                if (finalAlpha < 0.3) discard;

                return float4(i.color.xyz, finalAlpha * 0.8);
            }
            ENDCG
        }
    }
}
