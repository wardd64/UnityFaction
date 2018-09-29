Shader "UnityFaction/UVScroll" 
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_Color("Texture Color", Color) = (1, 1, 1, 1)

		_ScrollXSpeed("X Scroll Speed", float) = 0
		_ScrollYSpeed("Y Scroll Speed", float) = 0
	}

	SubShader
	{
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		LOD 100

		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

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
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _ScrollXSpeed;
			float _ScrollYSpeed;
			fixed4 _Color;

			v2f vert(appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target{
				fixed varX = i.uv.x + _ScrollXSpeed * _Time[1];
				fixed varY = i.uv.y - _ScrollYSpeed * _Time[1];
				return tex2D(_MainTex, fixed2(varX, varY)) * _Color;
			}

			ENDCG
		}
	}
}