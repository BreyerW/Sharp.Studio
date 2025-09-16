using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Sharp.DockManager
{
    public static class SimpleConverters
    {
        public static FuncValueConverter<Rect, string> SizeToTabShapeConverter { get; } =
        new FuncValueConverter<Rect, string>(bounds => {
            double width = bounds.Size.Width - 1;
            double height = bounds.Size.Height;
            double x1 = width - 15;
            double x2 = width - 10;
            double x3 = width - 5;
            double x4 = width - 2.5;
            double x5 = width;
            return string.Format(CultureInfo.InvariantCulture, "M0,{5} C2.5,{5} 5,0 10,0 15,0 {0},0 {1},0 {2},0 {3},{5} {4},{5}", x1, x2, x3, x4, x5, height);
        });
    }
}
