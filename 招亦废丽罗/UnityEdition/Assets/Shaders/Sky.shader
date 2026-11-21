// 天空渐变（全屏/内翻球体用，不受光照与雾影响）
Shader "Zhaoyi/Sky"
{
    Properties
    {
        _Horizon ("Horizon Color", Color) = (0.55, 0.24, 0.11, 1)
        _Zenith ("Zenith Color", Color) = (0.03, 0.04, 0.10, 1)
        _Power ("Gradient Power", Range(0.3, 3)) = 0.9
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Horizon;
            fixed4 _Zenith;
            float _Power;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = saturate(pow(saturate(i.uv.y), _Power));
                fixed3 grad = lerp(_Horizon.rgb, _Zenith.rgb, t);
                return fixed4(grad, 1.0);
            }
            ENDCG
        }
    }
    FallBack Off
}
