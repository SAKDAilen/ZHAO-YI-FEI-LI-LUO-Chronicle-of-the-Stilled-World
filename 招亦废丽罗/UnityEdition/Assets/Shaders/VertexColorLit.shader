// 顶点色 + 半兰伯特光照（地形 / 环境道具 / 合并网格）
Shader "Zhaoyi/VertexColorLit"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _EmissionColor ("Emission", Color) = (0,0,0,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.15
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert addshadow
        #pragma target 3.0

        fixed4 _Color;
        fixed4 _EmissionColor;

        struct Input
        {
            float4 color : COLOR;
            float3 worldPos;
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed3 tinted = IN.color.rgb * _Color.rgb;
            o.Albedo = tinted;
            o.Emission = _EmissionColor.rgb;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
