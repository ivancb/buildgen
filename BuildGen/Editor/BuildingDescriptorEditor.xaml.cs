using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using BuildGen.IO;
using System.Windows.Input;
using System.Collections.Generic;

namespace Editor
{
    public partial class BuildingDescriptorEditor : UserControl, IEditor
    {
        private enum DrawModes
        {
            None,
            BoundsAdd,
            BoundsRemove,
            EntranceRegular,
            EntranceTerminal,
            EntrancePassage,
            EntranceTransition,
            EntranceRemove,
        };

        private DrawModes activeDrawMode;
        private BuildGen.Data.Building activeBuilding;
        private System.Windows.Shapes.Rectangle activeShape;
        private BuildGen.Data.Point activeShapeOrigin;
        private BuildGen.Data.Point activeShapeEnd;
        private Point gridCellSize;
        private int activeFloorIndex;
        private DataRegistry Registry;

        public delegate void ContentsModifiedHandler(object sender);
        public event ContentsModifiedHandler ContentsModified;

        public BuildingDescriptorEditor(DataRegistry registry)
        {
            InitializeComponent();

            Registry = registry;
            activeDrawMode = DrawModes.None;
            activeBuilding = null;
            activeShape = null;
            activeFloorIndex = -1;
        }

        public string Text 
        { 
            get 
            {
                BuildingXmlIo io = new BuildingXmlIo();
                return io.ExportBuildingDescription(activeBuilding); 
            } 
        }

        public BuildGen.Data.Building ActiveBuilding { get { return activeBuilding; } }

        public bool Edit(string contents)
        {
            BuildingXmlIo reader = new BuildingXmlIo();
            activeBuilding = reader.Parse(contents);

            if (activeBuilding == null)
            {
                return false;
            }
            else
            {
                SetActiveFloor(0);
                InitializeFloorList();

                return true;
            }
        }

        public bool Edit(BuildGen.Data.Building bld)
        {
            if (bld == null)
            {
                return false;
            }
            else
            {
                activeBuilding = bld;
                SetActiveFloor(0);
                InitializeFloorList();

                return true;
            }
        }

        private void HandleBoundsOp(int x, int y, int xx, int yy)
        {
            if (activeDrawMode == DrawModes.BoundsAdd)
            {
                activeBuilding.Floors[activeFloorIndex].Grid.Set(x, y, xx + 1, yy + 1, BuildGen.Data.FloorTileType.Vacant);
            }
            else
            {
                // If we're subtracting an area, confirm first that the area doesn't include any entrances
                BuildGen.Data.Floor cfloor = activeBuilding.Floors[activeFloorIndex];
                bool canSubtract = true;

                foreach (var entrance in cfloor.Entrances)
                {
                    if ((entrance.GridPosition.X >= x) && (entrance.GridPosition.Y >= y) &&
                        (entrance.GridPosition.X <= xx) && (entrance.GridPosition.Y <= yy))
                    {
                        MessageBox.Show("Cannot subtract this area because it contains one or more entrances.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        canSubtract = false;
                        break;
                    }
                }

                if (canSubtract)
                    activeBuilding.Floors[activeFloorIndex].Grid.Set(x, y, xx + 1, yy + 1, BuildGen.Data.FloorTileType.Unavailable);
                else
                    DrawingCanvas.Children.RemoveAt(DrawingCanvas.Children.Count - 1);
            }

            InvalidateCanvasElements(false);
            ContentsModified(this);
        }

        private void HandleEntranceOp(int x, int y, bool sourceWasLeftClick)
        {
            BuildGen.Data.Floor cfloor = activeBuilding.Floors[activeFloorIndex];

            var pos = ConvertGridRenderPosToRealPos(x, y);
            bool canPerform = !cfloor.Grid.CheckType(x, y, BuildGen.Data.FloorTileType.Unavailable);

            if (canPerform)
            {
                switch(activeDrawMode)
                {
                    case DrawModes.EntranceRemove:
                        RemoveEntrance(x, y, activeFloorIndex);
                        break;
                    case DrawModes.EntranceRegular:
                        if (activeFloorIndex >= (activeBuilding.Floors.Count - 1))
                            MessageBox.Show("Terminal entrances require a floor above the current floor.");
                        else if(!AddEntrance(x, y, BuildGen.Data.EntranceType.Entrance, activeFloorIndex, activeFloorIndex, sourceWasLeftClick))
                            MessageBox.Show("Could not update the entrance at the specified location - no exit path available.");
                        break;
                    case DrawModes.EntrancePassage:
                        if (!AddEntrance(x, y, BuildGen.Data.EntranceType.Passage, activeFloorIndex, activeFloorIndex, sourceWasLeftClick))
                            MessageBox.Show("Could not update the entrance at the specified location - no exit path available.");
                        break;
                    case DrawModes.EntranceTerminal:
                        if (activeFloorIndex == 0)
                            MessageBox.Show("Terminal entrances require a floor below the current floor.");
                        else if(!AddEntrance(x, y, BuildGen.Data.EntranceType.Terminal, activeFloorIndex, activeFloorIndex, sourceWasLeftClick))
                            MessageBox.Show("Could not update the entrance at the specified location - no exit path available.");
                        break;
                    case DrawModes.EntranceTransition:
                        if((activeFloorIndex == 0) || (activeFloorIndex >= (activeBuilding.Floors.Count - 1)))
                            MessageBox.Show("Transition entrances require a floor below and above the current floor.");
                        else if(!AddEntrance(x, y, BuildGen.Data.EntranceType.Transition, activeFloorIndex, activeFloorIndex, sourceWasLeftClick))
                            MessageBox.Show("Could not update the entrance at the specified location - no exit path available.");
                        break;
                }
            }
            else
            {
                MessageBox.Show("Cannot complete the operation. The specified area isn't part of the building's floorplan.");
            }

            SetActiveFloor(activeFloorIndex);
        }

        private bool GetNextValidEntranceDirection(int x, int y, int floorIndex, ref BuildGen.Data.Direction baseDirection, bool reverse)
        {
            var cfloor = activeBuilding.Floors[floorIndex];
            BuildGen.Data.Point pos;

            if (cfloor.Grid.CheckUnavailable(x, y))
                return false;

            BuildGen.Data.Direction ndir = baseDirection;
            for (int n = 0; n < 4; n++)
            {
                ndir = BuildGen.Data.DirectionExtensions.Advance(ndir, reverse);

                pos.X = x;
                pos.Y = y;
                pos = pos.Advance(ndir, 1);

                if (cfloor.Grid.CheckUnavailable(pos.X, pos.Y))
                {
                    continue;
                }
                else
                {
                    baseDirection = ndir;
                    return true;
                }
            }

            return false;
        }

        private bool AddEntrance(int x, int y, BuildGen.Data.EntranceType type, int floorIndex, int sourceFloor, bool sourceWasLeftClick)
        {
            var cfloor = activeBuilding.Floors[floorIndex];
            var entrance = cfloor.GetEntranceAt(x, y);

            if (entrance != null)
            {
                var direction = entrance.Direction;
                if (!GetNextValidEntranceDirection(x, y, floorIndex, ref direction, !sourceWasLeftClick))
                    return false;

                if ((entrance.Type == BuildGen.Data.EntranceType.Entrance) && (floorIndex < (activeBuilding.Floors.Count - 1)) && ((floorIndex + 1) != sourceFloor))
                    AddEntrance(x, y, type, floorIndex + 1, floorIndex, sourceWasLeftClick);
                else if ((entrance.Type == BuildGen.Data.EntranceType.Terminal) && (floorIndex > 0) && ((floorIndex - 1) != sourceFloor))
                    AddEntrance(x, y, type, floorIndex - 1, floorIndex, sourceWasLeftClick);
                else if (entrance.Type == BuildGen.Data.EntranceType.Transition)
                {
                    if (floorIndex < (activeBuilding.Floors.Count - 1) && ((floorIndex + 1) != sourceFloor))
                        AddEntrance(x, y, type, floorIndex + 1, floorIndex, sourceWasLeftClick);
                    if ((floorIndex > 0)  && ((floorIndex - 1) != sourceFloor))
                        AddEntrance(x, y, type, floorIndex - 1, floorIndex, sourceWasLeftClick);
                }

                entrance.Direction = direction;
                return true;
            }
            else
            {
                var direction = BuildGen.Data.Direction.East;
                if (!GetNextValidEntranceDirection(x, y, floorIndex, ref direction, false))
                    return false;

                if ((type == BuildGen.Data.EntranceType.Entrance) && (floorIndex < (activeBuilding.Floors.Count - 1)) && ((floorIndex + 1) != sourceFloor))
                    AddEntrance(x, y, BuildGen.Data.EntranceType.Terminal, floorIndex + 1, floorIndex, sourceWasLeftClick);
                else if ((type == BuildGen.Data.EntranceType.Terminal) && (floorIndex > 0) && ((floorIndex - 1) != sourceFloor))
                    AddEntrance(x, y, BuildGen.Data.EntranceType.Entrance, floorIndex - 1, floorIndex, sourceWasLeftClick);
                else if (type == BuildGen.Data.EntranceType.Transition)
                {
                    if ((floorIndex < (activeBuilding.Floors.Count - 1)) && ((floorIndex + 1) != sourceFloor))
                        AddEntrance(x, y, BuildGen.Data.EntranceType.Terminal, floorIndex + 1, floorIndex, sourceWasLeftClick);
                    if ((floorIndex > 0)  && ((floorIndex - 1) != sourceFloor))
                        AddEntrance(x, y, BuildGen.Data.EntranceType.Entrance, floorIndex - 1, floorIndex, sourceWasLeftClick);
                }

                cfloor.AddEntrance(x, y, type, direction);
                return true;
            }
        }

        /// <summary>
        /// Removes an entrance from the currently active building.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="floorIndex"></param>
        private void RemoveEntrance(int x, int y, int floorIndex)
        {
            BuildGen.Data.Entrance entrance = activeBuilding.Floors[floorIndex].GetEntranceAt(x, y);

            if (entrance != null)
            {
                activeBuilding.Floors[floorIndex].RemoveEntranceAt(x, y);

                if ((entrance.Type == BuildGen.Data.EntranceType.Entrance) && (floorIndex < (activeBuilding.Floors.Count - 1)))
                    RemoveEntrance(x, y, floorIndex + 1);
                else if ((entrance.Type == BuildGen.Data.EntranceType.Terminal) && (floorIndex > 0))
                    RemoveEntrance(x, y, floorIndex - 1);
                else if (entrance.Type == BuildGen.Data.EntranceType.Transition)
                {
                    if (floorIndex < (activeBuilding.Floors.Count - 1))
                        RemoveEntrance(x, y, floorIndex + 1);
                    if (floorIndex > 0)
                        RemoveEntrance(x, y, floorIndex - 1);
                }
            }
        }

        /// <summary>
        /// Changes between active floors and clears and then renders the floor contents to the canvas.
        /// </summary>
        /// <param name="floorIndex">The floor number to switch to.</param>
        private void SetActiveFloor(int floorIndex)
        {
            BuildGen.Data.Floor cfloor = activeBuilding.Floors[floorIndex];
            BuildGen.Data.FloorplanGrid grid = cfloor.Grid;

            ResetCanvas(activeBuilding.Width, activeBuilding.Height, activeBuilding.Resolution);

            // Render the floor bounds
            var unavailableFill = new SolidColorBrush(Colors.Black);
            for(int cx = 0; cx < grid.Columns; cx++)
            {
                for (int cy = 0; cy < grid.Rows; cy++)
                {
                    if (grid.CheckType(cx, cy, BuildGen.Data.FloorTileType.Unavailable))
                    {
                        var nshape = new System.Windows.Shapes.Rectangle();
                        nshape.Fill = unavailableFill;

                        Point origin = ConvertRealPosToGridPos(cx, cy);

                        Canvas.SetLeft(nshape, origin.X);
                        Canvas.SetTop(nshape, origin.Y);
                        nshape.Width = gridCellSize.X;
                        nshape.Height = gridCellSize.Y;

                        DrawingCanvas.Children.Add(nshape);
                    }
                }
            }

            // Render the entrances
            foreach (var entrance in cfloor.Entrances)
            {
                var nshape = new System.Windows.Shapes.Rectangle();
                var textShape = new System.Windows.Controls.TextBlock();

                switch (entrance.Type)
                {
                    case BuildGen.Data.EntranceType.Entrance:
                        nshape.Fill = new SolidColorBrush(Colors.Green);
                        break;
                    case BuildGen.Data.EntranceType.Passage:
                        nshape.Fill = new SolidColorBrush(Colors.Gray);
                        break;
                    case BuildGen.Data.EntranceType.Terminal:
                        nshape.Fill = new SolidColorBrush(Colors.LightGray);
                        break;
                    case BuildGen.Data.EntranceType.Transition:
                        nshape.Fill = new SolidColorBrush(Colors.Gold);
                        break;
                }

                Point origin = ConvertRealPosToGridPos(entrance.GridPosition.X, entrance.GridPosition.Y);

                Canvas.SetLeft(nshape, origin.X);
                Canvas.SetTop(nshape, origin.Y);
                nshape.Width = gridCellSize.X;
                nshape.Height = gridCellSize.Y;

                Canvas.SetLeft(textShape, origin.X);
                Canvas.SetTop(textShape, origin.Y - 5);
                textShape.Width = 25;
                textShape.Height = 25;

                textShape.Text = Enum.GetName(typeof(BuildGen.Data.Direction), entrance.Direction).Substring(0, 1);

                DrawingCanvas.Children.Add(nshape);
                DrawingCanvas.Children.Add(textShape);
            }

            activeFloorIndex = floorIndex;
        }

        private void AddNewFloor()
        {
            BuildGen.Data.Floor previousFloor = activeBuilding.Floors[activeBuilding.Floors.Count - 1];

            var nFloor = activeBuilding.AddFloor();

            if (previousFloor != null)
            {
                nFloor.Grid = previousFloor.Grid.Clone();
                foreach (var entrance in previousFloor.Entrances)
                {
                    if ((entrance.Type != BuildGen.Data.EntranceType.Terminal) &&
                        (entrance.Type != BuildGen.Data.EntranceType.Passage))
                    {
                        nFloor.AddEntrance(entrance.GridPosition.X, entrance.GridPosition.Y, BuildGen.Data.EntranceType.Terminal, entrance.Direction);
                    }
                }
            }

            InitializeFloorList();
        }

        private void RemoveFloor()
        {
            if(activeBuilding.Floors.Count == 1)
            {
                MessageBox.Show("A building must have at least one floor.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else 
            {
                if((activeBuilding.Floors.Count >= 2) && (activeFloorIndex < (activeBuilding.Floors.Count - 1)))
                {
                    BuildGen.Data.Floor curFloor = activeBuilding.Floors[activeFloorIndex];
                    BuildGen.Data.Floor upperFloor = activeBuilding.Floors[activeFloorIndex + 1];

                    foreach (var entrance in curFloor.Entrances)
                    {
                        if ((entrance.Type == BuildGen.Data.EntranceType.Terminal) ||
                            (entrance.Type == BuildGen.Data.EntranceType.Transition))
                        {
                            upperFloor.AddEntrance(entrance.GridPosition.X, entrance.GridPosition.Y, entrance.Type, entrance.Direction);
                        }
                        else
                        {
                            upperFloor.RemoveEntranceAt(entrance.GridPosition.X, entrance.GridPosition.Y);
                        }
                    }
                }

                activeBuilding.Floors.RemoveAt(activeFloorIndex);
                SetActiveFloor((activeFloorIndex >= activeBuilding.Floors.Count) ? (activeFloorIndex - 1) : activeFloorIndex);
                InitializeFloorList();
            }
        }

        /// <summary>
        /// Creates buttons that allow the user to switch between active floors.
        /// Also creates a label that specifies the active floor.
        /// </summary>
        private void InitializeFloorList()
        {
            FloorPanel.Children.Clear();

            Thickness widgetMargin = new Thickness(0f, 0f, 5f, 0f);

            // Create the label that indicates which floor's currently active
            TextBlock curFloorLabel = new TextBlock();
            curFloorLabel.Width = 100;
            curFloorLabel.Height = 20;
            curFloorLabel.Text = "Current Floor - " + activeFloorIndex;
            curFloorLabel.Margin = widgetMargin;

            FloorPanel.Children.Add(curFloorLabel);

            // Create the various floor buttons
            for(int n = 0; n < activeBuilding.Floors.Count; n++)
            {
                // NOTE: This is necessary since the delegate 'lambda' actually gets a reference to a variable instead of its value.
                int cIndex = n;

                Button floorButton = new Button();
                floorButton.Width = 30;
                floorButton.Height = 20;
                floorButton.Content = n;
                floorButton.Margin = widgetMargin;
                floorButton.Click += delegate { SetActiveFloor(cIndex); curFloorLabel.Text = "Current Floor - " + cIndex; };

                FloorPanel.Children.Add(floorButton);
            }

            // Add new floor and remove floor buttons
            Button addFloorButton = new Button();
            Button removeFloorButton = new Button();

            addFloorButton.Width = 60;
            addFloorButton.Height = 20;
            addFloorButton.Content = "Add";
            addFloorButton.Margin = widgetMargin;
            addFloorButton.Click += delegate { AddNewFloor(); };

            removeFloorButton.Width = 60;
            removeFloorButton.Height = 20;
            removeFloorButton.Content = "Remove";
            removeFloorButton.Margin = widgetMargin;
            removeFloorButton.Click += delegate { RemoveFloor(); };

            FloorPanel.Children.Add(addFloorButton);
            FloorPanel.Children.Add(removeFloorButton);
        }

        /// <summary>
        /// Resets a canvas grid based on the provided parameters.
        /// </summary>
        /// <param name="width">Width of the building grid.</param>
        /// <param name="height">Height of the building's grid.</param>
        /// <param name="resolution">The resolution specified for the building's grid.</param>
        private void ResetCanvas(float width, float height, float resolution, bool removeNonGridLines = true)
        {
            if (DrawingCanvas.Children.Count != 0)
            {
                InvalidateCanvasElements(removeNonGridLines);
                DrawingCanvas.InvalidateVisual();
            }
            else
                DrawGrid(width, height, resolution);
        }

        private void InvalidateCanvasElements(bool removeNonGridLines)
        {
            List<int> invalidCanvasElementIndices = new List<int>();

            int n = 0;
            foreach (var childElement in DrawingCanvas.Children)
            {
                if (childElement is Line)
                {
                    Line cline = (Line)childElement;
                    
                    cline.InvalidateVisual();
                    cline.InvalidateMeasure(); cline.UpdateLayout();
                }
                else if (removeNonGridLines)
                    invalidCanvasElementIndices.Add(n);

                n++;
            }

            if (removeNonGridLines)
            {
                invalidCanvasElementIndices.Reverse();
                foreach (int index in invalidCanvasElementIndices)
                {
                    DrawingCanvas.Children.RemoveAt(index);
                }
            }
        }

        private void DrawGrid(float width, float height, float resolution)
        {
            int numHorizLines = (int)Math.Ceiling(width / resolution);
            int numVerticalLines = (int)Math.Ceiling(height / resolution);

            float xScale = (float)DrawingCanvas.ActualWidth / numHorizLines;
            float yScale = (float)DrawingCanvas.ActualHeight / numVerticalLines;
            gridCellSize.X = xScale;
            gridCellSize.Y = yScale;

            Brush lineBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100));

            // Create the horizontal lines
            // NOTE: Line 0 is skipped since its initial position is precisely 0
            for (int n = 1; n < numHorizLines; n++)
            {
                Line l = new Line();
                l.X1 = 0;
                l.X2 = DrawingCanvas.ActualWidth;
                l.Y1 = n * yScale - 1;
                l.Y2 = n * yScale + 1;
                l.Stroke = lineBrush;

                DrawingCanvas.Children.Add(l);
                Canvas.SetZIndex(l, 100);
            }

            // Create the vertical lines
            for (int n = 1; n < numVerticalLines; n++)
            {
                Line l = new Line();
                l.X1 = n * xScale - 1;
                l.X2 = n * xScale + 1;
                l.Y1 = 0;
                l.Y2 = DrawingCanvas.ActualHeight;
                l.Stroke = lineBrush;

                DrawingCanvas.Children.Add(l);
                Canvas.SetZIndex(l, 100);
            }
        }

        /// <summary>
        /// Updates the topleft position and the size of the currently active shape based on the rectangle for it.
        /// </summary>
        private void UpdateActiveShape()
        {
            Point gridPosStart = ConvertRealPosToGridPos(activeShapeOrigin.X, activeShapeOrigin.Y);
            Point gridPosEnd = ConvertRealPosToGridPos(activeShapeEnd.X, activeShapeEnd.Y);

            if ((activeDrawMode == DrawModes.BoundsAdd) || (activeDrawMode == DrawModes.BoundsRemove))
            {
                Canvas.SetLeft(activeShape, Math.Min(gridPosStart.X, gridPosEnd.X));
                Canvas.SetTop(activeShape, Math.Min(gridPosStart.Y, gridPosEnd.Y));
                activeShape.Width = Math.Max(gridPosStart.X, gridPosEnd.X) - Math.Min(gridPosStart.X, gridPosEnd.X) + gridCellSize.X;
                activeShape.Height = Math.Max(gridPosStart.Y, gridPosEnd.Y) - Math.Min(gridPosStart.Y, gridPosEnd.Y) + gridCellSize.Y;
            }
            else
            {
                Canvas.SetLeft(activeShape, gridPosEnd.X);
                Canvas.SetTop(activeShape, gridPosEnd.Y);
                activeShape.Width = gridCellSize.X;
                activeShape.Height = gridCellSize.Y;
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            activeBuilding.Floors[activeFloorIndex].Grid.Reset();
            SetActiveFloor(activeFloorIndex);
        }
       
        /// <summary>
        /// Begins drawing the area for a new operation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DrawingCanvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (activeDrawMode != DrawModes.None)
            {
                activeShape = new System.Windows.Shapes.Rectangle();

                switch (activeDrawMode)
                {
                    case DrawModes.BoundsAdd:
                        activeShape.Fill = new SolidColorBrush(Color.FromRgb(153, 180, 209));
                        break;
                    case DrawModes.BoundsRemove:
                        activeShape.Fill = new SolidColorBrush(Colors.Black);
                        break;
                    case DrawModes.EntranceRemove:
                        activeShape.Fill = new SolidColorBrush(Colors.Red);
                        break;
                    case DrawModes.EntranceRegular:
                        activeShape.Fill = new SolidColorBrush(Colors.Green);
                        break;
                    case DrawModes.EntrancePassage:
                        activeShape.Fill = new SolidColorBrush(Colors.Gray);
                        break;
                    case DrawModes.EntranceTerminal:
                        activeShape.Fill = new SolidColorBrush(Colors.LightGray);
                        break;
                    case DrawModes.EntranceTransition:
                        activeShape.Fill = new SolidColorBrush(Colors.Gold);
                        break;
                    default:
                        activeShape.Fill = new SolidColorBrush(Colors.Blue);
                        break;
                }

                Point clickPos = e.GetPosition(DrawingCanvas);
                activeShapeOrigin = ConvertGridRenderPosToRealPos(clickPos.X, clickPos.Y);
                activeShapeEnd = activeShapeOrigin;

                UpdateActiveShape();
                DrawingCanvas.Children.Add(activeShape);
            }
        }

        /// <summary>
        /// Stops drawing the bounds for an operation and stores the operation data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DrawingCanvas_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if ((activeDrawMode == DrawModes.BoundsAdd) || (activeDrawMode == DrawModes.BoundsRemove))
            {
                if (activeShape != null)
                {
                    Point clickPos = e.GetPosition(DrawingCanvas);
                    activeShapeEnd = ConvertGridRenderPosToRealPos(clickPos.X, clickPos.Y);
                    UpdateActiveShape();

                    int x = Math.Min(activeShapeOrigin.X, activeShapeEnd.X);
                    int y = Math.Min(activeShapeOrigin.Y, activeShapeEnd.Y);
                    int xx = Math.Max(activeShapeOrigin.X, activeShapeEnd.X);
                    int yy = Math.Max(activeShapeOrigin.Y, activeShapeEnd.Y);
                    HandleBoundsOp(x, y, xx, yy);

                    activeShape = null;
                }
            }
            else if (activeDrawMode != DrawModes.None)
            {
                activeShape = null;

                Point clickPos = e.GetPosition(DrawingCanvas);
                var gridPos = ConvertGridRenderPosToRealPos(clickPos.X, clickPos.Y);

                HandleEntranceOp(gridPos.X, gridPos.Y, e.ChangedButton == MouseButton.Left);
            }
        }

        private void DrawingCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (activeShape != null)
            {
                Point clickPos = e.GetPosition(DrawingCanvas);
                activeShapeEnd = ConvertGridRenderPosToRealPos(clickPos.X, clickPos.Y);
                UpdateActiveShape();
            }
        }

        private void DrawingCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (activeShape != null)
            {
                DrawingCanvas.Children.Remove(activeShape);
                activeShape = null;
            }
        }

        private Point ConvertRealPosToGridPos(int x, int y)
        {
            BuildGen.Data.Floor floor = activeBuilding.Floors[0];

            float xScale = (float)DrawingCanvas.ActualWidth / floor.Grid.Columns;
            float yScale = (float)DrawingCanvas.ActualHeight / floor.Grid.Stride;

            return new Point(x * xScale, y * yScale);
        }

        private BuildGen.Data.Point ConvertGridRenderPosToRealPos(double x, double y)
        {
            BuildGen.Data.Floor floor = activeBuilding.Floors[0];

            float xScale = (float)DrawingCanvas.ActualWidth / floor.Grid.Columns;
            float yScale = (float)DrawingCanvas.ActualHeight / floor.Grid.Stride;

            return new BuildGen.Data.Point((int)Math.Floor(x / xScale), (int)Math.Floor(y / yScale));
        }

        private void DrawingCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(activeFloorIndex != -1)
                SetActiveFloor(activeFloorIndex);
        }

        private void BuildingSettings_Click(object sender, RoutedEventArgs e)
        {
            DefinitionFileSettings settingsDialog = new DefinitionFileSettings(Registry);
            settingsDialog.ConstraintSet = activeBuilding.ConstraintSet;
            settingsDialog.Seed = activeBuilding.Seed;
            settingsDialog.BuildingWidth = activeBuilding.Width;
            settingsDialog.BuildingHeight = activeBuilding.Height;
            settingsDialog.BuildingResolution = activeBuilding.Resolution;

            bool? ret = settingsDialog.ShowDialog();
            if (ret.HasValue && ret.Value)
            {
                ActiveBuilding.ConstraintSet = settingsDialog.ConstraintSet;
                ActiveBuilding.Seed = settingsDialog.Seed;

                ContentsModified(this);
            }
        }

        private void AddRectangleMode_Click(object sender, RoutedEventArgs e)
        {
            activeDrawMode = DrawModes.BoundsAdd;
        }

        private void RemoveRectangleMode_Click(object sender, RoutedEventArgs e)
        {
            activeDrawMode = DrawModes.BoundsRemove;
        }

        private void RegularEntrance_Click(object sender, RoutedEventArgs e)
        {
            activeDrawMode = DrawModes.EntranceRegular;
        }

        private void TerminalEntrance_Click(object sender, RoutedEventArgs e)
        {
            activeDrawMode = DrawModes.EntranceTerminal;
        }

        private void TransitionEntrance_Click(object sender, RoutedEventArgs e)
        {
            activeDrawMode = DrawModes.EntranceTransition;
        }

        private void PassageEntrance_Click(object sender, RoutedEventArgs e)
        {
            activeDrawMode = DrawModes.EntrancePassage;
        }

        private void RemoveEntrance_Click(object sender, RoutedEventArgs e)
        {
            activeDrawMode = DrawModes.EntranceRemove;
        }
    }
}
