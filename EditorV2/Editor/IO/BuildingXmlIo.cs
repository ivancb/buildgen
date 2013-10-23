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

                editorElement.Add(ExportPassagesLayer(bld));
                editorElement.Add(ExportRoomsLayer(bld));
                editorElement.Add(ExportCeilingLayer(bld));

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

            List<Rectangle> windows = new List<Rectangle>();

            for (int n = 0; n < bld.Floors.Count; n++)
            {
                XElement floorElement = new XElement("floor");
                FloorplanGrid grid = bld.Floors[n].Grid;
                float floorHeight = bld.Resolution * 2;

                // Export the floor bounds
                XElement meshElement = new XElement("mesh");
                XElement triangleElement = null;

                for (int tileIndex = 0; tileIndex < grid.TileCount; tileIndex++)
                {
                    if ((grid[tileIndex] == FloorTileType.Passage) || (grid[tileIndex] == FloorTileType.Other))
                    {
                        meshElement.Add(new XComment("Tile " + tileIndex));

                        Point cPos = grid.ToPosition(tileIndex);
                        PointF realPos = new PointF(cPos.X * bld.Resolution, cPos.Y * bld.Resolution);

                        // Floor
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

                        bool[] walls = new bool[4] { false, false, false, false };

                        for (int cdir = 0; cdir < 4; cdir++)
                        {
                            Point offsetPos = cPos.Advance(cdir.ToDirection(), 1);
                            int nIndex = grid.ToIndex(offsetPos.X, offsetPos.Y);

                            if ((nIndex == -1) || ((grid[nIndex] != FloorTileType.Passage) && (grid[nIndex] != FloorTileType.Other)))
                            {
                                walls[cdir] = true;
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

        private static XElement ExportRoomsLayer(Building bld)
        {
            XElement ret = new XElement("layer");
            ret.SetAttributeValue("name", Enum.GetName(typeof(Layer), Layer.Rooms).ToLowerInvariant());

            List<Rectangle> windows = new List<Rectangle>();

            for (int n = 0; n < bld.Floors.Count; n++)
            {
                XElement floorElement = new XElement("floor");
                Floor floor = bld.Floors[n];
                float floorHeight = bld.Resolution * 2;

                // Export the floor bounds
                XElement meshElement = new XElement("mesh");
                XElement triangleElement = null;

                foreach (var room in floor.Rooms)
                {
                    PointF start = new PointF(room.TopLeft.X * bld.Resolution + InteriorOffset, room.TopLeft.Y * bld.Resolution + InteriorOffset);
                    PointF end = new PointF(room.BottomRight.X * bld.Resolution - InteriorOffset, room.BottomRight.Y * bld.Resolution - InteriorOffset);

                    // North walls
                    meshElement.Add(new XComment("North"));

                    triangleElement = new XElement("triangle");
                    triangleElement.Add(CreateXmlPoint(start.X, n * floorHeight, start.Y));
                    triangleElement.Add(CreateXmlPoint(end.X, n * floorHeight, start.Y));
                    triangleElement.Add(CreateXmlPoint(end.X, (n + 1) * floorHeight, start.Y));
                    meshElement.Add(triangleElement);

                    triangleElement = new XElement("triangle");
                    triangleElement.Add(CreateXmlPoint(end.X, (n + 1) * floorHeight, start.Y));
                    triangleElement.Add(CreateXmlPoint(start.X, (n + 1) * floorHeight, start.Y));
                    triangleElement.Add(CreateXmlPoint(start.X, n * floorHeight, start.Y));
                    meshElement.Add(triangleElement);

                    // South walls
                    meshElement.Add(new XComment("South"));

                    triangleElement = new XElement("triangle");
                    triangleElement.Add(CreateXmlPoint(start.X, n * floorHeight, end.Y));
                    triangleElement.Add(CreateXmlPoint(end.X, n * floorHeight, end.Y));
                    triangleElement.Add(CreateXmlPoint(end.X, (n + 1) * floorHeight, end.Y));
                    meshElement.Add(triangleElement);

                    triangleElement = new XElement("triangle");
                    triangleElement.Add(CreateXmlPoint(end.X, (n + 1) * floorHeight, end.Y));
                    triangleElement.Add(CreateXmlPoint(start.X, (n + 1) * floorHeight, end.Y));
                    triangleElement.Add(CreateXmlPoint(start.X, n * floorHeight, end.Y));
                    meshElement.Add(triangleElement);

                    // West walls
                    meshElement.Add(new XComment("West"));

                    triangleElement = new XElement("triangle");
                    triangleElement.Add(CreateXmlPoint(start.X, n * floorHeight, start.Y));
                    triangleElement.Add(CreateXmlPoint(start.X, n * floorHeight, end.Y));
                    triangleElement.Add(CreateXmlPoint(start.X, (n + 1) * floorHeight, end.Y));
                    meshElement.Add(triangleElement);

                    triangleElement = new XElement("triangle");
                    triangleElement.Add(CreateXmlPoint(start.X, (n + 1) * floorHeight, end.Y));
                    triangleElement.Add(CreateXmlPoint(start.X, (n + 1) * floorHeight, start.Y));
                    triangleElement.Add(CreateXmlPoint(start.X, n * floorHeight, start.Y));
                    meshElement.Add(triangleElement);

                    // East walls
                    meshElement.Add(new XComment("East"));

                    triangleElement = new XElement("triangle");
                    triangleElement.Add(CreateXmlPoint(end.X, n * floorHeight, start.Y));
                    triangleElement.Add(CreateXmlPoint(end.X, n * floorHeight, end.Y));
                    triangleElement.Add(CreateXmlPoint(end.X, (n + 1) * floorHeight, end.Y));
                    meshElement.Add(triangleElement);

                    triangleElement = new XElement("triangle");
                    triangleElement.Add(CreateXmlPoint(end.X, (n + 1) * floorHeight, end.Y));
                    triangleElement.Add(CreateXmlPoint(end.X, (n + 1) * floorHeight, start.Y));
                    triangleElement.Add(CreateXmlPoint(end.X, n * floorHeight, start.Y));
                    meshElement.Add(triangleElement);

                    // Floor
                    meshElement.Add(new XComment("Floor"));

                    triangleElement = new XElement("triangle");
                    triangleElement.Add(CreateXmlPoint(start.X, n * floorHeight + InteriorOffset, start.Y));
                    triangleElement.Add(CreateXmlPoint(start.X, n * floorHeight + InteriorOffset, end.Y));
                    triangleElement.Add(CreateXmlPoint(end.X, n * floorHeight + InteriorOffset, end.Y));
                    meshElement.Add(triangleElement);

                    triangleElement = new XElement("triangle");
                    triangleElement.Add(CreateXmlPoint(end.X, n * floorHeight + InteriorOffset, end.Y));
                    triangleElement.Add(CreateXmlPoint(end.X, n * floorHeight + InteriorOffset, start.Y));
                    triangleElement.Add(CreateXmlPoint(start.X, n * floorHeight + InteriorOffset, start.Y));
                    meshElement.Add(triangleElement);
                }

                floorElement.Add(meshElement);
                ret.Add(floorElement);
            }

            return ret;
        }

        private static XElement ExportExteriorWallLayer(Building bld)
        {
            XElement ret = new XElement("layer");
            ret.SetAttributeValue("name", Enum.GetName(typeof(Layer), Layer.ExteriorWall).ToLowerInvariant());

            List<Rectangle> windows = new List<Rectangle>();

            for (int n = 0; n < bld.Floors.Count; n++)
            {
                XElement floorElement = new XElement("floor");
                FloorplanGrid grid = bld.Floors[n].Grid;

                // Export the floor bounds
                XElement meshElement = new XElement("mesh");
                XElement triangleElement = null;

                meshElement.Add(triangleElement);

                floorElement.Add(meshElement);
                ret.Add(floorElement);
            }

            return ret;
        }

        private static XElement ExportCeilingLayer(Building bld)
        {
            XElement ret = new XElement("layer");
            ret.SetAttributeValue("name", Enum.GetName(typeof(Layer), Layer.Ceiling).ToLowerInvariant());

            /*List<Rectangle> windows = new List<Rectangle>();

            for (int n = 0; n < bld.Floors.Count; n++)
            {
                XElement floorElement = new XElement("floor");
                FloorplanGrid grid = bld.Floors[n].Grid;
                float floorHeight = grid.Resolution * 2;

                XElement meshElement = new XElement("mesh");
                XElement triangleElement = null;
                var rects = grid.UsableRegion.Evaluate();

                foreach (var rect in rects)
                {
                    triangleElement = new XElement("triangle");
                    triangleElement.Add(CreateXmlPoint(rect.X, n * floorHeight, rect.Y));
                    triangleElement.Add(CreateXmlPoint(rect.XX, n * floorHeight, rect.Y));
                    triangleElement.Add(CreateXmlPoint(rect.XX, n * floorHeight, rect.YY));
                    meshElement.Add(triangleElement);

                    triangleElement = new XElement("triangle");
                    triangleElement.Add(CreateXmlPoint(rect.XX, n * floorHeight, rect.YY));
                    triangleElement.Add(CreateXmlPoint(rect.X, n * floorHeight, rect.YY));
                    triangleElement.Add(CreateXmlPoint(rect.X, n * floorHeight, rect.Y));
                    meshElement.Add(triangleElement);
                }

                meshElement.Add(triangleElement);

                floorElement.Add(meshElement);
                ret.Add(floorElement);
            }*/

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
