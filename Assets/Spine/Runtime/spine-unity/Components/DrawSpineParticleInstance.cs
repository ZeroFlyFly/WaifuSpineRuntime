using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawSpineParticleInstance : MonoBehaviour
{
    // How many meshes to draw.
    public int population;
    // Range to draw meshes within.
    public float range;

    // Material to use for drawing the meshes.
    public Material material;

    // CPU.
    private Matrix4x4[][] matrices;
    private MaterialPropertyBlock block;

    private Mesh mesh;

    private bool isInited = false;

    private int instanceOnceMaxCount = 1023;

    private void Setup()
    {
        Mesh mesh = CreateQuad();
        this.mesh = mesh;

        int count = (int)(population * 1.0f / instanceOnceMaxCount) + 1;

        int lastNum = population - (count - 1) * instanceOnceMaxCount;

        matrices = new Matrix4x4[count][];

        for(int i = 0; i < matrices.Length; i++)
        {
            int currentCount = (i == (matrices.Length - 1)) ? lastNum : instanceOnceMaxCount;

            matrices[i] = new Matrix4x4[currentCount];
        }
        //block = new MaterialPropertyBlock();

        for (int i = 0; i < matrices.Length; i++)
        {
            for(int j = 0; j < matrices[i].Length; j++)
            {
                // Build matrix.
                Matrix4x4 mat = Matrix4x4.identity;
                Vector3 position = transform.position + new Vector3(Random.Range(-range, range), Random.Range(-range, range), Random.Range(-range, range));
                Quaternion rotation = Quaternion.identity;// Quaternion.Euler(Random.Range(-180, 180), Random.Range(-180, 180), Random.Range(-180, 180));
                Vector3 scale = Vector3.one;

                mat.SetTRS(position, rotation, scale);

                matrices[i][j] = mat;
            }
        }

        //block.SetVectorArray("_Colors", colors);
        isInited = true;
    }

    private Mesh CreateQuad(float width = 1f, float height = 1f)
    {
        // Create a quad mesh.
        var mesh = new Mesh();

        float w = width * .5f;
        float h = height * .5f;
        var vertices = new Vector3[4] {
            new Vector3(-w, -h, 0),
            new Vector3(w, -h, 0),
            new Vector3(-w, h, 0),
            new Vector3(w, h, 0)
        };

        var tris = new int[6] {
            // lower left tri.
            0, 2, 1,
            // lower right tri
            2, 3, 1
        };

        var normals = new Vector3[4] {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
        };

        var uv = new Vector2[4] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
        };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.normals = normals;
        mesh.uv = uv;

        return mesh;
    }

    private void Start()
    {
        if(material != null)
            Setup();
    }

    private void Update()
    {
        if (isInited)
        {
            for(int i = 0; i < matrices.Length; i++)
            {
                Graphics.DrawMeshInstanced(mesh, 0, material, matrices[i], matrices[i].Length, null);
            }
        }
    }
}