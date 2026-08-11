Shader "Custom/DuotoneRedBlack"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ShadowColor ("Shadow Color (negro)", Color) = (0,0,0,1)
        _HighlightColor ("Highlight Color (rojo)", Color) = (0.75,0.05,0.05,1)
        _Intensity ("Intensity (0=original, 1=duotono completo)", Range(0,1)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _ShadowColor;
            fixed4 _HighlightColor;
            float _Intensity;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;

                float luminance = dot(c.rgb, float3(0.299, 0.587, 0.114));
                fixed3 duotone = lerp(_ShadowColor.rgb, _HighlightColor.rgb, luminance);

                c.rgb = lerp(c.rgb, duotone, _Intensity);
                return c;
            }
            ENDCG
        }
    }
}
