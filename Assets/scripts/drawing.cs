using UnityEngine;

public class drawing : MonoBehaviour
{
    [Header("drawing")]
    public float radius;
    public float strength;

    [Header("rendering texture")]
    public Mesh quad;
    public Vector2Int pixelSize;

    public Material fullscreenMat;
    RenderTexture drawingTexture;

    [Header("computing")]
    public ComputeShader computeDraw;
    Vector2 groupSize;

    [Header("marching squares")]
    public marchingSquares marcher;

    private void Awake()
    {
        drawingTexture = new RenderTexture(pixelSize.x, pixelSize.y, 0, RenderTextureFormat.R8);
        drawingTexture.filterMode = FilterMode.Bilinear;
        drawingTexture.enableRandomWrite = true;
        drawingTexture.Create();

        uint threadX;
        uint threadY;
        computeDraw.GetKernelThreadGroupSizes(0, out threadX, out threadY, out _);
        groupSize = new Vector2((float)pixelSize.x / threadX, (float)pixelSize.y / threadY);
        computeDraw.SetTexture(0, "Result", drawingTexture);

        fullscreenMat.SetTexture("_data", drawingTexture);
        marcher.dataTexture = drawingTexture;
    }

    private void Update()
    {
        computeDraw.SetInts("mousePos", new int[2] { (int)Input.mousePosition.x, (int)Input.mousePosition.y });
        computeDraw.SetInts("res", new int[2] { Screen.width, Screen.height });
        computeDraw.SetInts("pixelRes", new int[2] { pixelSize.x, pixelSize.y });
        computeDraw.SetInt("isDrawing", Input.GetMouseButton(0) ? 1 : Input.GetMouseButton(1) ? -1 : 0);
        computeDraw.SetFloat("radius", radius);
        computeDraw.SetFloat("strength", strength);
        for (int i = 0; i < 10; i++)
        {
            computeDraw.Dispatch(0, (int)groupSize.x, (int)groupSize.y, 1);
        }
        Graphics.DrawMesh(quad, Matrix4x4.Scale(2f * new Vector3(((float)Screen.width / Screen.height) * Camera.main.orthographicSize, Camera.main.orthographicSize)), fullscreenMat, 0);
    }
}
