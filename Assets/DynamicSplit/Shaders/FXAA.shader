Shader "Voronoi Split Screen/FXAA"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #define FXAA_SPAN_MAX	8.0
            #define FXAA_REDUCE_MUL 1.0/8.0
            #define FXAA_REDUCE_MIN 1.0/128.0

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;

            fixed4 frag (v2f i) : SV_Target
            {
				float2 add = float2(1.0, 1.0) / _ScreenParams.xy;

	            float3 rgbNW = tex2D(_MainTex, i.uv + float2(-add.x, -add.y));
	            float3 rgbNE = tex2D(_MainTex, i.uv + float2(add.x, -add.y));
	            float3 rgbSW = tex2D(_MainTex, i.uv + float2(-add.x, add.y));
	            float3 rgbSE = tex2D(_MainTex, i.uv + float2(add.x, add.y));
	            float3 rgbM = tex2D(_MainTex, i.uv);

	            float3 luma = float3(0.299, 0.587, 0.114);
	            float lumaNW = dot(rgbNW, luma);
	            float lumaNE = dot(rgbNE, luma);
	            float lumaSW = dot(rgbSW, luma);
	            float lumaSE = dot(rgbSE, luma);
	            float lumaM = dot(rgbM,  luma);

	            float lumaMin = min(lumaM, min(min(lumaNW, lumaNE), min(lumaSW, lumaSE)));
	            float lumaMax = max(lumaM, max(max(lumaNW, lumaNE), max(lumaSW, lumaSE)));

	            float2 dir;
	            dir.x = -((lumaNW + lumaNE) - (lumaSW + lumaSE));
	            dir.y = ((lumaNW + lumaSW) - (lumaNE + lumaSE));

	            float dirReduce = max((lumaNW + lumaNE + lumaSW + lumaSE) * (0.25 * FXAA_REDUCE_MUL), FXAA_REDUCE_MIN);

	            float rcpDirMin = 1.0 / (min(abs(dir.x), abs(dir.y)) + dirReduce);

	            dir = min(float2(FXAA_SPAN_MAX,  FXAA_SPAN_MAX), max(float2(-FXAA_SPAN_MAX, -FXAA_SPAN_MAX), dir * rcpDirMin)) * add;

	            float3 rgbA = (1.0 / 2.0) * (tex2D(_MainTex, i.uv + dir * (1.0 / 3.0 - 0.5)) + tex2D(_MainTex, i.uv + dir * (2.0 / 2.0 - 0.5)));

	            float3 rgbB = rgbA * (1.0 / 2.0) + (1.0 / 4.0) * (tex2D(_MainTex, i.uv.xy + dir * (0.0 / 3.0 - 0.5)) + tex2D(_MainTex, i.uv.xy + dir * (3.0 / 3.0 - 0.5)));

	            float lumaB = dot(rgbB, luma);

	            if ((lumaB < lumaMin) || (lumaB > lumaMax))
	            {
		            return fixed4(rgbA, 1.0);
	            }
                else
                {
	                return fixed4(rgbB, 1.0);
                }
            }
            ENDCG
        }
    }
}
