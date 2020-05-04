Shader "Custom/ScrollShader" {
	Properties {
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)
		_pixelsPerUnit("Pixels Per Unit", Float) = 16
		[PerRendererData]_scroll("Scroll", Vector) = (0,0,0,0)
		[PerRendererData]_bounds("Bounds", Vector) = (0,0,0,0)
		[PerRendererData]_scale("Scale", Vector) = (0,0,0,0)
	}

	SubShader {
		Tags {
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Blend One OneMinusSrcAlpha

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile _ UNITY_ETC1_EXTERNAL_ALPHA

			#include "UnityCG.cginc"

			fixed4 _Color;
			float _pixelsPerUnit;
			//float2 _scroll;
			//float2 _size;
			//float2 _offset;
			UNITY_INSTANCING_BUFFER_START(Props)
				UNITY_DEFINE_INSTANCED_PROP(float2, _scroll)
				UNITY_DEFINE_INSTANCED_PROP(float4, _bounds)
				UNITY_DEFINE_INSTANCED_PROP(float2, _scale)
			UNITY_INSTANCING_BUFFER_END(Props)

			float4 AlignToPixelGrid(float4 vertex) {
				float4 worldPos = mul(unity_ObjectToWorld, vertex);

				worldPos.x = floor(worldPos.x * _pixelsPerUnit + 0.5) / _pixelsPerUnit;
				worldPos.y = floor(worldPos.y * _pixelsPerUnit + 0.5) / _pixelsPerUnit;

				return mul(unity_WorldToObject, worldPos);
			}

			struct appdata_t {
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f {
				float4 vertex   : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord  : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			float map(float value, float min1, float max1, float min2, float max2) {
				// Convert the current value to a percentage
				// 0% - min1, 100% - max1
				float perc = (value - min1) / (max1 - min1);

				// Do the same operation backwards with min2 and max2
				return perc * (max2 - min2) + min2;
			}

			v2f vert(appdata_t IN) {
				v2f OUT;

				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

				float4 alignedPos = AlignToPixelGrid(IN.vertex);

				OUT.vertex = UnityObjectToClipPos(alignedPos);
				OUT.texcoord = IN.texcoord;
				OUT.color = IN.color * _Color;

				return OUT;
			}

			sampler2D _MainTex;
			sampler2D _AlphaTex;

			fixed4 SampleSpriteTexture(float2 uv) {
				float2 srl = UNITY_ACCESS_INSTANCED_PROP(Props, _scroll);
				float4 bnd = UNITY_ACCESS_INSTANCED_PROP(Props, _bounds);
				float2 scl = UNITY_ACCESS_INSTANCED_PROP(Props, _scale);

				float2 uv01 = float2(map(uv.x, bnd.x, bnd.y, 0.0, 1.0), map(uv.y, bnd.z, bnd.w, 0.0, 1.0));

				//modifuv *= scl;
				//float premod = uv.x + srl.x;
				//premod = (premod - floor(premod / sze.x) * sze.x);
				//premod = fmod(x, sze.x);

				//modifuv = float2(premod + off.x, clamp((modifuv.y + off.y) + srl.y + (1.0f - scl.y) * 0.5f, 0, 1));

				float2 modifuv = float2(
					fmod((uv01.x * scl.x) + srl.x, 1.0),
					clamp((uv01.y * scl.y) - srl.y + (1.0f - scl.y) * 0.5f, 0.0, 0.9999)
				);
				fixed4 color = tex2D(_MainTex, float2(map(modifuv.x, 0.0, 1.0, bnd.x, bnd.y), map(modifuv.y, 0.0, 1.0, bnd.z, bnd.w)));

				/*#if ETC1_EXTERNAL_ALPHA
				color.a = tex2D(_AlphaTex, uv).r;
				#endif*/

				return color;
			}

			fixed4 frag(v2f IN) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(IN);

				fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;
				c.rgb *= c.a;
				return c;
			}

			ENDCG
		}
	}
}