using Avalonia.Data.Converters;
using Avalonia.Input;
using System;
using System.Globalization;

namespace Sharp.DockManager
{
    public class CursorConverter : IValueConverter
    {
        public static CursorConverter Instance { get; } = new CursorConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var position = (Avalonia.Controls.Dock)value;
            var isHorizontal = GetIsHorizontal(position);
            return new Cursor(isHorizontal ? StandardCursorType.SizeNorthSouth : StandardCursorType.SizeWestEast);
        }
        static bool GetIsHorizontal(Avalonia.Controls.Dock position)
           => position == Avalonia.Controls.Dock.Top || position == Avalonia.Controls.Dock.Bottom;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
                   => throw new NotImplementedException();
    }
}