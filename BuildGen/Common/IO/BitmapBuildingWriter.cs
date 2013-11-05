using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuildGen.Data;

namespace BuildGen.IO
{
    public class BitmapBuildingWriter
    {
        public int MaxSize = 1024;

        public bool Write(string destination, Building bld)
        {
            if ((destination.Length == 0) || (bld.Floors.Count() == 0))
                return false;

            for (int n = 0; n < bld.Floors.Count(); n++)
            {
                try
                {
                    System.Drawing.Bitmap bitmap = RenderFloor(bld, n);
                    bitmap.Save(string.Format(destination, n));
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return true;
        }

        public bool Write(string destination, Building bld, int floorIndex)
        {
            if ((destination.Length == 0) || (bld.Floors.Count() == 0))
                return false;

            try
            {
                System.Drawing.Bitmap bitmap = RenderFloor(bld, floorIndex);
                bitmap.Save(string.Format(destination, floorIndex));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public System.Drawing.Bitmap RenderFloor(Building bld, int floorIndex)
        {
            if (bld == null)
                throw new ArgumentNullException("bld");
            if ((floorIndex < 0) || (floorIndex >= bld.Floors.Count))
                throw new ArgumentOutOfRangeException("floorIndex");

            Floor floor = bld.Floors[floorIndex];
            float widthFactor = bld.Width / bld.Height;
            System.Drawing.Bitmap ret = null;

            // Use the appropriate scale if it's not horizontal
            if (widthFactor > 1.0)
                ret = new System.Drawing.Bitmap(MaxSize, (int)Math.Ceiling(MaxSize * (bld.Height / bld.Width)));
            else
                ret = new System.Drawing.Bitmap((int)Math.Ceiling(MaxSize * widthFactor), MaxSize);

            float scaleFactorHorizontal = ret.Width / bld.Width;
            float scaleFactorVertical = ret.Height / bld.Height;

            using (var gfx = System.Drawing.Graphics.FromImage(ret))
            {
                // Base floor
                using (var tileBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Black))
                {
                    for (int n = 0; n < floor.Grid.TileCount; n++)
                    {
                        var curTile = floor.Grid[n];

                        int x = n % floor.Grid.Stride;
                        int y = (n - x) / floor.Grid.Stride;
                        float curX = x * bld.Resolution;
                        float curY = y * bld.Resolution;

                            
                        switch (curTile)
                        {
                            case FloorTileType.Vacant:
                                tileBrush.Color = System.Drawing.Color.Black;
                                break;
                            case FloorTileType.Passage:
                                tileBrush.Color = System.Drawing.Color.White;
                                break;
                            case FloorTileType.Room:
                                tileBrush.Color = System.Drawing.Color.LightGreen;
                                break;
                            default:
                                tileBrush.Color = System.Drawing.Color.LightSlateGray;
                                break;
                        }

                        gfx.FillRectangle(tileBrush, curX * scaleFactorHorizontal, curY * scaleFactorVertical,
                                bld.Resolution * scaleFactorHorizontal, bld.Resolution * scaleFactorVertical);
                    }
                }

                // Entrances
                using (var tileBrush = new System.Drawing.SolidBrush(System.Drawing.Color.LightSalmon))
                {
                    var nfont = new System.Drawing.Font("Area", 8f);
                    var textBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Blue);

                    foreach(var entrance in floor.Entrances)
                    {
                        float x = (entrance.GridPosition.X * bld.Resolution) * scaleFactorHorizontal;
                        float y = (entrance.GridPosition.Y * bld.Resolution) * scaleFactorVertical;

                        gfx.FillRectangle(tileBrush, x, y, bld.Resolution * scaleFactorHorizontal, bld.Resolution * scaleFactorVertical);
                        gfx.DrawString(entrance.Direction.ToString().Substring(0, 1), nfont, textBrush, x, y);
                    }

                    nfont.Dispose();
                    textBrush.Dispose();
                }

                // Room outlines
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.DarkGray))
                {
                    foreach (var room in floor.Rooms)
                    {
                        float x = (room.TopLeft.X * bld.Resolution) * scaleFactorHorizontal;
                        float y = (room.TopLeft.Y * bld.Resolution) * scaleFactorVertical;
                        float xx = (room.BottomRight.X * bld.Resolution) * scaleFactorHorizontal;
                        float yy = (room.BottomRight.Y * bld.Resolution) * scaleFactorVertical;

                        gfx.DrawRectangle(pen, x, y, xx - x, yy - y);
                    }
                }
            }

            return ret;
        }
    }
}
