using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Building
{
    public List<Layer> Layers;
    public int FloorCount;

    // Model representation
    public List<Vector3> Vertices;
    public List<Color> Colors;

    public Building() 
    {
        Layers = new List<Layer>();
        Vertices = new List<Vector3>();
        Colors = new List<Color>();
        FloorCount = 0;
    }

    public void UpdateVertexBuffer()
    {
        Vertices.Clear();
        Colors.Clear();

        foreach(var layer in Layers)
        {
            layer.AppendToVertexBuffer(ref Vertices, ref Colors);
        }

        Debug.Log("Updated building mesh with " + Vertices.Count + " vertices.");
    }

    public float FloorHeight
    {
        get
        {
            for (int n = 0; n < Layers.Count; n++)
            {
                foreach (var cfloor in Layers[n].Floors)
                {
                    float maxHeight = 0f;

                    foreach (var czone in cfloor.Zones)
                    {
                        foreach (var cvert in czone.Vertices)
                        {
                            if (cvert.y > maxHeight)
                                maxHeight = cvert.y;
                        }
                    }

                    if (maxHeight > 0.01f)
                        return maxHeight;
                }
            }

            return 0f;
        }
    }

    public string ContentsDescription 
    {
        get
        {
            string ret = "Contains " + Layers.Count + " layers:\n";

            for(int n = 0; n < Layers.Count; n++)
            {
                ret += "\tLayer '" + Layers[n].Name+ "' (i=" + n +") present in " + Layers[n].Floors.Count + " floors and contains ";

                int vertCount = 0;
                int zoneCount = 0;

                foreach(var cfloor in Layers[n].Floors)
                {
                    zoneCount += cfloor.Zones.Count;

                    foreach(var czone in cfloor.Zones)
                    {
                        vertCount += czone.Vertices.Count;
                    }
                }

                ret += zoneCount.ToString() + " zones and " + vertCount.ToString() + " vertices.\n";
            }

            return ret;
        }
    }
}