using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class UserInterface : MonoBehaviour 
{
    private enum UIMode
    {
        Initializing,
        BuildingSelection,
        BuildingDisplay,
    };

    private static readonly int BuildingFilesPerPage = 5;

    private int BuildingFilesPage;
    private List<string> AvailableBuildingFiles;
    private UIMode Mode;
    private string LastError;
    private string CurrentFileBase;
    private float FloorHeight;
    private int FloorIndex;
    private Texture FloorPlanTexture;

    public GUISkin Skin;

    public UserInterface()
    {
        AvailableBuildingFiles = new List<string>();
        Mode = UIMode.Initializing;
        LastError = "";
        CurrentFileBase = null;
        FloorHeight = 0f;
        FloorIndex = -1;
        FloorPlanTexture = null;
        BuildingFilesPage = 0;
    }

    void Update()
    {
        // NOTE: Building file list has to be done outside of the constructor
        // otherwise it triggers an access error due to the constructor
        // not being executed in the main unity thread.
        if (Mode == UIMode.Initializing)
        {
            Mode = UIMode.BuildingSelection;
            BuildFileList();

            Screen.lockCursor = false;
        }

        if (Input.GetButtonUp("Select Building") && (Mode != UIMode.BuildingSelection))
        {
            BuildFileList();
            LastError = "";
            Mode = UIMode.BuildingSelection;
        }
    }

    void OnGUI()
    {
        GUI.skin = Skin;

        if (Mode == UIMode.BuildingSelection)
            DrawBuildingSelectionList();
        else if (Mode == UIMode.BuildingDisplay)
            DrawLayerControls();

        if (GUI.Button(new Rect(25, Screen.height - 50, 100, 25), "Exit"))
            Application.Quit();
	}

    private void BuildFileList()
    {
        Debug.Log("Generating building file list from directory " + Application.dataPath + "/../Output/");

        var files = Directory.GetFiles(Application.dataPath + "/../Output/", "*.xml", SearchOption.AllDirectories);
        AvailableBuildingFiles.Clear();

        foreach (var v in files)
        {
            AvailableBuildingFiles.Add(v);
        }
    }

    private void DrawBuildingSelectionList()
    {
        GameObject buildingObj = GameObject.Find("Building");
        BuildingMeshGenerator buildGen = buildingObj.GetComponent<BuildingMeshGenerator>();

        int numStart = BuildingFilesPage * BuildingFilesPerPage;
        int numEnd = System.Math.Min(AvailableBuildingFiles.Count, numStart + BuildingFilesPerPage);

        for (int n = numStart; n < numEnd; n++)
        {
            string file = AvailableBuildingFiles[n];

            if (GUI.Button(new Rect(25, 25 + 35 * (n - numStart), 850, 25), "Load " + file))
            {
                if (!buildGen.Load(file))
                {
                    LastError = "Could not load the mesh at " + file;
                }
                else
                {
                    if (buildGen.RefreshMesh())
                    {
                        Mode = UIMode.BuildingDisplay;
                        Screen.lockCursor = true;
                        CurrentFileBase = System.IO.Path.GetDirectoryName(file) + "/" + System.IO.Path.GetFileNameWithoutExtension(file);
                        FloorHeight = buildGen.BuildingGenerated.FloorHeight;
                    }
                    else
                    {
                        LastError = "Mesh at " + file + " has no geometry data";
                    }
                }
            }
        }

        int numElements = System.Math.Max(numEnd - numStart, 0);

        if (AvailableBuildingFiles.Count == 0)
            GUI.Box(new Rect(25, 25, 300, 25), "No valid files found.");
        else
        {
            int numPages = (int)System.Math.Floor((double)AvailableBuildingFiles.Count / BuildingFilesPerPage);

            if ((BuildingFilesPage < numPages) && GUI.Button(new Rect(25, 25 + 35 * numElements, 100, 25), "Next Page"))
                BuildingFilesPage++;
            if ((BuildingFilesPage > 0) && GUI.Button(new Rect(25 + 125, 25 + 35 * numElements, 100, 25), "Previous Page"))
                BuildingFilesPage--;
        }

        if (LastError != "")
            GUI.Box(new Rect(25, Screen.height - 250, 300, 50), "Error: " + LastError);
    }

    private void DrawLayerControls()
    {
        GameObject buildingObj = GameObject.Find("Building");
        BuildingMeshGenerator buildGen = buildingObj.GetComponent<BuildingMeshGenerator>();

        if (buildGen.BuildingGenerated == null)
            return;

        // Layer toggles
        int layerIndex = 0;
        foreach (var layer in buildGen.BuildingGenerated.Layers)
        {
            if (GUI.Button(new Rect(25, 25 + 35 * layerIndex, 250, 25), "Toggle Layer " + layer.Name + (layer.Enabled ? " (Visible)" : " (Hidden)")))
            {
                layer.Enabled = !layer.Enabled;
                buildGen.RefreshMesh();
            }

            layerIndex++;
        }

        // Floor toggles
        for (int n = 0; n < buildGen.BuildingGenerated.FloorCount; n++)
        {
            if (GUI.Button(new Rect(Screen.width - 275, 25 + 35 * n, 250, 25), "Toggle Floor " + n))
            {
                foreach (var layer in buildGen.BuildingGenerated.Layers)
                {
                    if (layer.Floors.Count > n)
                    {
                        layer.Floors[n].Enabled = !layer.Floors[n].Enabled;
                    }
                }

                buildGen.RefreshMesh();
            }
        }

        // Floor plan image
        int nFloorIndex = (int)System.Math.Round((this.transform.position.y / 5f) / FloorHeight);

        if (nFloorIndex != FloorIndex)
        {
            string filename = CurrentFileBase + "_final_" + nFloorIndex + ".bmp";
            FloorIndex = nFloorIndex;

            if(System.IO.File.Exists(filename))
            {
                var www = new WWW("file://" + filename);
                FloorPlanTexture = www.texture;
            }
        }

        if (FloorPlanTexture != null)
            GUI.DrawTexture(new Rect(Screen.width - 125, Screen.height - 125, 100, 100), FloorPlanTexture);

        GUI.Box(new Rect(25, Screen.height - 250, 300, 100), "Controls:\n1 - Camera 1\n2 - Camera 2\nc - Change the currently loaded building\nt - Toggle mouselook\nr - Toggle material");
    }
}
