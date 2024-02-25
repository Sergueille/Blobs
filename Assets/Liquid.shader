Shader "Unlit/Liquid"
{
    Properties
    {
        _Previous ("Previous frame", 2D) = "white" {}
        _FlowMap ("Flow map", 2D) = "black" {}
        _FlowSpeed ("(Amount, Flow circle size, Blur, Attenuation speed)", Vector) = (5, 2, 0.2, 1)
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
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _Previous;
            float4 _Previous_TexelSize;
            sampler2D _FlowMap;
            float4 _FlowSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float ratio = _Previous_TexelSize.x * _Previous_TexelSize.w;
                fixed4 flow = tex2D(_FlowMap, (i.uv + float2(_CosTime.y * ratio, _SinTime.y) * _FlowSpeed.g) * float2(1, ratio));

                fixed2 amount = (flow.rg - fixed2(0.5, 0.5)) * _FlowSpeed.r * flow.b * unity_DeltaTime.z;

                fixed4 right = tex2D(_Previous, i.uv + float2(amount.x * ratio, amount.y));
                fixed4 here = tex2D(_Previous, i.uv);

                float attSpeed = _FlowSpeed.w;
                float attenuation = 1.0 - unity_DeltaTime.z / attSpeed;

                return (right * (1.0 - _FlowSpeed.b) + here * _FlowSpeed.b) * attenuation;
            }
            ENDCG
        }
    }
}
