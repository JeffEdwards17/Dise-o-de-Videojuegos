Shader "Custom/VHSVision"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Duotone)]
        _ShadowColor ("Shadow Color (negro)", Color) = (0,0,0,1)
        _HighlightColor ("Highlight Color (rojo)", Color) = (0.75,0.05,0.05,1)
        _DuotoneIntensity ("Duotone Intensity", Range(0,1)) = 0.85

        [Header(VHS)]
        _ScanlineIntensity ("Scanline Intensity", Range(0,1)) = 0.25
        _ScanlineCount ("Scanline Count", Float) = 240
        _NoiseIntensity ("Grain/Noise Intensity", Range(0,1)) = 0.15
        _ChromaticAberration ("Chromatic Aberration", Range(0,0.02)) = 0.004
        _GlitchIntensity ("Glitch Intensity", Range(0,0.3)) = 0.08
        _GlitchSpeed ("Glitch Speed", Float) = 8
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
            float _DuotoneIntensity;
            float _ScanlineIntensity;
            float _ScanlineCount;
            float _NoiseIntensity;
            float _ChromaticAberration;
            float _GlitchIntensity;
            float _GlitchSpeed;

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

            float rand(float2 co)
            {
                return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;

                // --- Glitch: desplaza bloques horizontales de la imagen aleatoriamente ---
                float glitchLine = floor(uv.y * 40);
                float glitchTime = floor(_Time.y * _GlitchSpeed);
                float glitchRand = rand(float2(glitchLine, glitchTime));
                float glitchOffset = 0;
                if (glitchRand > 0.93) // solo algunos frames/lineas tienen glitch
                {
                    glitchOffset = (rand(float2(glitchTime, glitchLine)) - 0.5) * _GlitchIntensity;
                }
                uv.x += glitchOffset;

                // --- Aberración cromática: separa canales R/G/B levemente ---
                float2 caOffset = float2(_ChromaticAberration, 0);
                fixed r = tex2D(_MainTex, uv + caOffset).r;
                fixed g = tex2D(_MainTex, uv).g;
                fixed b = tex2D(_MainTex, uv - caOffset).b;
                fixed a = tex2D(_MainTex, uv).a;

                fixed4 c = fixed4(r, g, b, a) * IN.color;

                // --- Duotono rojo/negro ---
                float luminance = dot(c.rgb, float3(0.299, 0.587, 0.114));
                fixed3 duotone = lerp(_ShadowColor.rgb, _HighlightColor.rgb, luminance);
                c.rgb = lerp(c.rgb, duotone, _DuotoneIntensity);

                // --- Scanlines (líneas horizontales oscuras tipo TV vieja) ---
                float scanline = sin(uv.y * _ScanlineCount * 3.14159) * 0.5 + 0.5;
                c.rgb *= lerp(1.0, scanline, _ScanlineIntensity);

                // --- Grano/ruido aleatorio ---
                float noise = rand(uv * _Time.y);
                c.rgb += (noise - 0.5) * _NoiseIntensity;

                return c;
            }
            ENDCG
        }
    }
}
