using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Zone
{
    public List<Vector3> Vertices;

    public Zone() { Vertices = new List<Vector3>(); }

    public void AppendToVertexBuffer(ref List<Vector3> outVerts)
    {
        for (int n = 0; n < Vertices.Count - 2; )
        {
            // Front face
            outVerts.Add(Vertices[n]);
            outVerts.Add(Vertices[n + 1]);
            outVerts.Add(Vertices[n + 2]);

            // Back face
            outVerts.Add(Vertices[n + 2]);
            outVerts.Add(Vertices[n + 1]);
            outVerts.Add(Vertices[n]);

            n += 3;
        }
    }
}