using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapEditor
{
    public static class RectangleDecomposition
    {     
        public static Tuple<int,int> getSmartPosition(List<System.Drawing.Rectangle> listRect, System.Drawing.Rectangle rect)
        {

            HashSet<Rectangle> hsRect = new HashSet<Rectangle>(listRect);
            List<Rectangle> listGotCollide = new List<Rectangle>();

            foreach (var item in hsRect)
            {
                Rectangle intersect = Rectangle.Intersect(item, rect);
                if (intersect.Width > 1 && intersect.Height > 1) 
                {
                    listGotCollide.Add(item);
                }
            }
            
            int xOuter = listGotCollide.Min(item => item.X);
            int yOuter = listGotCollide.Min(item => item.Y);

            int xInner = xOuter + 32;
            int yInner = yOuter + 32;

            int xResult = Math.Abs(rect.X - xOuter) > Math.Abs(rect.X - xInner) ? xInner : xOuter;
            int yResult = Math.Abs(rect.Y - yOuter) > Math.Abs(rect.Y - yInner) ? yInner : yOuter;

            Tuple<int, int> result = new Tuple<int, int>(xResult, yResult);

            return result;
        }

        
    }
}
