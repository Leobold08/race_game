Shader "Custom/PSXTERRAIN"
{
	Properties
	{
		_Control ("Control", 2D) = "red" {}

		_Splat0 ("Layer 1", 2D) = "white" {}
		_Splat1 ("Layer 2", 2D) = "white" {}
		_Splat2 ("Layer 3", 2D) = "white" {}
		_Splat3 ("Layer 4", 2D) = "white" {}

		_Tile0 ("Layer 1 Tiling", Float) = 16
		_Tile1 ("Layer 2 Tiling", Float) = 16
		_Tile2 ("Layer 3 Tiling", Float) = 16
		_Tile3 ("Layer 4 Tiling", Float) = 16

		_VertexSnap ("Vertex Snap", Float) = 120
	}

	SubShader
	{
		Tags { "Queue"="Geometry" "RenderType"="Opaque" }

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			sampler2D _Control;
			sampler2D _Splat0;
			sampler2D _Splat1;
			sampler2D _Splat2;
			sampler2D _Splat3;

			float _Tile0;
			float _Tile1;
			float _Tile2;
			float _Tile3;

			float _VertexSnap;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float2 controlUV : TEXCOORD1;
			};

			v2f vert (appdata v)
			{
				v2f o;

				float4 clip = UnityObjectToClipPos(v.vertex);

				// PSX vertex snapping
				clip.xy = floor(clip.xy * _VertexSnap) / _VertexSnap;

				o.pos = clip;
				o.uv = v.uv;
				o.controlUV = v.uv2;

				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float4 control = tex2D(_Control, i.controlUV);

				float4 col0 = tex2D(_Splat0, i.uv * _Tile0);
				float4 col1 = tex2D(_Splat1, i.uv * _Tile1);
				float4 col2 = tex2D(_Splat2, i.uv * _Tile2);
				float4 col3 = tex2D(_Splat3, i.uv * _Tile3);

				float4 finalColor =
					col0 * control.r +
					col1 * control.g +
					col2 * control.b +
					col3 * control.a;

				return finalColor;
			}
			ENDCG
		}
	}
}