Shader "UnityFaction/Animated"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1, 1, 1, 1)

		_Cols("Cols Count", Int) = 5
		_Rows("Rows Count", Int) = 3
		_Frame("Per Frame Length", Float) = 0.5
	}

		SubShader
	{
		Tags{"Queue"="Transparent" "RenderType" = "Transparent" }
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

	fixed4 _Color;

	uint _Cols;
	uint _Rows;

	float _Frame;

	fixed4 shot(sampler2D tex, float2 uv, float dx, float dy, int frame) {
		return tex2D(tex, float2(
			(uv.x * dx) + fmod(frame, _Cols) * dx,
			1.0 - ((uv.y * dy) + (frame / _Cols) * dy)
			));
	}

	v2f vert(appdata v) {
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = TRANSFORM_TEX(v.uv, _MainTex);
		return o;
	}

	fixed4 frag(v2f i) : SV_Target{
		int frames = _Rows * _Cols;
		float r = fmod(_Time.y, _Frame);
		float frame = fmod(_Time.y / _Frame, frames);
		int current = floor(frame);
		int next = floor(fmod(frame + 1, frames));

		float dx = 1.0 / _Cols;
		float dy = 1.0 / _Rows;

		fixed4 left = shot(_MainTex, i.uv, dx, dy, current);
		fixed4 right = shot(_MainTex, i.uv, dx, dy, next);
		fixed4 toReturn = lerp(left, right, r) * _Color;
		return toReturn;
	}

		ENDCG
	}
	}
}