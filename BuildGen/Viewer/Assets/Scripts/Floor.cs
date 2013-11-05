using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Floor
{
    public List<Zone> Zones;
    public bool Enabled;

    public Floor() 
    {
        Zones = new List<Zone>();
        Enabled = true;
    }

    public void AppendToVertexBuffer(ref List<Vector3> outVerts)
    {
        if (Enabled)
        {
            foreach (var zone in Zones)
            {
                zone.AppendToVertexBuffer(ref outVerts);
            }
        }
    }
}