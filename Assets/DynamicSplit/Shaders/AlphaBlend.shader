Shader "Voronoi Split Screen/Alpha Blend Shader"
{
	Properties
	{
		_MainTex("UI Texture", 2D) = "white" {}
		_SecondaryTex("Split Screen Render", 2D) = "white" {}
	}

	SubShader
	{
		ColorMask RGB

		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D _SecondaryTex;

			struct appdata
			{
				float2 uv : TEXCOORD0;
				float4 vertex : POSITION;
				fixed4 color : COLOR;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
				fixed4 color : COLOR;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.color = v.color;
				o.uv = v.uv;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col1 = tex2D(_MainTex, float2(i.uv.x, i.uv.y));
				fixed4 col2 = tex2D(_SecondaryTex, float2(i.uv.x, i.uv.y));

				if (col1.w > 0)
				{
					return fixed4(col1.x * col1.w + col2.x * (1 - col1.w), col1.y * col1.w + col2.y * (1 - col1.w), col1.z * col1.w + col2.z * (1 - col1.w), 1);
				}
				else
				{
					return col2;
				}
			}
			ENDCG
		}
	}
}
