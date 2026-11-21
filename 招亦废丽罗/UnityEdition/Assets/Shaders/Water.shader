// 泪湖水面（菲涅尔 + 波纹 + 半透明）
Shader "Zhaoyi/Water"
{
    Properties
    {
        _Color ("Water Color", Color) = (0.12, 0.22, 0.28, 1)
        _Wave ("Wave Strength", Range(0, 0.3)) = 0.08
        _Speed ("Wave Speed", Range(0, 3)) = 0.8
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert alpha:fade
        #pragma target 3.0

        fixed4 _Color;
        float _Wave;
        float _Speed;

        struct Input
        {
            float3 worldPos;
            float3 viewDir;
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            float2 p = IN.worldPos.xz * 0.09;
            float t = _Time.y * _Speed;
            float w = sin(p.x * 1.3 + t) * 0.5 + sin(p.y * 1.7 - t * 1.3) * 0.5;
            float w2 = sin((p.x + p.y) * 0.7 + t * 1.7) * 0.5 + 0.5;

            fixed3 deep = _Color.rgb * 0.55;
            fixed3 shallow = _Color.rgb * 1.25 + fixed3(0.06, 0.10, 0.12);
            o.Albedo = lerp(deep, shallow, w2 * 0.6 + 0.2);
            o.Emission = _Color.rgb * (0.10 + 0.08 * w) + fixed3(0.03, 0.07, 0.09) * w2;
            o.Alpha = saturate(0.72 + 0.10 * w * _Wave * 10.0);
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}
