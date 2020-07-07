Shader "Voronoi Split Screen/Split Line"
{
    Properties
    {
		_MainTex ("Main Texture", 2D) = "white" {}
		_VornoiTex ("Vornoi Cells Texture", 2D) = "white" {}
        _LineColor ("Line Color", Color) = (0.0, 0.0, 0.0, 1.0)
        _LineThickness ("Line Thickness", float) = 5.0
    }

    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            sampler2D _VornoiTex;
            fixed4 _LineColor;
            float _LineThickness;

            #include "UnityCG.cginc"

			void MakeKernel(inout fixed4 n[9], sampler2D tex, float2 coord)
			{
				float w = _LineThickness / _ScreenParams.x;
				float h = _LineThickness / _ScreenParams.y;

				n[0] = tex2D(tex, coord + float2(-w, -h));
				n[1] = tex2D(tex, coord + float2(0.0, -h));
				n[2] = tex2D(tex, coord + float2(w, -h));
				n[3] = tex2D(tex, coord + float2(-w, 0.0));
				n[4] = tex2D(tex, coord);
				n[5] = tex2D(tex, coord + float2(w, 0.0));
				n[6] = tex2D(tex, coord + float2(-w, h));
				n[7] = tex2D(tex, coord + float2(0.0, h));
				n[8] = tex2D(tex, coord + float2(w, h));
			}

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

            fixed4 frag(v2f i) : SV_Target
            {
				fixed4 n[9];
                MakeKernel(n, _VornoiTex, i.uv.xy);

				fixed4 edgeH = n[2] + (2.0*n[5]) + n[8] - (n[0] + (2.0*n[3]) + n[6]);
				fixed4 edgeV = n[0] + (2.0*n[1]) + n[2] - (n[6] + (2.0*n[7]) + n[8]);
				fixed4 sobel = sqrt((edgeH * edgeH) + (edgeV * edgeV));

				if (distance(sobel.rgb, fixed3(0.0, 0.0, 0.0)) == 0)
				{
					return tex2D(_MainTex, i.uv.xy);
				}

				return _LineColor;
            }
            ENDCG
        }
    }
}
