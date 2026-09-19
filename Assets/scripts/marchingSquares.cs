using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class marchingSquares : MonoBehaviour
{
    [Header("getting the texture")]
    public RenderTexture dataTexture;

    [Header("marching squares")]
    public MeshFilter filter;
    Mesh marchedSquares;
    bool readbackInProgress = false;

    private void Awake()
    {
        marchedSquares = new Mesh();
        marchedSquares.name = "marched squares";
    }

    void Update()
    {
        if (!readbackInProgress)
        {
            readbackInProgress = true;
            AsyncGPUReadback.Request(dataTexture, 0, TextureFormat.R8, OnCompleteReadback);
        }
    }

    void OnCompleteReadback(AsyncGPUReadbackRequest request)
    {
        readbackInProgress = false;
        if (request.hasError)
        {
            Debug.LogWarning("GPU readback error detected.");
            return;
        }
        if (marchedSquares == null) return;

        NativeArray<byte> data = request.GetData<byte>();

        List<Vector3> verts = new List<Vector3>(0);
        List<int> tris = new List<int>(0);
        for (int y = 0; y < request.width; y++)
        {
            for (int x = 0; x < request.height; x++)
            {
                byte pixelColor = data[y * request.width + x];

                if (pixelColor == 0) continue;

                float sizeY = Camera.main.orthographicSize;
                float sizeX = sizeY * (Screen.width / Screen.height);
                float realX = ((float)x / request.width - 0.5f) * sizeX * 2f;
                float realY = ((float)y / request.height - 0.5f) * sizeY * 2f;

                float difX = 1f / request.width * sizeX * 2f;
                float difY = 1f / request.height * sizeY * 2f;
                int baseIndex = verts.Count;
                verts.Add(new Vector3(realX     , realY     ));
                verts.Add(new Vector3(realX+difX, realY     ));
                verts.Add(new Vector3(realX+difX, realY+difY));
                verts.Add(new Vector3(realX     , realY+difY));

                tris.Add(baseIndex + 0);
                tris.Add(baseIndex + 1);
                tris.Add(baseIndex + 2);
                tris.Add(baseIndex + 0);
                tris.Add(baseIndex + 2);
                tris.Add(baseIndex + 3);
            }
        }
        marchedSquares.vertices = verts.ToArray();
        marchedSquares.triangles = tris.ToArray();
        filter.mesh = marchedSquares;
    }
    private void OnDestroy()
    {
        Destroy(marchedSquares);
    }
}
