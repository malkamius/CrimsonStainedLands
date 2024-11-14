using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using CLSMapper;
using Microsoft.VisualBasic.ApplicationServices;
using SkiaSharp;
using CrimsonStainedLands.World;
internal class Drawer
{
    public class Box
    {
        internal Dictionary<Direction, Box> Exits = new Dictionary<Direction, Box>();

        public int x { get; set; }

        public int y { get; set; }

        public Rectangle drawlocation;

        public int XOffsetForZone;
        public int width { get; set; }

        public int height { get; set; }

        public string text { get; set; } = "";

        public SkiaSharp.SKColor OriginalBackColor { get; internal set; } = SkiaSharp.SKColors.White;
        public SkiaSharp.SKColor BackColor { get; internal set; } = SkiaSharp.SKColors.White;

    }

    public static List<Box> Boxes { get; set; } = new List<Box>();


    public static Point Origin { get; set; } = Point.Empty;


    public static SKBitmap? Draw(int XOffsetForZone = 0)
    {
        if (Drawer.Boxes.Count == 0) return null;
        int minY = Drawer.Boxes.Min((Drawer.Box b) => b.y);
        int maxY = Drawer.Boxes.Max((Drawer.Box b) => b.y);
        var yoffset = 0 - minY;

        int minX = Drawer.Boxes.Min((Drawer.Box b) => b.x);
        int maxX = Drawer.Boxes.Max((Drawer.Box b) => b.x);
        var xoffset = 0 - minX;

        var boxheight = 50;

        int height = (maxY - minY) * boxheight + boxheight * 2;
        
        using (var paint = new SkiaSharp.SKPaint())
        {
            var columnwidths = new Dictionary<int, int>();
            var columnstarts = new Dictionary<int, int>();

            foreach (var box in Drawer.Boxes)
            {
                columnwidths.TryGetValue(xoffset + box.x, out var columnwidth);
                box.width = (int)Math.Ceiling(paint.MeasureText(box.text)) + 20;
                if (columnwidth < box.width)
                    columnwidths[xoffset + box.x] = box.width;
            }

            for(int ix = 0; ix < maxX + xoffset; ix++)
                if(!columnwidths.ContainsKey(ix))
                    columnwidths[ix] = 50;

            var widthsum = 0;
            for (int i = 0; i < columnwidths.Count; i++)
            {
                columnstarts[i] = widthsum + 10;

                try
                {
                    widthsum += columnwidths[i] + 10;
                }
                catch { }
            }

            var skBitmap = new SKBitmap(widthsum + 20, height);
            using (var canvas = new SKCanvas(skBitmap))
            {
                canvas.Clear(SKColors.White);

                foreach (Drawer.Box box in Drawer.Boxes)
                {
                    var keyx = xoffset + box.x;
                    
                    paint.StrokeWidth = 1;
                    paint.Style = SKPaintStyle.Fill;
                    paint.Color = box.BackColor;
                    canvas.DrawRect(columnstarts[keyx] + 5, (yoffset + box.y) * boxheight + 10, columnwidths[keyx], boxheight - 10, paint);

                    paint.StrokeWidth = 2;
                    paint.Color = SKColors.Black;
                    paint.Style = SKPaintStyle.Stroke;
                    canvas.DrawRect(columnstarts[keyx] + 5, (yoffset + box.y) * boxheight + 10, columnwidths[keyx], boxheight - 10, paint);

                    paint.StrokeWidth = 1;
                    paint.Style = SKPaintStyle.Fill;
                    paint.TextAlign = SKTextAlign.Center;
                    canvas.DrawText(box.text, columnstarts[xoffset + box.x] + (columnwidths[xoffset + box.x] / 2) + 10, (yoffset + box.y) * boxheight + boxheight / 2, paint);

                    box.drawlocation = new Rectangle(columnstarts[keyx] + 5, (yoffset + box.y) * boxheight + 10, columnwidths[keyx], boxheight - 10);
                    box.XOffsetForZone = XOffsetForZone;
                }

                foreach (Drawer.Box box in Drawer.Boxes)
                {
                    paint.StrokeWidth = 1;
                    paint.Style = SKPaintStyle.Stroke;
                    foreach (var exit in box.Exits.Where(x => x.Value != null))
                    {
                        if (exit.Value is null) continue;
                        var startx = columnstarts[xoffset + box.x] + (columnwidths[xoffset + exit.Value.x] / 2) + 10;
                        var starty = (yoffset + box.y) * boxheight + 10 + (boxheight - 10) / 2;
                        var endx = columnstarts[xoffset + exit.Value.x] + (columnwidths[xoffset + exit.Value.x] / 2) + 10;
                        var endy = (yoffset + exit.Value.y) * boxheight + 10 + (boxheight - 10) / 2;

                        switch (exit.Key)
                        {
                            case Direction.North:
                                starty = (yoffset + box.y) * boxheight + 10;
                                endy = (yoffset + exit.Value.y) * boxheight + 10 + boxheight - 10;
                                break;
                            case Direction.South:
                                starty = (yoffset + box.y) * boxheight + 10 + boxheight - 10;
                                endy = (yoffset + exit.Value.y) * boxheight + 10;
                                break;
                            case Direction.East:
                                startx = columnstarts[xoffset + box.x] + 5 + columnwidths[xoffset + box.x];
                                endx = columnstarts[xoffset + exit.Value.x] + 5;
                                break;
                            case Direction.West:
                                startx = columnstarts[xoffset + box.x] + 5;
                                endx = columnstarts[xoffset + exit.Value.x] + 5 + columnwidths[xoffset + exit.Value.x];
                                break;
                            case Direction.Up:
                                startx = columnstarts[xoffset + box.x] + 5 + columnwidths[xoffset + box.x];
                                starty = (yoffset + box.y) * boxheight + 10;
                                endx = columnstarts[xoffset + exit.Value.x] + 5;
                                endy = (yoffset + exit.Value.y) * boxheight + 10 + boxheight - 10;
                                break;
                            case Direction.Down:
                                startx = columnstarts[xoffset + box.x] + 5;
                                starty = (yoffset + box.y) * boxheight + 10 + boxheight - 10;
                                endx = columnstarts[xoffset + exit.Value.x] + 5 + columnwidths[xoffset + exit.Value.x];
                                endy = (yoffset + exit.Value.y) * boxheight + 10;
                                break;
                        }

                        canvas.DrawLine(startx, starty, endx, endy, paint);
                    }
                }
                return skBitmap;
                
            }
        }
    }
}
