using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Sharp.DockManager
{
    public static class SimpleConverters
    {
        //https://github.com/sskodje/wpfchrometabs-mvvm/blob/master/ChromeTabs/TabShape.cs
        public static FuncValueConverter<Rect, Geometry> ContentToTabShapeConverter { get; } =
        new FuncValueConverter<Rect, Geometry>(value => {

            var ps = new PathSegments();
            double h = value.Height;
            double w = value.Width;
            // Smaller unit, so don't need fractional multipliers.
            double u = 0.1 * h;
            // HACK: Start before "normal" start of tab.
            double x0 = 0;
            // end of transition
            double x9 = w;
            // transition width
            double tw = 5.5 * u;
            // top "radius" (actually, gradualness of curve. Larger value is more rounded.)
            double rt = 2.5 * u;
            // bottom "radius" (actually, gradualness of curve. Larger value is more rounded.)
            double rb = 2.5 * u;
            // "(x0, 0)" is start point - defined in PathFigure.
            // Cubic: From previous endpoint, 2 control points + new endpoint.
            var bezier = new BezierSegment();
            bezier.Point1 = new Point(x0 + rb, h);
            bezier.Point2 = new Point(x0 + tw - rt, 0);
            bezier.Point3 = new Point(x0 + tw, 0);
            ps.Add(bezier);
            var line = new LineSegment();
            line.Point = new Point(x9 - tw, 0);
            ps.Add(line);
            var bezier1 = new BezierSegment();
            bezier1.Point1 = new Point(x9 - tw + rt, 0);
            bezier1.Point2 = new Point(x9 - rb, h);
            bezier1.Point3 = new Point(x9, h);
            ps.Add(bezier1);

            // "(x0, 0)" is start point.
            PathFigure figure = new PathFigure();
            figure.StartPoint = new Point(x0, h);
            figure.Segments.AddRange(ps);
            figure.IsClosed = false;
            PathGeometry geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            return geometry;
        });
    }
}
