Shader "Voronoi Split Screen/Voronoi Cell"
{
    Properties
    {
        _Player1Pos ("Player 1 Position", Vector) = (1.0, 1.0, 1.0, 0.0)
        _Player2Pos ("Player 2 Position", Vector) = (1.0, 1.0, 1.0, 0.0)
        _Player3Pos ("Player 3 Position", Vector) = (1.0, 1.0, 1.0, 0.0)
        _Player4Pos ("Player 4 Position", Vector) = (1.0, 1.0, 1.0, 0.0)
        _Player5Pos ("Player 5 Position", Vector) = (1.0, 1.0, 1.0, 0.0)
        _Player6Pos ("Player 6 Position", Vector) = (1.0, 1.0, 1.0, 0.0)
        _Player7Pos ("Player 7 Position", Vector) = (1.0, 1.0, 1.0, 0.0)
        _Player8Pos ("Player 8 Position", Vector) = (1.0, 1.0, 1.0, 0.0)

        _Player ("Player", Int) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Geometry-1" }

        Stencil {
            Ref [_VoronoiCellsPlayerStencil]
            Comp [_VoronoiCellsStencilOp]
            Pass Replace
        }

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 _Player1Pos;
            float4 _Player2Pos;
            float4 _Player3Pos;
            float4 _Player4Pos;
            float4 _Player5Pos;
            float4 _Player6Pos;
            float4 _Player7Pos;
            float4 _Player8Pos;

            int _Player;

            int _VoronoiCellsStencilOp;

            void GetCloserPlayer(float4 playerPos, int player, float2 coord, inout float minDistance, inout int closestPlayer)
            {
                if (playerPos.w > 0)
                {
                    float dst = distance(playerPos.xy, coord);
                    if (dst < minDistance)
                    {
                        minDistance = dst;
                        closestPlayer = player;
                    }
                }
            }

            fixed frag(v2f i) : SV_Target
            {
                // player 1 is default
                int player = 1;
                float minDistance = distance(_Player1Pos.xy, i.uv.xy);

                // go through other players
                GetCloserPlayer(_Player2Pos, 2, i.uv.xy, minDistance, player);
                GetCloserPlayer(_Player3Pos, 3, i.uv.xy, minDistance, player);
                GetCloserPlayer(_Player4Pos, 4, i.uv.xy, minDistance, player);
                GetCloserPlayer(_Player5Pos, 5, i.uv.xy, minDistance, player);
                GetCloserPlayer(_Player6Pos, 6, i.uv.xy, minDistance, player);
                GetCloserPlayer(_Player7Pos, 7, i.uv.xy, minDistance, player);
                GetCloserPlayer(_Player8Pos, 8, i.uv.xy, minDistance, player);

                if (_VoronoiCellsStencilOp == 0)
                {
					return 1 / player;
                }
                else
                {
                    if (player != _Player)
                    {
                        clip(-1);
                    }
                    return 0;
                }
            }
            ENDCG
        }
    }
}
