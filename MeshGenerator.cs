using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PDS;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    PoissonDiskSampler poissonDiskSampler;

    private Mesh mesh;
    [SerializeField] private Gradient gradient;
    private Texture2D noiseMapTexture;
    // private float density = 0.5f;
    [SerializeField] private GameObject prefab;

    private Vector3[] vertices;
    private int[] triangles;
    private Vector2[] uvs;
    private Color[] colors;

    [SerializeField] private int xSize = 32;
    [SerializeField] private int zSize = 32;

    private float minTerrainHeight;
    private float maxTerrainHeight;

    [SerializeField] private Texture2D[] _textures;
    private Texture2D _texture;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        CreateShape();
        UpdateMesh();

        GetComponent<MeshCollider>().sharedMesh = mesh;

        poissonDiskSampler = GetComponentInChildren<PoissonDiskSampler>();
        PlaceObjectsProcedurally();
    }

    private void UpdateMesh()
    {
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.colors = colors;

        mesh.RecalculateNormals();
    }

    private void PlaceObjectsProcedurally()
    {
        poissonDiskSampler.SnapToTerrain = true;
        poissonDiskSampler.Generate();
    }

    private void GenerateNoise()
    {
        int width = (int)Terrain.activeTerrain.terrainData.size.x;
        int height = (int)Terrain.activeTerrain.terrainData.size.y;
        float scale = 5;
        noiseMapTexture = Noise.GetNoiseMap(width, height, scale);
    }

    private void CreateShape()
    {
        // Choose randomly from several textures
        _texture = _textures[UnityEngine.Random.Range(0, _textures.Length)];
 
        Color32[] pixels = _texture.GetPixels32();

        int worldX = _texture.width;
        int worldZ = _texture.height;
        //

        // VERTICES
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float y;

                y = Mathf.PerlinNoise(x * .1f, z * .1f) * 2;

                // creating a wall around the paremeter
                if ((z == 0 ||  x ==0) || (z == zSize ||  x == xSize))
                {
                    y = 100f;
                }
                //

                vertices[i] = new Vector3(x, y, z);

                if (y > maxTerrainHeight)
                    maxTerrainHeight = y;
                if (y < minTerrainHeight)
                    minTerrainHeight = y;
                i++;
            }
        }

        // TESSELATION BASED ON PIXELS (it works but not aligning right became <= is not working)
        for (int i = 0, z = 0; z < worldZ; z++)
        {
            for (int x = 0; x < worldX; x++)
            {
                Color color = pixels[i];

                if (color.Equals(Color.white))
                {
                    // code to raise walls
                    vertices[i].y = 100f;
                }
                i++;
            }
        }

        // TRIANGLES
        triangles = new int[xSize * zSize * 6];

        int vert = 0;
        int tris = 0;

        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + xSize + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + xSize + 1;
                triangles[tris + 5] = vert + xSize + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }

        // UVS
        uvs = new Vector2[vertices.Length];

        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                uvs[i] = new Vector2((float)x / xSize, (float)z / zSize);
                i++;
            }
        }

        // GRADIENT BASED ON HEIGHT
        colors = new Color[vertices.Length];

        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float height = Mathf.InverseLerp(minTerrainHeight, maxTerrainHeight, vertices[i].y);
                colors[i] = gradient.Evaluate(height);
                i++;
            }
        }

    }
}
