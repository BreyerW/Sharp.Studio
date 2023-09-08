using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.LogicalTree;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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
		public static bool ReplaceChild(this Control toBeReplaced, Control replacement)
		{
			var parent = toBeReplaced.GetLogicalParent();
			if (parent is Panel p)
			{
				p.Children.Remove(toBeReplaced);
				p.Children.Add(replacement);
				return true;
			}
			else if (parent is ContentControl cc)
			{
				cc.Content = replacement;
				return true;
			}
			else if (parent is ContentPresenter cp)
			{
				cp.Content = replacement;
				return true;
			}
			else if (parent is Decorator d)
			{
				d.Child = replacement;
				return true;
			}
			return false;
		}
		internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

			//TODO: XQueryTree for X11 based Linux and for macos NSWindow.orderedIndex
		[DllImport("USER32.DLL")]
		internal static extern bool EnumWindows(EnumWindowsProc enumFunc, IntPtr lParam);
	}
}
