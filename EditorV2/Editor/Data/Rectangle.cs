using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildGen.Data
{
    public struct Rectangle
    {
        public Point TopLeft;
        public Point BottomRight;

        public int X { get { return TopLeft.X; } }
        public int Y { get { return TopLeft.Y; } }
        public int XX { get { return BottomRight.X; } }
        public int YY { get { return BottomRight.Y; } }
        public float Area { get { return Width * Height; } }
        public bool Valid { get { return (TopLeft != BottomRight) && (X < XX) && (Y < YY); } }

        public int Width
        {
            get { return BottomRight.X - TopLeft.X; }
            set { BottomRight.X = TopLeft.X + value; }
        }

        public int Height
        {
            get { return BottomRight.Y - TopLeft.Y; }
            set { BottomRight.Y = TopLeft.Y + value; }
        }

        public Rectangle(int x, int y, int xx, int yy)
        {
            TopLeft = new Point(x, y);
            BottomRight = new Point(xx, yy);
        }

        public Rectangle Intersect(Rectangle rect)
        {
            if ((rect.XX < X) || (rect.YY < Y) || (rect.X > XX) || (rect.Y > YY))
            {
                return new Rectangle(0, 0, 0, 0);
            }
            else
            {
                return new Rectangle(Math.Max(X, rect.X), Math.Max(Y, rect.Y), Math.Min(XX, rect.XX), Math.Min(YY, rect.YY));
            }
        }

        public List<Rectangle> Add(Rectangle rect)
        {
            List<Rectangle> ret = new List<Rectangle>();
            ret.Add(this);
            ret = ret.Union(rect.Subtract(this)).ToList();

            return ret;
        }

        public List<Rectangle> Subtract(Rectangle rect)
        {
            if (rect.Contains(this))
                return null;

            Rectangle intersection = Intersect(rect);
            List<Rectangle> res = new List<Rectangle>();

            if (!intersection.Valid)
            {
                res.Add(this);
            }
            else
            {
                res.Add(new Rectangle(Math.Min(X, intersection.X), Math.Min(Y, intersection.Y), intersection.X, Math.Max(YY, intersection.Y)));
                res.Add(new Rectangle(Math.Min(XX, intersection.X), Y, XX, intersection.Y));
                res.Add(new Rectangle(intersection.XX, intersection.Y, XX, intersection.YY));
                res.Add(new Rectangle(intersection.X, intersection.YY, XX, YY));

                for (int n = res.Count - 1; n >= 0; n--)
                {
                    if (!res[n].Valid)
                        res.RemoveAt(n);
                }

                if (res.Count == 0)
                    res.Add(this);
            }

            return res;
        }

        public bool Contains(Rectangle rect)
        {
            return ((rect.X >= X) && (rect.Y >= Y) &&
                (rect.XX <= XX) && (rect.YY <= YY));
        }

        public bool Contains(int x, int y)
        {
            return ((x >= X) && (y >= Y) &&
                (x <= XX) && (y <= YY));
        }

        public Rectangle Clone()
        {
            return new Rectangle(X, Y, XX, YY);
        }

        public bool IsValid()
        {
            return (Width != 0) && (Height != 0);
        }

        public override string ToString()
        {
            return "x=" + X + " y=" + Y + " xx=" + XX + " yy=" + YY;
        }
    }

    public struct RectangleF
    {
        public PointF TopLeft;
        public PointF BottomRight;

        public float X { get { return TopLeft.X; } }
        public float Y { get { return TopLeft.Y; } }
        public float XX { get { return BottomRight.X; } }
        public float YY { get { return BottomRight.Y; } }
        public float Area { get { return Width * Height; } }
        public bool Valid { get { return (TopLeft != BottomRight) && (X < XX) && (Y < YY); } }

        public float Width
        {
            get { return BottomRight.X - TopLeft.X; }
            set { BottomRight.X = TopLeft.X + value; }
        }

        public float Height
        {
            get { return BottomRight.Y - TopLeft.Y; }
            set { BottomRight.Y = TopLeft.Y + value; }
        }

        public RectangleF(float x, float y, float xx, float yy)
        {
            TopLeft = new PointF(x, y);
            BottomRight = new PointF(xx, yy);
        }

        public RectangleF Intersect(RectangleF rect)
        {
            if ((rect.XX < X) || (rect.YY < Y) || (rect.X > XX) || (rect.Y > YY))
            {
                return new RectangleF(0f, 0f, 0f, 0f);
            }
            else
            {
                return new RectangleF(Math.Max(X, rect.X), Math.Max(Y, rect.Y), Math.Min(XX, rect.XX), Math.Min(YY, rect.YY));
            }
        }

        public List<RectangleF> Add(RectangleF rect)
        {
            List<RectangleF> ret = new List<RectangleF>();
            ret.Add(this);
            ret = ret.Union(rect.Subtract(this)).ToList();

            return ret;
        }

        public List<RectangleF> Subtract(RectangleF rect)
        {
            if (rect.Contains(this))
                return null;

            RectangleF intersection = Intersect(rect);
            List<RectangleF> res = new List<RectangleF>();

            if (!intersection.Valid)
            {
                res.Add(this);
            }
            else
            {
                res.Add(new RectangleF(Math.Min(X, intersection.X), Math.Min(Y, intersection.Y), intersection.X, Math.Max(YY, intersection.Y)));
                res.Add(new RectangleF(Math.Min(XX, intersection.X), Y, XX, intersection.Y));
                res.Add(new RectangleF(intersection.XX, intersection.Y, XX, intersection.YY));
                res.Add(new RectangleF(intersection.X, intersection.YY, XX, YY));

                for (int n = res.Count - 1; n >= 0; n--)
                {
                    if (!res[n].Valid)
                        res.RemoveAt(n);
                }

                if (res.Count == 0)
                    res.Add(this);
            }

            return res;
        }

        public bool Contains(RectangleF rect)
        {
            return ((rect.X >= X) && (rect.Y >= Y) &&
                (rect.XX <= XX) && (rect.YY <= YY));
        }

        public bool Contains(float x, float y)
        {
            return ((x >= X) && (y >= Y) &&
                (x <= XX) && (y <= YY));
        }

        public RectangleF Clone()
        {
            return new RectangleF(X, Y, XX, YY);
        }

        public override string ToString()
        {
            return "x=" + X + " y=" + Y + " xx=" + XX + " yy=" + YY;
        }
    }
}
