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
        float height = request.height - 1;
        float sizeX = Camera.main.orthographicSize * ((float)Screen.width / (float)Screen.height);
        float sizeY = Camera.main.orthographicSize;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float bl = (float)data[ y      * request.width + x    ] / 255f;
                float br = (float)data[ y      * request.width + x + 1] / 255f;
                float tr = (float)data[(y + 1) * request.width + x + 1] / 255f;
                float tl = (float)data[(y + 1) * request.width + x    ] / 255f;

                int index = 0;
                if (bl >= threshold) index |= 1;
                if (br >= threshold) index |= 2;
                if (tr >= threshold) index |= 4;
                if (tl >= threshold) index |= 8;
                if (index == 0) continue;

                cellPoints[4] = new Vector2(Mathf.InverseLerp(bl, br, threshold), 0f);
                cellPoints[5] = new Vector2(1f, Mathf.InverseLerp(br, tr, threshold));
                cellPoints[6] = new Vector2(Mathf.InverseLerp(tl, tr, threshold), 1f);
                cellPoints[7] = new Vector2(0f, Mathf.InverseLerp(bl, tl, threshold));

                foreach (int[] poly in cases[index])
                {
                    int baseIndex = verts.Count;

                    foreach (int p in poly)
                    {
                        float realX = ((x + cellPoints[p].x) /  width - 0.5f) * sizeX * 2f;
                        float realY = ((y + cellPoints[p].y) / height - 0.5f) * sizeY * 2f;
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
