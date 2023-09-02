using Avalonia;
using Avalonia.Controls;
using System.Collections.Generic;

namespace Sharp.DockManager
{
	internal static class Helpers
	{
		public static readonly StyledProperty<double> TabItemXProperty =
				AvaloniaProperty.Register<TabItem, double>("X");

		public static readonly StyledProperty<double> TabItemYProperty =
			AvaloniaProperty.Register<TabItem, double>("Y");
		public static void SwapWith<T>(this LinkedListNode<T> first, LinkedListNode<T> second)
		{
			first.List.Remove(first);
			second.List.AddAfter(second, first);
		}
	}
}
