using UnityEngine;

public class perlinNoise : MonoBehaviour
{
    [Header("making noise")]
    public float perlinRes;
    Texture2D texture;

    [Header("rendering texture")]
    public Mesh quad;
    public Vector2Int pixelSize;

    public Material fullscreenMat;
    RenderTexture perlinTexture;

    [Header("marching squares")]
    public marchingSquares marcher;

    private void Awake()
    {
        perlinTexture = new RenderTexture(pixelSize.x, pixelSize.y, 0, RenderTextureFormat.R8);
        texture = new Texture2D(perlinTexture.width, perlinTexture.height, TextureFormat.R8, false);
        perlinTexture.filterMode = FilterMode.Bilinear;
        perlinTexture.enableRandomWrite = true;
        perlinTexture.Create();

        fullscreenMat.SetTexture("_data", perlinTexture);
        marcher.dataTexture = perlinTexture;
    }

    private void Update()
    {
        for (int x = 0; x < perlinTexture.width; x++)
        {
            for (int y = 0; y < perlinTexture.height; y++)
            {
                float value = Mathf.PerlinNoise((float)x / perlinTexture.width * perlinRes, (float)y / perlinTexture.height * perlinRes);
                texture.SetPixel(x, y, Color.white * value);
            }
        }
        texture.Apply();
        Graphics.Blit(texture, perlinTexture);

        Graphics.DrawMesh(quad, Matrix4x4.Scale(2f * new Vector3(((float)Screen.width / Screen.height) * Camera.main.orthographicSize, Camera.main.orthographicSize)), fullscreenMat, 0);
    }

    private void OnDestroy()
    {
        Destroy(texture);
    }
}
