using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;

public class BuildingMeshGenerator : MonoBehaviour {
    public float FloorHeight = 2f;

    private Building CurrentBuilding;

    public Building BuildingGenerated
    {
        get { return CurrentBuilding; }
    }

	void Start () 
    {
        CurrentBuilding = null;
	}

    public bool Load(string xmlSource)
    {
        if (xmlSource.Length == 0)
            return false;

        try
        {
            Debug.Log("Loading " + xmlSource);

            var document = XDocument.Load(xmlSource);
            CurrentBuilding = new Building();

            // <building><layer>
            foreach (var clayer in document.Descendants("layer"))
            {
                Layer curLayer = new Layer();
                curLayer.Name = clayer.Attribute("name").Value;

                // <building><layer><floor>
                foreach (var cfloor in clayer.Descendants("floor"))
                {
                    Floor curFloor = new Floor();

                    // <building><layer><floor><zone>
                    foreach (var czone in cfloor.Descendants("mesh"))
                    {
                        Zone latestZone = new Zone();

                        // <building><layer><floor><zone><triangle>
                        foreach (var ctriangle in czone.Descendants("triangle"))
                        {
                            // <building><layer><floor><zone><triangle><point>
                            foreach (var cpoint in ctriangle.Descendants("point"))
                            {
                                Vector3 nvec = new Vector3(float.Parse(cpoint.Attribute("x").Value, CultureInfo.InvariantCulture),
                                    float.Parse(cpoint.Attribute("y").Value, CultureInfo.InvariantCulture),
                                    float.Parse(cpoint.Attribute("z").Value, CultureInfo.InvariantCulture));
                                latestZone.Vertices.Add(nvec);
                            }
                        }

                        curFloor.Zones.Add(latestZone);
                    }

                    curLayer.Floors.Add(curFloor);
                }

                if(curLayer.Floors.Count > CurrentBuilding.FloorCount)
                {
                    CurrentBuilding.FloorCount = curLayer.Floors.Count;
                }
                CurrentBuilding.Layers.Add(curLayer);
            }

            Debug.Log("Successfully loaded the building mesh file. " + CurrentBuilding.ContentsDescription);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.Log(e.Message);
            return false;
        }
    }

    public bool RefreshMesh()
    {
        if (CurrentBuilding == null)
        {
            Debug.Log("No mesh data available");
            return false;
        }

        CurrentBuilding.UpdateVertexBuffer();
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("MeshFilter not found");
            return false;
        }

        // Create a mesh if there's none assigned yet
        if (meshFilter.mesh == null)
            meshFilter.mesh = new Mesh();

        Mesh mesh = meshFilter.sharedMesh;
        mesh.Clear();

        mesh.vertices = CurrentBuilding.Vertices.ToArray();
        mesh.colors = CurrentBuilding.Colors.ToArray();

        // Generate the triangle array
        int[] triangles = new int[CurrentBuilding.Vertices.Count];

        for (int n = CurrentBuilding.Vertices.Count - 1, startPos = CurrentBuilding.Vertices.Count - 1; n >= 0; n--)
        {
            triangles[startPos - n] = n;
        }

        mesh.SetIndices(triangles, MeshTopology.Triangles, 0);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.Optimize();

        MeshCollider collider = GetComponent<MeshCollider>();
        collider.sharedMesh = mesh;

        return true;
    }
}
