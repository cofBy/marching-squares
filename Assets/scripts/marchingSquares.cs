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
    [Range(0.001f, 0.999f)] public float threshold;
    Mesh marchedSquares;
    bool readbackInProgress = false;

    static readonly Vector2[] cellPoints =
    {
        new Vector2(0.0f, 0.0f),
        new Vector2(1.0f, 0.0f),
        new Vector2(1.0f, 1.0f),
        new Vector2(0.0f, 1.0f),
        new Vector2(0.5f, 0.0f),
        new Vector2(1.0f, 0.5f),
        new Vector2(0.5f, 1.0f),
        new Vector2(0.0f, 0.5f),
    };
    static readonly int[][][] cases =
    {
        new int[0][],
        new[] { new[] {0, 4, 7} },
        new[] { new[] {4, 1, 5} },
        new[] { new[] {0, 1, 5, 7} },
        new[] { new[] {5, 2, 6} },
        new[] { new[] {0, 4, 7}, new[] {5, 2, 6} },
        new[] { new[] {4, 1, 2, 6} },
        new[] { new[] {0, 1, 2, 6, 7} },
        new[] { new[] {7, 6, 3} },
        new[] { new[] {0, 4, 6, 3} },
        new[] { new[] {4, 1, 5}, new[] {7, 6, 3} },
        new[] { new[] {0, 1, 5, 6, 3} },
        new[] { new[] {7, 5, 2, 3} },
        new[] { new[] {0, 4, 5, 2, 3} },
        new[] { new[] {4, 1, 2, 3, 7} },
        new[] { new[] {0, 1, 2, 3} },
    };

    private void Awake()
    {
        marchedSquares = new Mesh();
        marchedSquares.name = "marched squares";
        marchedSquares.indexFormat = IndexFormat.UInt32;
        filter.mesh = marchedSquares;
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
        float width = request.width - 1;
        float height = request.width - 1;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte index = 0b00000000;
                if ((float)data[ y      * request.width + x    ] / 255f > threshold) index |= (byte)(1 << 0);
                if ((float)data[ y      * request.width + x + 1] / 255f > threshold) index |= (byte)(1 << 1);
                if ((float)data[(y + 1) * request.width + x + 1] / 255f > threshold) index |= (byte)(1 << 2);
                if ((float)data[(y + 1) * request.width + x    ] / 255f > threshold) index |= (byte)(1 << 3);
                if (index == 0) continue;

                foreach (int[] poly in cases[index])
                {
                    int baseIndex = verts.Count;

                    float sizeY = Camera.main.orthographicSize;
                    float sizeX = sizeY * (Screen.width / Screen.height);
                    foreach (int p in poly)
                    {
                        float realX = ((x + cellPoints[p].x) / width - 0.5f) * sizeX * 2;
                        float realY = ((y + cellPoints[p].y) / height - 0.5f) * sizeY * 2;
                        verts.Add(new Vector3(realX, realY));
                    }
                    for (int i = 1; i < poly.Length - 1; i++)
                    {
                        tris.Add(baseIndex);
                        tris.Add(baseIndex + i + 1);
                        tris.Add(baseIndex + i);
                    }
                }
            }
        }
        marchedSquares.Clear();
        marchedSquares.SetVertices(verts);
        marchedSquares.SetTriangles(tris, 0);
        marchedSquares.RecalculateBounds();
        marchedSquares.RecalculateNormals();
    }
    private void OnDestroy()
    {
        Destroy(marchedSquares);
    }
}
