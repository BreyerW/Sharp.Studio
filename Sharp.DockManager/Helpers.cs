using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
		public static bool ReplaceWith(this Control toBeReplaced, Control replacement)
		{
			var parent = toBeReplaced.GetLogicalParent();
			if (parent is Panel p)
			{
				var i = p.Children.IndexOf(toBeReplaced);
				if (i > -1)
				{
					p.Children[i] = replacement;
					return true;
				}
				return false;
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
		public static T FindAncestorOfTypeAndName<T>(this Control c, string name, bool includeSelf = false) where T: Control
		{
			var found = c.FindAncestorOfType<T>(includeSelf);
			while (found is not null && found.Name != name)
			//do not include includeSelf here to prevent infinite loop
				found = found.FindAncestorOfType<T>();
			return found;
		}
		internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

			//TODO: XQueryTree for X11 based Linux and for macos NSWindow.orderedIndex
		[DllImport("USER32.DLL")]
		internal static extern bool EnumWindows(EnumWindowsProc enumFunc, IntPtr lParam);
	}
}
