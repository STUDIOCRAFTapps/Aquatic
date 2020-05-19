Shader "Custom/Sprites/Colorized" {
	Properties {
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		[MaterialToggle] PixelSnap("Pixel snap", Float) = 0
	}

	SubShader {
		Tags {
			"Queue" = "AlphaTest"
			"IgnoreProjector" = "True"
			"RenderType" = "TransparentCutout"
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}

		Cull Off
		Lighting Off
		//ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ PIXELSNAP_ON
			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex		: POSITION;
				float4 color		: COLOR;
				float2 texcoord		: TEXCOORD0;
			};

			struct v2f {
				float4 vertex		: SV_POSITION;
				float4 color		: COLOR;
				float2 texcoord		: TEXCOORD0;
			};

			fixed4 _Color;

			v2f vert(appdata_t IN) {
				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = IN.texcoord;
				OUT.color = IN.color;
				#ifdef PIXELSNAP_ON
				OUT.vertex = UnityPixelSnap(OUT.vertex);
				#endif

				OUT.color = IN.color;
				return OUT;
			}

			sampler2D _MainTex;
			sampler2D _AlphaTex;

			fixed4 SampleSpriteTexture(float2 uv) {
				fixed4 color = tex2D(_MainTex, uv);

				return color;
			}

			fixed4 frag(v2f IN) : SV_Target {
				fixed4 textureColor = SampleSpriteTexture(IN.texcoord);
				fixed3 c = lerp(textureColor.rgb, IN.color.rgb, IN.color.a);

				clip(textureColor.a - 0.5f);
				return fixed4(c, textureColor.a);
			}
			ENDCG
		}
	}
}