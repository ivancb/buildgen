using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Layer
{
    public List<Floor> Floors;
    public bool Enabled;
    public string Name;

    public Layer() 
    {
        Floors = new List<Floor>();
        Enabled = true;
        Name = "Unspecified";
    }

    public void AppendToVertexBuffer(ref List<Vector3> outVerts, ref List<Color> colors)
    {
        if(Enabled)
        {
            foreach (var floor in Floors)
            {
                int initialVertCount = outVerts.Count;
                floor.AppendToVertexBuffer(ref outVerts);
                int numAddedVertices = outVerts.Count - initialVertCount;

                Color color = new Color(0f, 0f, 0f);

                switch (Name)
                {
                    case "passages":
                        color.r = 1f;
                        break;
                    case "ceiling":
                        color.b = 1f;
                        break;
                    case "rooms":
                        color.g = 1f;
                        break;
                }

                for (int n = 0; n < numAddedVertices; n++)
                {
                    colors.Add(color);
                }
            }
        }
    }
}