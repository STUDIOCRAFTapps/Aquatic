Shader "Custom/Transparent Texture2D" {
	Properties {
		_Color("Main Color (A=Opacity)", Color) = (1,1,1,1)
		_MainTex("Tex", 2DArray) = "" {}
	}

	SubShader {
		Tags {
			"Queue" = "AlphaTest" 
			"IgnoreProjector" = "True"
			"PreviewType" = "Plane"
			"RenderType" = "TransparentCutout" 
		}
		LOD 100

		Lighting Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.5
			#pragma multi_compile_fog
			#pragma require 2darray

			#include "UnityCG.cginc"

			struct appdata_t {
				float3 texcoord : TEXCOORD0;
				float2 texcoord1 : TEXCOORD1;
				float4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f {
				float3 texcoord : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float texindex : POSITION1;
				UNITY_FOG_COORDS(1)
				UNITY_VERTEX_OUTPUT_STEREO
			};

			float4 _MainTex_ST;
			fixed4 _Color;

			v2f vert(appdata_t v) {
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord.xy = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
				o.texcoord.z = v.texcoord.z;
				o.texindex = floor((_Time.y * v.texcoord1.y) % v.texcoord1.x);
				return o;
			}

			UNITY_DECLARE_TEX2DARRAY(_MainTex);

			half4 frag(v2f i) : SV_Target {
				//Animate Tiles
				fixed4 col = UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(i.texcoord.x, i.texcoord.y, i.texcoord.z + i.texindex)) * _Color;
				clip(col.a - 0.5);
				return col;
			}
			ENDCG
		}
	}
}
