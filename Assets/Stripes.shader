Shader "Unlit/Test"
{
    Properties
    {
        _ColorA ("Color A", Color) = (.2, .2, .2, 1)
        _ColorB ("Color B", Color) = (.4, .4, .4, 1)
        _Size ("Size", Float) = 0
        _Shift ("Shift", Float) = 0
        _Discard ("Discard", Range(0, 1)) = 0
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            fixed4 _ColorA;
            fixed4 _ColorB;
            float _Size;
            float _Shift;
            float _Discard;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 pos = float2(((_ScreenParams.x / 2 - i.vertex.x) / _ScreenParams.y) + _Shift, i.vertex.y / _ScreenParams.y + _Shift);

                if ((pos.x + pos.y) % _Size < _Size * _Discard) discard;

                bool isA = (pos.x + pos.y) % _Size < _Size / 2;
                return isA ? _ColorA : _ColorB;
            }
            ENDCG
        }
    }
}
