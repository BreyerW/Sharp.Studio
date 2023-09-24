using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharp.DockManager
{
	public class DockToOrientationConverter : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			var dock = (Dock)value;
			return dock switch
			{
				Dock.Left or Dock.Right => Orientation.Vertical,
				Dock.Top or Dock.Bottom => Orientation.Horizontal
			};
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
