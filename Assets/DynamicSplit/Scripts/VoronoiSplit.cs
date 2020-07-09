using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class VoronoiSplit : MonoBehaviour
{
    private struct RenderProperties
    {
        public readonly int Width;
        public readonly int Height;
        public readonly float AspectRatio;
        public float OrthoSize;

        public RenderProperties(int width, int height, float orthoSize = 0)
        {
            Width = width;
            Height = height;
            OrthoSize = orthoSize;
            AspectRatio = (float)Width / Height;
        }
    }

    #region Constants

    private readonly string[] SHADER_PLAYER_POSITION = new[]
    {
        "_Player1Pos",
        "_Player2Pos"
    };

    private readonly string SHADER_PLAYER = "_Player";
    private readonly string SHADER_LINE_COLOR = "_LineColor";
    private readonly string SHADER_LINE_THICKNESS = "_LineThickness";
    private readonly string SHADER_CELLS_STENCIL_OP = "_VoronoiCellsStencilOp";
    private readonly string SHADER_CELLS_STENCIL_PLAYER = "_VoronoiCellsPlayerStencil";
    private readonly string SHADER_CELLS_STENCIL_TEX = "_VornoiTex";
    private readonly string SHADER_MASKED_STENCIL_OP = "_MaskedStencilOp";
    private readonly string SHADER_BLEND_TEXTURE = "_SecondaryTex";
    
    private const int MAX_PLAYERS = 2;

    #endregion

    #region Public Variables

    [Header("References")]
    public Camera MainCamera;
    public Camera PlayerCamera;
    public Renderer MaskRenderer;
    public Transform MaskTransform;

    public Material VoronoiCellsMaterial;
    public Material SplitLineMaterial;
    public Material AlphaBlendMaterial;
    public Material FxaaMaterial;

    [Header("Graphics")]
    public Color LineColor = Color.black;
    public bool EnableFXAA = true;

    [Header("Players")]
    public int PlayerCount = 0;
    public Transform[] Players;
    public bool EnableMerging = true;
    
    #endregion

    #region Variables

    private Color lastLineColor = Color.black;
    
    private RenderProperties screen;
    private RenderTexture playerTex;
    private RenderTexture cellsTexture;

    private Vector2[] worldPositions = new Vector2[MAX_PLAYERS];
    private Vector2[] normalizedPositions = new Vector2[MAX_PLAYERS];
    private Vector2 mergedPosition = Vector2.one / 2;
    private float mergeRatio = 1f;
    private int activePlayers = 2;

    #endregion

    private void Awake()
    {
        VoronoiCellsMaterial = Instantiate(VoronoiCellsMaterial);
        SplitLineMaterial = Instantiate(SplitLineMaterial);
        AlphaBlendMaterial = Instantiate(AlphaBlendMaterial);
        FxaaMaterial = Instantiate(FxaaMaterial);

        MaskRenderer.sharedMaterial = VoronoiCellsMaterial;

        InitializeCameras();

        SetLineColor(LineColor);
    }

    private void InitializeCameras()
    {
        PlayerCamera.depthTextureMode = DepthTextureMode.Depth;
        UpdateRenderProperties();
    }

    private void UpdateRenderProperties()
    {
        OnResolutionChanged(Screen.width, Screen.height);
        OnOrthoSizeChanged(MainCamera.orthographicSize);
    }

    private void OnResolutionChanged(int width, int height)
    {
        if (screen.Width == width && screen.Height == height)
        {
            return;
        }

        playerTex?.Release();
        cellsTexture?.Release();

        playerTex = new RenderTexture(width, height, 32);
        playerTex.name = "Player Render";

        PlayerCamera.targetTexture = playerTex;

        cellsTexture = new RenderTexture(width, height, 0, GraphicsFormat.R8_UNorm);
        cellsTexture.name = "Cells Visualization Texture";

        SplitLineMaterial.SetTexture(SHADER_CELLS_STENCIL_TEX, cellsTexture);
        SplitLineMaterial.SetFloat(SHADER_LINE_THICKNESS, (float)height / 200);

        screen = new RenderProperties(width, height);
    }

    private void OnOrthoSizeChanged(float orthoSize)
    {
        if (Mathf.Abs(screen.OrthoSize - orthoSize) < Mathf.Epsilon)
        {
            return;
        }

        PlayerCamera.orthographicSize = orthoSize;
        MaskTransform.localScale = new Vector3(orthoSize * screen.AspectRatio * 2, orthoSize * 2);

        screen.OrthoSize = orthoSize;
    }

    private void SetLineColor(Color color)
    {
        SplitLineMaterial.SetColor(SHADER_LINE_COLOR, LineColor);
        lastLineColor = LineColor;
    }

    private void Update()
    {
        // handle edge cases
        {
            if (Players.Length < PlayerCount)
            {
                PlayerCount = Players.Length;
                Debug.LogWarningFormat(
                    "PlayerCount ({0}) is higher than number of players in Players ({1}) array. Setting PlayerCount to {1}.",
                    PlayerCount, Players.Length);
            }

            if (PlayerCount > MAX_PLAYERS)
            {
                PlayerCount = MAX_PLAYERS;
                Debug.LogWarningFormat(
                    "Voronoi split screen doesn't support more than {0} players right now. Setting PlayerCount to {0}",
                    MAX_PLAYERS);
            }
        }

        // set player world positions
        {
            if (Players.Length == 0)
            {
                worldPositions[0] = transform.position;
            }

            for (int i = 0; i < Players.Length && i < MAX_PLAYERS; i++)
            {
                worldPositions[i] = Players[i].position;
            }
        }
        
        // handle single player
        if (PlayerCount <= 1)
        {
            normalizedPositions[0] = Vector3.one / 2;
            mergeRatio = 0;
            activePlayers = 1;
            RenderPlayers(activePlayers);
            return;
        }

        // update screen properties
        {
            UpdateRenderProperties();

            if (lastLineColor != LineColor)
            {
                SetLineColor(LineColor);
            }
        }

        // calculate normalized positions
        {
            Vector2 min, max;

            // calculate positions in <0, infinity> range
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                normalizedPositions[i] = worldPositions[i];
            }

            min = normalizedPositions[0];
            max = normalizedPositions[0];

            for (int i = 1; i < MAX_PLAYERS; i++)
            {
                if (normalizedPositions[i].x < min.x) min.x = normalizedPositions[i].x;
                if (normalizedPositions[i].x > max.x) max.x = normalizedPositions[i].x;
                if (normalizedPositions[i].y < min.y) min.y = normalizedPositions[i].y;
                if (normalizedPositions[i].y > max.y) max.y = normalizedPositions[i].y;
            }

            max -= min;
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                normalizedPositions[i] -= min;
            }

            // correct positions for screen aspect ratio
            var diff = Vector2.zero;
            if (max.x > max.y * screen.AspectRatio)
            {
                diff.y = ((max.x / screen.AspectRatio) - max.y) / 2;
            }
            else if (max.y > max.x * 1 / screen.AspectRatio)
            {
                diff.x = ((max.y * screen.AspectRatio) - max.x) / 2;
            }

            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                normalizedPositions[i] += diff;
            }
            max += diff * 2;

            // convert to <0, 1> range
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                normalizedPositions[i] /= max;
            }
        }

        // handle merging
        {
            activePlayers = 2;
            mergeRatio = 0;

            if (EnableMerging)
            {
                var diff = normalizedPositions[1] - normalizedPositions[0];
                var mergeDistance = Vector2.SqrMagnitude(diff * screen.OrthoSize);
                var realDiff = worldPositions[1] - worldPositions[0];
                var realDistance = Vector2.SqrMagnitude(realDiff);

                var distRatio = realDistance / mergeDistance;
                var smoothingDistance = 1;

                if (distRatio <= 1 + smoothingDistance)
                {
                    mergedPosition = Vector2.Lerp(worldPositions[0], worldPositions[1], 0.5f);
                    if (distRatio <= 1)
                    {
                        // screens are merged
                        mergeRatio = 1;
                        activePlayers = 1;
                    }
                    else
                    {
                        // screens are in between merged and split, we need to convert mergeRatio to <0, 1> range,
                        // where 0 is split and 1 merged
                        mergeRatio = distRatio - 1;
                        mergeRatio /= smoothingDistance;
                        mergeRatio = 1 - mergeRatio;
                    }
                }
            }
        }

        // render multiplayer
        RenderPlayers(activePlayers);
    }

    private void RenderPlayers(int playerCount)
    {
        for (int i = 0; i < playerCount; i++)
        {
            var pivot = normalizedPositions[i];
            VoronoiCellsMaterial.SetVector(SHADER_PLAYER_POSITION[i], new Vector4(pivot.x, pivot.y, 0.0f, 1.0f));
        }

        for (int i = playerCount; i < MAX_PLAYERS; i++)
        {
            VoronoiCellsMaterial.SetVector(SHADER_PLAYER_POSITION[i], Vector4.zero);
        }

        VoronoiCellsMaterial.SetInt(SHADER_CELLS_STENCIL_OP, (int)CompareFunction.Always);
        Shader.SetGlobalInt(SHADER_MASKED_STENCIL_OP, (int)CompareFunction.Equal);
        MaskRenderer.enabled = true;

        RenderTexture.active = playerTex;
        GL.Clear(true, false, Color.black);
        RenderTexture.active = null;

        for (int i = 0; i < playerCount; i++)
        {
            Shader.SetGlobalInt(SHADER_CELLS_STENCIL_PLAYER, i + 1);

            Vector2 center = Vector2.one / 2;
            Vector2 offset = (center - normalizedPositions[i]) * screen.OrthoSize * new Vector2(screen.AspectRatio, 1);
            transform.localPosition = Vector2.Lerp(worldPositions[i] + offset, mergedPosition, mergeRatio);

            VoronoiCellsMaterial.SetInt(SHADER_PLAYER, i + 1);
            PlayerCamera.Render();
        }

        MaskRenderer.enabled = false;
        Shader.SetGlobalInt(SHADER_MASKED_STENCIL_OP, (int)CompareFunction.Disabled);
        VoronoiCellsMaterial.SetInt(SHADER_CELLS_STENCIL_OP, (int)CompareFunction.Disabled);
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        var screenTex = RenderTexture.GetTemporary(screen.Width, screen.Height);
        var fxaaTex = EnableFXAA ? RenderTexture.GetTemporary(screen.Width, screen.Height) : null;

        Graphics.Blit(null, cellsTexture, VoronoiCellsMaterial);                          // cells visalization
        Graphics.Blit(playerTex, screenTex, SplitLineMaterial);                                // merge screens and split line texture
        if (EnableFXAA) Graphics.Blit(screenTex, fxaaTex, FxaaMaterial);                       // FXAA pass
        AlphaBlendMaterial.SetTexture(SHADER_BLEND_TEXTURE, EnableFXAA ? fxaaTex : screenTex); // set screen texture
        Graphics.Blit(src, dst, AlphaBlendMaterial);                                           // blend rendered UI on top of screen texture

        if (EnableFXAA) RenderTexture.ReleaseTemporary(fxaaTex);
        RenderTexture.ReleaseTemporary(screenTex);
    }
}
