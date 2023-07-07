Shader "Unlit/Test"
{
    Properties
    {
        _MainTex ("Main texture", 2D) = "white" {}
        _ColorTex ("Color texture", 2D) = "white" {}
        _Color ("Color", Vector) = (0, 0, 0, 0)
        _Intensity ("Intensity", float) = 0
        _TexSize ("Texture size", float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
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
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _ColorTex;
            float _Intensity;
            float _TexSize;
            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.color = v.color;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                if (tex.a < 0.1) discard;

                fixed4 colorTex = tex2D(_ColorTex, i.pos.xy / _ScreenParams.x * _TexSize);
                fixed4 colorVal = colorTex * _Color;
                int val = colorVal.r + colorVal.g + colorVal.b > .8 ? 1 : 0;

                return tex * i.color - float4(1, 1, 1, 0) * val * _Intensity;
            }
            ENDCG
        }
    }
}
