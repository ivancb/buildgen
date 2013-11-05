using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using BuildGen.Data;

namespace BuildGen.IO
{
    public class BuildingXmlIo
    {
        private enum WallFeature
        {
            None,
            Doorway,
            Window,
        }

        static readonly private int MinimumTilesPerWindow = 3;
        static readonly private int AverageTilesPerWindow = 3;
        static readonly private float FloorHeightScalingFactor = 2f;
        static readonly private float InteriorOffset = 0.02f;
        static readonly private string SchemaName = "InputSchema";
        static readonly private string SchemaPath = "Schemas/InputSchema.xsd";
        
        private string ErrMessage = "";

        public bool PerformValidation { get; set; }
        public string ErrorMessage { get { return ErrMessage; } }

        public BuildingXmlIo()
        {
            ErrMessage = "";
            PerformValidation = true;
        }

        public bool ExportBuildingMesh(Building bld, string filename)
        {
            var document = new XDocument();

            try
            {
                var editorElement = new XElement("building");
                editorElement.SetAttributeValue("seed", bld.Seed);
                editorElement.SetAttributeValue("constraintset", "tmp");
                editorElement.SetAttributeValue("floorcount", bld.Floors.Count);

                Dictionary<int, List<int>> generatedWindows = null;
                editorElement.Add(ExportPassagesLayer(bld));
                editorElement.Add(ExportRoomsLayer(bld, out generatedWindows));
                editorElement.Add(ExportCeilingLayer(bld));
                editorElement.Add(ExportExteriorWallLayer(bld, generatedWindows));
                editorElement.Add(ExportInteriorFloorLayer(bld));

                document.Add(editorElement);
                document.Save(filename);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while exporting xml: " + e.Message);
                return false;
            }
        }

        public string ExportBuildingDescription(Building bld)
        {
            if ((bld == null) || (bld.Floors.Count() == 0) || !bld.Valid)
            {
                ErrMessage = "Argument is null or contains invalid data.";
                return null;
            }

            string xml = "<?xml version='1.0'?>\n";

            // <building>
            FloorplanGrid baseGrid = bld.Floors[0].Grid;
            xml += "<building xmlns=\"InputSchema\"";
            if (bld.Seed != 0)
                xml += " seed=\"" + bld.Seed + "\"";
            if (!string.IsNullOrEmpty(bld.ConstraintSet))
                xml += " constraints=\"" + bld.ConstraintSet + "\"";
            xml += " floorcount=\"" + bld.Floors.Count() + "\"";
            xml += " floorwidth=\"" + bld.Width.ToString(CultureInfo.InvariantCulture) + "\"";
            xml += " floorheight=\"" + bld.Height.ToString(CultureInfo.InvariantCulture) + "\"";
            xml += " gridresolution=\"" + bld.Resolution.ToString(CultureInfo.InvariantCulture) + "\"";
            xml += ">\n";

            // <floorplans>
            xml += "<floorplans>\n";
            foreach (var floor in bld.Floors)
            {
                // <floor>
                xml += "<floor>\n";

                // <subbounds>
                // NOTE: This method's used instead of simply writing the region's operation bounds because
                // this way we only need to store the final subtract operations for a region.
                var subbounds = floor.Grid.FindRectangles(FloorTileType.Unavailable);
                xml += "<subbounds>\n";
                foreach (var rect in subbounds)
                {
                    xml += string.Format("<rect x=\"{0}\" y=\"{1}\" xx=\"{2}\" yy=\"{3}\"/>\n",
                        rect.X.ToString(CultureInfo.InvariantCulture), rect.Y.ToString(CultureInfo.InvariantCulture),
                        rect.XX.ToString(CultureInfo.InvariantCulture), rect.YY.ToString(CultureInfo.InvariantCulture));
                }
                xml += "</subbounds>\n";

                // <entrance>
                foreach (var entrance in floor.Entrances)
                {
                    xml += string.Format("<entrance x=\"{0}\" y=\"{1}\" type=\"{2}\" direction=\"{3}\"/>\n",
                        entrance.GridPosition.X.ToString(CultureInfo.InvariantCulture),
                        entrance.GridPosition.Y.ToString(CultureInfo.InvariantCulture), entrance.Type, entrance.Direction);
                }

                xml += "</floor>\n";
            }
            xml += "</floorplans>\n";

            xml += "</building>";
            return xml;
        }

        public Building Read(string filepath)
        {
            try
            {
                // Initialize the reader with validation settings if requested
                bool validXml = true;
                string validationMessage = "";
                XmlReaderSettings settings = new XmlReaderSettings();

                if (!PerformValidation)
                {
                    settings.ValidationType = ValidationType.None;
                }
                else
                {
                    settings.Schemas.Add(SchemaName, SchemaPath);
                    settings.ValidationType = ValidationType.Schema;
                    settings.ValidationFlags = XmlSchemaValidationFlags.ProcessSchemaLocation | XmlSchemaValidationFlags.ReportValidationWarnings;
                    settings.ValidationEventHandler += new ValidationEventHandler(delegate(object sender, ValidationEventArgs eargs)
                    {
                        validXml = false;
                        validationMessage += eargs.Message + " Severity: " + eargs.Severity.ToString() + "\n";
                    });
                }

                var reader = XmlTextReader.Create(filepath, settings);

                while (reader.Read()) ;

                if (!validXml)
                {
                    ErrMessage = validationMessage;
                    return null;
                }
                else
                {
                    XDocument doc = XDocument.Load(filepath);

                    if (doc.Root.Name.LocalName == "building")
                    {
                        ErrMessage = "";
                        return ParseBuilding(doc.Root);
                    }
                    else
                    {
                        ErrMessage = "Document root is invalid (expected <building> got <" + doc.Root.Name.LocalName + ">)";
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                ErrMessage = e.Message;
                return null;
            }
        }

        public Building Parse(string text)
        {
            try
            {
                // Initialize the reader with validation settings if requested
                bool validXml = true;
                string validationMessage = "";
                XmlReaderSettings settings = new XmlReaderSettings();

                if (!PerformValidation)
                {
                    settings.ValidationType = ValidationType.None;
                }
                else
                {
                    settings.Schemas.Add(SchemaName, SchemaPath);
                    settings.ValidationType = ValidationType.Schema;
                    settings.ValidationFlags = XmlSchemaValidationFlags.ProcessSchemaLocation | XmlSchemaValidationFlags.ReportValidationWarnings;
                    settings.ValidationEventHandler += new ValidationEventHandler(delegate(object sender, ValidationEventArgs eargs)
                    {
                        validXml = false;
                        validationMessage += eargs.Message + " Severity: " + eargs.Severity.ToString() + "\n";
                    });
                }

                var reader = XmlTextReader.Create(new StringReader(text), settings);

                while (reader.Read()) ;

                if (!validXml)
                {
                    ErrMessage = validationMessage;
                    return null;
                }
                else
                {
                    XDocument doc = XDocument.Parse(text);

                    if (doc.Root.Name.LocalName == "building")
                    {
                        ErrMessage = "";
                        return ParseBuilding(doc.Root);
                    }
                    else
                    {
                        ErrMessage = "Document root is invalid (expected <building> got <" + doc.Root.Name.LocalName + ">)";
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                ErrMessage = e.Message;
                return null;
            }
        }

        private Building ParseBuilding(XElement element)
        {
            // Confirm that we have enough data
            XAttribute floorCount = element.Attribute("floorcount");
            XAttribute floorWidth = element.Attribute("floorwidth");
            XAttribute floorHeight = element.Attribute("floorheight");
            XAttribute resolution = element.Attribute("gridresolution");

            if ((floorCount == null) || (floorWidth == null) || 
                (floorHeight == null) || (resolution == null))
                return null;

            Building res = new Building(float.Parse(floorWidth.Value, CultureInfo.InvariantCulture),
                float.Parse(floorHeight.Value, CultureInfo.InvariantCulture),
                float.Parse(resolution.Value, CultureInfo.InvariantCulture));

            // Create the various floors
            for (int n = 0; n < int.Parse(floorCount.Value); n++)
            {
                res.AddFloor();
            }

            // Set the building's generation parameters if any
            XAttribute seed = element.Attribute("seed");
            XAttribute constraints = element.Attribute("constraints");

            if (seed != null)
                res.Seed = int.Parse(seed.Value);
            if (constraints != null)
                res.ConstraintSet = constraints.Value;

            // Parse the floorplans belonging to this building
            int curFloorIndex = 0;
            foreach (var childElement in element.Elements())
            {
                if (childElement.Name.LocalName == "floorplans")
                {
                    foreach (var floorplanElement in childElement.Elements())
                    {
                        if (floorplanElement.Name.LocalName == "floor")
                        {
                            Floor cfloor = res.Floors[curFloorIndex];

                            if (!ParseFloor(floorplanElement, ref cfloor))
                                return null;

                            curFloorIndex++;
                        }
                    }
                }
            }

            // Confirm that we read the correct number of floors
            if (curFloorIndex != res.Floors.Count())
            {
                ErrMessage = "Incorrect number of floors (expected " + res.Floors.Count() + " got " + curFloorIndex + ")";
            }

            // Validate the data we obtained before returning just in case
            if (res.Valid)
            {
                return res;
            }
            else
            {
                ErrMessage = "Invalid building";
                return null;
            }
        }

        /// 
        /// <summary>
        /// Parses the contents of a <floor> XML tag and updates the provided floor with the parsed data
        /// </summary>
        /// <param name="element">XElement that points to a <floor> tag</param>
        /// <param name="floor">The floor that's being parsed</param>
        /// <returns>True if no errors occurred, false otherwise</returns>
        private bool ParseFloor(XElement element, ref Floor floor)
        {
            foreach (var childElement in element.Elements())
            {
                switch (childElement.Name.LocalName)
                {
                    case "subbounds":
                        if (!ParseSubtractedBounds(childElement, ref floor))
                        {
                            return false;
                        }
                        break;
                    case "entrance":
                        if (!ParseEntrance(childElement, ref floor))
                        {
                            return false;
                        }
                        break;
                }
            }

            return true;
        }

        /// <summary>
        /// Parses the contents of a <subbounds> XML tag. A <subbounds> tag is a set of <rect> tags that define
        /// a valid area for a floorplan.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="floor"></param>
        /// <returns></returns>
        private bool ParseSubtractedBounds(XElement element, ref Floor floor)
        {
            foreach (var childElement in element.Elements())
            {
                if (childElement.Name.LocalName == "rect")
                {
                    XAttribute xAttrib = childElement.Attribute("x");
                    XAttribute yAttrib = childElement.Attribute("y");
                    XAttribute xxAttrib = childElement.Attribute("xx");
                    XAttribute yyAttrib = childElement.Attribute("yy");

                    if ((xAttrib != null) && (yAttrib != null) &&
                        (xxAttrib != null) && (yyAttrib != null))
                    {
                        int x = int.Parse(xAttrib.Value, CultureInfo.InvariantCulture);
                        int y = int.Parse(yAttrib.Value, CultureInfo.InvariantCulture);
                        int xx = int.Parse(xxAttrib.Value, CultureInfo.InvariantCulture);
                        int yy = int.Parse(yyAttrib.Value, CultureInfo.InvariantCulture);

                        floor.Grid.Set(x, y, xx, yy, FloorTileType.Unavailable);
                    }
                }
            }

            return true;
        }

        private bool ParseEntrance(XElement element, ref Floor floor)
        {
            XAttribute xAttrib = element.Attribute("x");
            XAttribute yAttrib = element.Attribute("y");
            XAttribute typeAttrib = element.Attribute("type");
            XAttribute directionAttrib = element.Attribute("direction");

            if ((xAttrib == null) || (yAttrib == null) ||
                (typeAttrib == null) || (directionAttrib == null))
            {
                return false;
            }

            floor.AddEntrance(int.Parse(xAttrib.Value, CultureInfo.InvariantCulture),
                int.Parse(yAttrib.Value, CultureInfo.InvariantCulture),
                (EntranceType)Enum.Parse(typeof(EntranceType), typeAttrib.Value), (Direction)Enum.Parse(typeof(Direction), directionAttrib.Value));

            return true;
        }

        private static XElement ExportPassagesLayer(Building bld)
        {
            XElement ret = new XElement("layer");
            ret.SetAttributeValue("name", Enum.GetName(typeof(Layer), Layer.Passages).ToLowerInvariant());

            for (int n = 0; n < bld.Floors.Count; n++)
            {
                XElement floorElement = new XElement("floor");
                FloorplanGrid grid = bld.Floors[n].Grid;
                float floorHeight = bld.Resolution * FloorHeightScalingFactor;

                // Export the floor bounds
                XElement meshElement = new XElement("mesh");
                XElement triangleElement = null;

                for (int tileIndex = 0; tileIndex < grid.TileCount; tileIndex++)
                {
                    if ((grid[tileIndex] == FloorTileType.Passage) || (grid[tileIndex] == FloorTileType.Other))
                    {
                        meshElement.Add(new XComment("Tile " + tileIndex));
                        
                        Point cPos = grid.ToPosition(tileIndex);
                        Entrance nEntrance = bld.Floors[n].GetEntranceAt(cPos.X, cPos.Y);
                        PointF realPos = new PointF(cPos.X * bld.Resolution, cPos.Y * bld.Resolution);

                        bool[] walls = new bool[4] { false, false, false, false };
                        int entranceWall = -1;

                        for (int cdir = 0; cdir < 4; cdir++)
                        {
                            if ((nEntrance != null) && (nEntrance.Type == EntranceType.Passage) && (cdir.ToDirection() == nEntrance.Direction.GetOpposite()))
                            {
                                entranceWall = nEntrance.Direction.GetOpposite().ToNumber();
                                continue;
                            }

                            Point offsetPos = cPos.Advance(cdir.ToDirection(), 1);
                            int nIndex = grid.ToIndex(offsetPos.X, offsetPos.Y);

                            if ((nIndex == -1) || ((grid[nIndex] != FloorTileType.Passage) && (grid[nIndex] != FloorTileType.Other)))
                            {
                                walls[cdir] = true;
                            }
                        }

                        // Floor
                        if (nEntrance == null)
                        {
                            triangleElement = new XElement("triangle");
                            triangleElement.Add(CreateXmlPoint(realPos.X, n * floorHeight + InteriorOffset, realPos.Y));
                            triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, n * floorHeight + InteriorOffset, realPos.Y));
                            triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, n * floorHeight + InteriorOffset, realPos.Y + bld.Resolution));
                            meshElement.Add(triangleElement);

                            triangleElement = new XElement("triangle");
                            triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, n * floorHeight + InteriorOffset, realPos.Y + bld.Resolution));
                            triangleElement.Add(CreateXmlPoint(realPos.X, n * floorHeight + InteriorOffset, realPos.Y + bld.Resolution));
                            triangleElement.Add(CreateXmlPoint(realPos.X, n * floorHeight + InteriorOffset, realPos.Y));
                            meshElement.Add(triangleElement);
                        }
                        else
                        {
                            PointF origin = realPos;
                            PointF end = origin.Advance(nEntrance.Direction, -1f);

                            switch(nEntrance.Direction)
                            {
                                case Direction.West:
                                    triangleElement = new XElement("triangle");
                                    triangleElement.Add(CreateXmlPoint(realPos.X, n * floorHeight + InteriorOffset, realPos.Y));
                                    triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, (n + 1) * floorHeight + InteriorOffset, realPos.Y));
                                    triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, (n + 1) * floorHeight + InteriorOffset, realPos.Y + bld.Resolution));
                                    meshElement.Add(triangleElement);

                                    triangleElement = new XElement("triangle");
                                    triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, (n + 1) * floorHeight + InteriorOffset, realPos.Y + bld.Resolution));
                                    triangleElement.Add(CreateXmlPoint(realPos.X, n * floorHeight + InteriorOffset, realPos.Y + bld.Resolution));
                                    triangleElement.Add(CreateXmlPoint(realPos.X, n * floorHeight + InteriorOffset, realPos.Y));
                                    meshElement.Add(triangleElement);
                                    break;
                                case Direction.East:
                                    triangleElement = new XElement("triangle");
                                    triangleElement.Add(CreateXmlPoint(realPos.X, (n + 1) * floorHeight + InteriorOffset, realPos.Y));
                                    triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, n * floorHeight + InteriorOffset, realPos.Y));
                                    triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, n * floorHeight + InteriorOffset, realPos.Y + bld.Resolution));
                                    meshElement.Add(triangleElement);

                                    triangleElement = new XElement("triangle");
                                    triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, n * floorHeight + InteriorOffset, realPos.Y + bld.Resolution));
                                    triangleElement.Add(CreateXmlPoint(realPos.X, (n + 1) * floorHeight + InteriorOffset, realPos.Y + bld.Resolution));
                                    triangleElement.Add(CreateXmlPoint(realPos.X, (n + 1) * floorHeight + InteriorOffset, realPos.Y));
                                    meshElement.Add(triangleElement);
                                    break;
                                case Direction.South:
                                    triangleElement = new XElement("triangle");
                                    triangleElement.Add(CreateXmlPoint(realPos.X, (n + 1) * floorHeight + InteriorOffset, realPos.Y));
                                    triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, (n + 1) * floorHeight + InteriorOffset, realPos.Y));
                                    triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, n * floorHeight + InteriorOffset, realPos.Y + bld.Resolution));
                                    meshElement.Add(triangleElement);

                                    triangleElement = new XElement("triangle");
                                    triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, n * floorHeight + InteriorOffset, realPos.Y + bld.Resolution));
                                    triangleElement.Add(CreateXmlPoint(realPos.X, n * floorHeight + InteriorOffset, realPos.Y + bld.Resolution));
                                    triangleElement.Add(CreateXmlPoint(realPos.X, (n + 1) * floorHeight + InteriorOffset, realPos.Y));
                                    meshElement.Add(triangleElement);
                                    break;
                            }
                        }

                        // North walls
                        if (walls[0])
                        {
                            meshElement.Add(new XComment("North"));

                            triangleElement = new XElement("triangle");
                            triangleElement.Add(CreateXmlPoint(realPos.X, n * floorHeight, realPos.Y));
                            triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, n * floorHeight, realPos.Y));
                            triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, (n + 1) * floorHeight, realPos.Y));
                            meshElement.Add(triangleElement);

                            triangleElement = new XElement("triangle");
                            triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, (n + 1) * floorHeight, realPos.Y));
                            triangleElement.Add(CreateXmlPoint(realPos.X, (n + 1) * floorHeight, realPos.Y));
                            triangleElement.Add(CreateXmlPoint(realPos.X, n * floorHeight, realPos.Y));
                            meshElement.Add(triangleElement);
                        }

                        // South walls
                        if (walls[1])
                        {
                            meshElement.Add(new XComment("South"));

                            triangleElement = new XElement("triangle");
                            triangleElement.Add(CreateXmlPoint(realPos.X, n * floorHeight, realPos.Y + bld.Resolution));
                            triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, n * floorHeight, realPos.Y + bld.Resolution));
                            triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, (n + 1) * floorHeight, realPos.Y + bld.Resolution));
                            meshElement.Add(triangleElement);

                            triangleElement = new XElement("triangle");
                            triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, (n + 1) * floorHeight, realPos.Y + bld.Resolution));
                            triangleElement.Add(CreateXmlPoint(realPos.X, (n + 1) * floorHeight, realPos.Y + bld.Resolution));
                            triangleElement.Add(CreateXmlPoint(realPos.X, n * floorHeight, realPos.Y + bld.Resolution));
                            meshElement.Add(triangleElement);
                        }

                        // West walls
                        if (walls[2])
                        {
                            meshElement.Add(new XComment("West"));

                            triangleElement = new XElement("triangle");
                            triangleElement.Add(CreateXmlPoint(realPos.X, n * floorHeight, realPos.Y));
                            triangleElement.Add(CreateXmlPoint(realPos.X, n * floorHeight, realPos.Y + bld.Resolution));
                            triangleElement.Add(CreateXmlPoint(realPos.X, (n + 1) * floorHeight, realPos.Y + bld.Resolution));
                            meshElement.Add(triangleElement);

                            triangleElement = new XElement("triangle");
                            triangleElement.Add(CreateXmlPoint(realPos.X, (n + 1) * floorHeight, realPos.Y + bld.Resolution));
                            triangleElement.Add(CreateXmlPoint(realPos.X, (n + 1) * floorHeight, realPos.Y));
                            triangleElement.Add(CreateXmlPoint(realPos.X, n * floorHeight, realPos.Y));
                            meshElement.Add(triangleElement);
                        }

                        // East walls
                        if (walls[3])
                        {
                            meshElement.Add(new XComment("East"));

                            triangleElement = new XElement("triangle");
                            triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, n * floorHeight, realPos.Y));
                            triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, n * floorHeight, realPos.Y + bld.Resolution));
                            triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, (n + 1) * floorHeight, realPos.Y + bld.Resolution));
                            meshElement.Add(triangleElement);

                            triangleElement = new XElement("triangle");
                            triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, (n + 1) * floorHeight, realPos.Y + bld.Resolution));
                            triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, (n + 1) * floorHeight, realPos.Y));
                            triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, n * floorHeight, realPos.Y));
                            meshElement.Add(triangleElement);
                        }

                        // Floor
                        meshElement.Add(new XComment("Floor"));

                        triangleElement = new XElement("triangle");
                        triangleElement.Add(CreateXmlPoint(realPos.X, n * floorHeight + InteriorOffset, realPos.Y));
                        triangleElement.Add(CreateXmlPoint(realPos.X, n * floorHeight + InteriorOffset, realPos.Y + bld.Resolution));
                        triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, n * floorHeight + InteriorOffset, realPos.Y + bld.Resolution));
                        meshElement.Add(triangleElement);

                        triangleElement = new XElement("triangle");
                        triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, n * floorHeight + InteriorOffset, realPos.Y + bld.Resolution));
                        triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, n * floorHeight + InteriorOffset, realPos.Y));
                        triangleElement.Add(CreateXmlPoint(realPos.X, n * floorHeight + InteriorOffset, realPos.Y));
                        meshElement.Add(triangleElement);
                    }
                }

                floorElement.Add(meshElement);
                ret.Add(floorElement);
            }

            return ret;
        }

        private static XElement ExportRoomsLayer(Building bld, out Dictionary<int, List<int>> generatedWindowTiles)
        {
            generatedWindowTiles = new Dictionary<int, List<int>>();
            XElement ret = new XElement("layer");
            ret.SetAttributeValue("name", Enum.GetName(typeof(Layer), Layer.Rooms).ToLowerInvariant());

            for (int n = 0; n < bld.Floors.Count; n++)
            {
                List<int> windowTiles = new List<int>();
                XElement floorElement = new XElement("floor");
                Floor floor = bld.Floors[n];
                float floorHeight = bld.Resolution * FloorHeightScalingFactor;

                // Export the floor bounds
                XElement meshElement = new XElement("mesh");

                foreach (var room in floor.Rooms)
                {
                    if (room.Children.Count() != 0)
                    {
                        foreach (var childRoom in room.Children)
                            ExportRoom(bld, n, ref windowTiles, floor, floorHeight, ref meshElement, childRoom);
                    }
                    else
                    {
                        ExportRoom(bld, n, ref windowTiles, floor, floorHeight, ref meshElement, room);
                    }
                }

                floorElement.Add(meshElement);
                ret.Add(floorElement);
                generatedWindowTiles[n] = windowTiles;
            }

            return ret;
        }

        private static void ExportRoom(Building bld, int n, ref List<int> windowTiles, Floor floor, float floorHeight, ref XElement meshElement, Room room)
        {
            PointF start = new PointF(room.TopLeft.X * bld.Resolution + InteriorOffset, room.TopLeft.Y * bld.Resolution + InteriorOffset);
            PointF end = new PointF(room.BottomRight.X * bld.Resolution - InteriorOffset, room.BottomRight.Y * bld.Resolution - InteriorOffset);

            int numHorizontalTiles = room.BottomRight.X - room.TopLeft.X;
            int numVerticalTiles = room.BottomRight.Y - room.TopLeft.Y;
            var validWindowTilesNorth = GenerateListValidWindowTiles(room.TopLeft, numHorizontalTiles, floor, Direction.East, Direction.North);
            var validWindowTilesWest = GenerateListValidWindowTiles(room.TopLeft, numVerticalTiles, floor, Direction.South, Direction.West);
            var validWindowTilesSouth = GenerateListValidWindowTiles(new Point(room.TopLeft.X, room.BottomRight.Y), numHorizontalTiles, floor, Direction.East, Direction.South);
            var validWindowTilesEast = GenerateListValidWindowTiles(new Point(room.BottomRight.X, room.TopLeft.Y), numVerticalTiles, floor, Direction.South, Direction.East);

            GenerateWall(floor, room.TopLeft, n, floorHeight, start, end, validWindowTilesNorth, Direction.North, ref meshElement, ref windowTiles);
            GenerateWall(floor, new Point(room.TopLeft.X, room.BottomRight.Y - 1), n, floorHeight, start, end, validWindowTilesSouth, Direction.South, ref meshElement, ref windowTiles);
            GenerateWall(floor, room.TopLeft, n, floorHeight, start, end, validWindowTilesWest, Direction.West, ref meshElement, ref windowTiles);
            GenerateWall(floor, new Point(room.BottomRight.X - 1, room.TopLeft.Y), n, floorHeight, start, end, validWindowTilesEast, Direction.East, ref meshElement, ref windowTiles);
            GenerateFloor(n, floorHeight, start, end, ref meshElement);
        }

        private static List<int> GenerateListValidWindowTiles(Point start, int numTiles, Floor floor, Direction expansionDirection, Direction windowDirection)
        {
            List<int> validWindowTileOffsets = new List<int>();
            Point pos = start;
            bool[] prevTileValid = new bool[2] { false, false };

            for (int n = 0; n < numTiles; n++)
            {
                Point windowPos = pos.Advance(windowDirection, 1);
                bool curTileValid = floor.Grid.CheckUnavailable(windowPos.X, windowPos.Y);

                if (curTileValid && prevTileValid[0] && prevTileValid[1])
                {
                    validWindowTileOffsets.Add(n - 1);
                }

                prevTileValid[1] = prevTileValid[0];
                prevTileValid[0] = curTileValid;
                pos = pos.Advance(expansionDirection, 1);
            }

            return validWindowTileOffsets;
        }

        private static void GenerateFloor(int floorIndex, float floorHeight, PointF start, PointF end, ref XElement meshElement)
        {
            meshElement.Add(new XComment("Floor"));

            XElement triangleElement = new XElement("triangle");
            triangleElement.Add(CreateXmlPoint(start.X, floorIndex * floorHeight + InteriorOffset, start.Y));
            triangleElement.Add(CreateXmlPoint(start.X, floorIndex * floorHeight + InteriorOffset, end.Y));
            triangleElement.Add(CreateXmlPoint(end.X, floorIndex * floorHeight + InteriorOffset, end.Y));
            meshElement.Add(triangleElement);

            triangleElement = new XElement("triangle");
            triangleElement.Add(CreateXmlPoint(end.X, floorIndex * floorHeight + InteriorOffset, end.Y));
            triangleElement.Add(CreateXmlPoint(end.X, floorIndex * floorHeight + InteriorOffset, start.Y));
            triangleElement.Add(CreateXmlPoint(start.X, floorIndex * floorHeight + InteriorOffset, start.Y));
            meshElement.Add(triangleElement);
        }

        private static void GenerateWall(Floor floor, Point origin, int floorIndex, float floorHeight, PointF start, PointF end, List<int> validWindowTiles, Direction wallFacing, 
            ref XElement meshElement, ref List<int> generatedWindowTileIndices)
        {
            PointF modStart;
            PointF modEnd;

            switch (wallFacing)
            {
                case Direction.West:
                    modStart.X = start.X;
                    modStart.Y = start.Y;
                    modEnd.X = start.X;
                    modEnd.Y = end.Y;
                    break;
                case Direction.East:
                    modStart.X = end.X;
                    modStart.Y = start.Y;
                    modEnd.X = end.X;
                    modEnd.Y = end.Y;
                    break;
                case Direction.North:
                    modStart.X = start.X;
                    modStart.Y = start.Y;
                    modEnd.X = end.X;
                    modEnd.Y = start.Y;
                    break;
                case Direction.South:
                    modStart.X = start.X;
                    modStart.Y = end.Y;
                    modEnd.X = end.X;
                    modEnd.Y = end.Y;
                    break;
                default:
                    throw new ArgumentException();
            }

            meshElement.Add(new XComment("Wall facing " + Enum.GetName(typeof(Direction), wallFacing)));

            float finalFloorHeight = floorIndex * floorHeight;
            float finalCeilingHeight = (floorIndex + 1) * floorHeight;

            if (validWindowTiles.Count() > 0)
            {
                float buildingResolution = floorHeight / FloorHeightScalingFactor;

                Direction deltaDirection;
                PointF delta;
                switch (wallFacing)
                {
                    case Direction.West:
                        delta.X = 0f;
                        delta.Y = buildingResolution;
                        deltaDirection = Direction.South;
                        break;
                    case Direction.East:
                        delta.X = 0f;
                        delta.Y = buildingResolution;
                        deltaDirection = Direction.South;
                        break;
                    case Direction.North:
                        delta.X = buildingResolution;
                        delta.Y = 0f;
                        deltaDirection = Direction.East;
                        break;
                    case Direction.South:
                        delta.X = buildingResolution;
                        delta.Y = 0f;
                        deltaDirection = Direction.East;
                        break;
                    default:
                        throw new ArgumentException();
                }

                PointF cStart = modStart;
                PointF cEnd = new PointF(cStart.X + validWindowTiles[0] * delta.X, cStart.Y + validWindowTiles[0] * delta.Y);

                AppendRectangle(cStart.X, cStart.Y, finalFloorHeight, cEnd.X, cEnd.Y, finalCeilingHeight, ref meshElement);
                cStart = cEnd;

                float windowBottomHeight = finalCeilingHeight - floorHeight * 0.5f;
                float windowTopHeight = finalCeilingHeight - floorHeight * 0.2f;

                bool skipNext = false;
                foreach (int tileOffset in validWindowTiles)
                {
                    cEnd.X = modStart.X + (tileOffset + 1) * delta.X;
                    cEnd.Y = modStart.Y + (tileOffset + 1) * delta.Y;

                    if (skipNext)
                    {
                        AppendRectangle(cStart.X, cStart.Y, finalFloorHeight, cEnd.X, cEnd.Y, finalCeilingHeight, ref meshElement);
                        skipNext = false;
                    }
                    else
                    {
                        Point nPos = origin.Advance(deltaDirection, tileOffset);
                        generatedWindowTileIndices.Add(floor.Grid.ToIndex(nPos.X, nPos.Y));
                        AppendRectangle(cStart.X, cStart.Y, finalFloorHeight, cEnd.X, cEnd.Y, windowBottomHeight, ref meshElement);
                        AppendRectangle(cStart.X, cStart.Y, windowTopHeight, cEnd.X, cEnd.Y, finalCeilingHeight, ref meshElement);
                        skipNext = true;
                    }

                    cStart = cEnd;
                }

                AppendRectangle(cStart.X, cStart.Y, finalFloorHeight, modEnd.X, modEnd.Y, finalCeilingHeight, ref meshElement);
            }
            else
            {
                AppendRectangle(modStart.X, modStart.Y, finalFloorHeight, modEnd.X, modEnd.Y, finalCeilingHeight, ref meshElement);
            }
        }

        private static void GenerateWallTile(int floorIndex, float floorHeight, PointF start, PointF end, WallFeature feature, Direction wallFacing, ref XElement meshElement)
        {
            PointF modStart;
            PointF modEnd;

            switch (wallFacing)
            {
                case Direction.West:
                    modStart.X = start.X;
                    modStart.Y = start.Y;
                    modEnd.X = start.X;
                    modEnd.Y = end.Y;
                    break;
                case Direction.East:
                    modStart.X = end.X;
                    modStart.Y = start.Y;
                    modEnd.X = end.X;
                    modEnd.Y = end.Y;
                    break;
                case Direction.North:
                    modStart.X = start.X;
                    modStart.Y = start.Y;
                    modEnd.X = end.X;
                    modEnd.Y = start.Y;
                    break;
                case Direction.South:
                    modStart.X = start.X;
                    modStart.Y = end.Y;
                    modEnd.X = end.X;
                    modEnd.Y = end.Y;
                    break;
                default:
                    throw new ArgumentException();
            }

            float finalFloorHeight = floorIndex * floorHeight;
            float finalCeilingHeight = (floorIndex + 1) * floorHeight + InteriorOffset;

            switch (feature)
            {
                case WallFeature.Window:
                    float windowBottomHeight = finalCeilingHeight - floorHeight * 0.5f;
                    float windowTopHeight = finalCeilingHeight - floorHeight * 0.2f;
                    AppendRectangle(modStart.X, modStart.Y, finalFloorHeight, modEnd.X, modEnd.Y, windowBottomHeight, ref meshElement);
                    AppendRectangle(modStart.X, modStart.Y, windowTopHeight, modEnd.X, modEnd.Y, finalCeilingHeight, ref meshElement);
                    break;
                case WallFeature.Doorway:
                    float doorwayTopHeight = finalCeilingHeight - floorHeight * 0.15f;
                    AppendRectangle(modStart.X, modStart.Y, doorwayTopHeight, modEnd.X, modEnd.Y, finalCeilingHeight, ref meshElement);
                    break;
                case WallFeature.None:
                    AppendRectangle(modStart.X, modStart.Y, finalFloorHeight, modEnd.X, modEnd.Y, finalCeilingHeight, ref meshElement);
                    break;
            }
        }

        private static void AppendRectangle(float x, float y, float z, float xx, float yy, float zz, ref XElement appendTarget)
        {
            XElement triangleElement = new XElement("triangle");
            triangleElement.Add(CreateXmlPoint(x, z, y));
            triangleElement.Add(CreateXmlPoint(xx, z, yy));
            triangleElement.Add(CreateXmlPoint(xx, zz, yy));
            appendTarget.Add(triangleElement);

            triangleElement = new XElement("triangle");
            triangleElement.Add(CreateXmlPoint(xx, zz, yy));
            triangleElement.Add(CreateXmlPoint(x, zz, y));
            triangleElement.Add(CreateXmlPoint(x, z, y));
            appendTarget.Add(triangleElement);
        }

        private static XElement ExportExteriorWallLayer(Building bld, Dictionary<int, List<int>> windowTiles)
        {
            XElement ret = new XElement("layer");
            ret.SetAttributeValue("name", Enum.GetName(typeof(Layer), Layer.ExteriorWall).ToLowerInvariant());

            for (int floorIndex = 0; floorIndex < bld.Floors.Count; floorIndex++)
            {
                XElement floorElement = new XElement("floor");
                Floor cFloor = bld.Floors[floorIndex];
                FloorplanGrid grid = cFloor.Grid;
                List<int> floorWindowTiles = windowTiles[floorIndex];

                // Export the floor bounds
                XElement meshElement = new XElement("mesh");
                float floorHeight = bld.Resolution * FloorHeightScalingFactor;

                for (int tile = 0; tile < grid.TileCount; tile++)
                {
                    Point origPos = grid.ToPosition(tile);

                    if (grid.CheckUnavailable(origPos.X, origPos.Y))
                        continue;

                    Entrance entrance = cFloor.GetEntranceAt(origPos.X, origPos.Y);
                    bool hasWindow = floorWindowTiles.Contains(tile);

                    for (int cdir = 0; cdir < 4; cdir++)
                    {
                        Direction curDirection = cdir.ToDirection();
                        Point modPos = origPos.Advance(curDirection, 1);

                        if (grid.CheckUnavailable(modPos.X, modPos.Y))
                        {
                            PointF start = new PointF(origPos.X * bld.Resolution, origPos.Y * bld.Resolution);
                            PointF end = new PointF(start.X + bld.Resolution, start.Y + bld.Resolution);
                            WallFeature feature = WallFeature.None;

                            if ((entrance != null) &&
                                (entrance.Type == EntranceType.Passage) &&
                                (entrance.Direction.GetOpposite() == curDirection))
                                feature = WallFeature.Doorway;
                            else if(hasWindow)
                                feature = WallFeature.Window;

                            GenerateWallTile(floorIndex, floorHeight, start, end, feature, curDirection, ref meshElement);
                        }
                    }
                }

                floorElement.Add(meshElement);
                ret.Add(floorElement);
            }

            return ret;
        }

        private static XElement ExportInteriorFloorLayer(Building bld)
        {
            XElement ret = new XElement("layer");
            ret.SetAttributeValue("name", Enum.GetName(typeof(Layer), Layer.InteriorFloor).ToLowerInvariant());

            for (int floorIndex = 0; floorIndex < bld.Floors.Count; floorIndex++)
            {
                XElement floorElement = new XElement("floor");
                Floor cFloor = bld.Floors[floorIndex];
                FloorplanGrid grid = cFloor.Grid;

                XElement meshElement = new XElement("mesh");
                float floorHeight = (bld.Resolution * FloorHeightScalingFactor) * floorIndex;

                for (int tile = 0; tile < grid.TileCount; tile++)
                {
                    Point origPos = grid.ToPosition(tile);

                    if (grid.CheckUnavailable(origPos.X, origPos.Y))
                        continue;

                    PointF realPos = new PointF(origPos.X * bld.Resolution, origPos.Y * bld.Resolution);
                    XElement triangleElement = new XElement("triangle");
                    triangleElement.Add(CreateXmlPoint(realPos.X, floorHeight, realPos.Y));
                    triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, floorHeight, realPos.Y));
                    triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, floorHeight, realPos.Y + bld.Resolution));
                    meshElement.Add(triangleElement);

                    triangleElement = new XElement("triangle");
                    triangleElement.Add(CreateXmlPoint(realPos.X + bld.Resolution, floorHeight, realPos.Y + bld.Resolution));
                    triangleElement.Add(CreateXmlPoint(realPos.X, floorHeight, realPos.Y + bld.Resolution));
                    triangleElement.Add(CreateXmlPoint(realPos.X, floorHeight, realPos.Y));
                    meshElement.Add(triangleElement);
                }

                floorElement.Add(meshElement);
                ret.Add(floorElement);
            }

            return ret;
        }

        private static XElement ExportCeilingLayer(Building bld)
        {
            XElement ret = new XElement("layer");
            ret.SetAttributeValue("name", Enum.GetName(typeof(Layer), Layer.Ceiling).ToLowerInvariant());

            for(int n = 0; n < bld.Floors.Count(); n++)
                ret.Add(new XElement("floor"));

            Floor cfloor = (Floor)bld.Floors.Last();
            var validBounds = cfloor.Grid.FindRectanglesExclusive(FloorTileType.Unavailable);
            float floorHeight = bld.Resolution * FloorHeightScalingFactor;
            XElement floorElement = new XElement("floor");
            XElement meshElement = new XElement("mesh");

            foreach (var rect in validBounds)
            {
                PointF start = new PointF(rect.TopLeft.X * bld.Resolution, rect.TopLeft.Y * bld.Resolution);
                PointF end = new PointF(rect.BottomRight.X * bld.Resolution, rect.BottomRight.Y * bld.Resolution);

                GenerateFloor(bld.Floors.Count(), floorHeight, start, end, ref meshElement);
            }

            floorElement.Add(meshElement);
            ret.Add(floorElement);
            return ret;
        }

        private static XElement CreateXmlPoint(float x, float y, float z)
        {
            XElement pointElement = new XElement("point");
            pointElement.SetAttributeValue("x", x);
            pointElement.SetAttributeValue("y", y);
            pointElement.SetAttributeValue("z", z);

            return pointElement;
        }
    }
}
