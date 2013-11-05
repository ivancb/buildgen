using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildGen.Data
{
    public class Region
    {
        public enum OperationType
        {
            Add,
            Subtract,
        };

        private Rectangle bounds;
        private List<Tuple<Rectangle, OperationType>> operationBounds;
        private List<Rectangle> cachedResults;
        private bool invalidated;

        public Rectangle Bounds
        {
            get { return bounds; }
            set { Reset(value); }
        }

        public float Area
        {
            get
            {
                var rectangles = Evaluate();
                float totalArea = 0f;

                foreach (var rect in rectangles)
                {
                    totalArea += rect.Area;
                }

                return totalArea;
            }
        }

        public Region(Rectangle newBounds) 
        { 
            bounds = newBounds;
            operationBounds = new List<Tuple<Rectangle, OperationType>>();
            invalidated = true; 
        }

        public Region(int x, int y, int xx, int yy) 
        { 
            bounds = new Rectangle(x, y, xx, yy);
            operationBounds = new List<Tuple<Rectangle, OperationType>>();
            invalidated = true; 
        }

        public void Reset(Rectangle newBounds)
        {
            Clear();
            bounds = newBounds;
            invalidated = true;
        }

        public void Clear()
        {
            operationBounds.Clear();
            invalidated = true;
        }

        public void Add(int x, int y, int xx, int yy)
        {
            Add(new Rectangle(x, y, xx, yy));
        }

        public void Add(Rectangle rect)
        {
            if (!bounds.Contains(rect) || (operationBounds.Count() > 0))
            {
                RemoveOp(rect);

                operationBounds.Add(new Tuple<Rectangle, OperationType>(rect, OperationType.Add));
                invalidated = true;
            }
        }

        public void Subtract(int x, int y, int xx, int yy)
        {
            Subtract(new Rectangle(x, y, xx, yy));
        }

        public void Subtract(Rectangle rect)
        {
            RemoveOp(rect);
            operationBounds.Add(new Tuple<Rectangle, OperationType>(rect, OperationType.Subtract));
            invalidated = true;
        }

        public void RemoveOp(Rectangle rect)
        {
            for (int n = 0; n < operationBounds.Count(); n++)
            {
                Rectangle cOp = operationBounds[n].Item1;

                if ((cOp.X == rect.X) && (cOp.Y == rect.Y) &&
                    (cOp.XX == rect.XX) && (cOp.YY == rect.YY))
                {
                    operationBounds.RemoveAt(n);
                    break;
                }
            }
        }

        public List<Rectangle> Evaluate()
        {
            if (!invalidated)
                return cachedResults;

            List<Rectangle> ret = new List<Rectangle>();
            ret.Add(bounds);

            foreach(var op in operationBounds)
            {
                if (op.Item2 == OperationType.Add)
                    ret = EvaluateAddition(ret, op.Item1);
                else if(op.Item2 == OperationType.Subtract)
                    ret = EvaluateSubtraction(ret, op.Item1);

                if (ret.Count() == 0)
                    break;
            }

            cachedResults = ret;
            invalidated = false;

            return ret;
        }

        public Region Clone()
        {
            Region ret = new Region(bounds.Clone());

            foreach (var op in operationBounds)
            {
                ret.operationBounds.Add(new Tuple<Rectangle, OperationType>(op.Item1.Clone(), op.Item2));
            }

            ret.invalidated = true;
            return ret;
        }

        public List<Rectangle> GetOperationRects(OperationType type)
        {
            List<Rectangle> ret = new List<Rectangle>();

            foreach (var op in operationBounds)
            {
                if (op.Item2 == type)
                    ret.Add(op.Item1);
            }

            return ret;
        }

        private List<Rectangle> EvaluateAddition(List<Rectangle> rects, Rectangle subtractedRect)
        {
            List<Rectangle> ret = new List<Rectangle>();

            foreach (Rectangle rect in rects)
            {
                var subResults = rect.Add(subtractedRect);

                if (subResults != null)
                    ret = subResults.Union(ret).ToList();
            }

            return ret;
        }

        private List<Rectangle> EvaluateSubtraction(List<Rectangle> rects, Rectangle subtractedRect)
        {
            List<Rectangle> ret = new List<Rectangle>();

            foreach(Rectangle rect in rects)
            {
                var subResults = rect.Subtract(subtractedRect);

                if(subResults != null)
                    ret = subResults.Union(ret).ToList();
            }

            return ret;
        }
    }
}
