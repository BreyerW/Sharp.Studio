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
	public static class Helpers
	{
		public static void CopyGridProperties(this Control source, Control copyTo)
		{
			Grid.SetColumn(copyTo, Grid.GetColumn(source));
			Grid.SetColumnSpan(copyTo, Grid.GetColumnSpan(source));
			Grid.SetRow(copyTo, Grid.GetRow(source));
			Grid.SetRowSpan(copyTo, Grid.GetRowSpan(source));
		}
		public static void SwapWith<T>(this LinkedListNode<T> first, LinkedListNode<T> second)
		{
			first.List.Remove(first);
			second.List.AddAfter(second, first);
		}
		//can always use DetachedFrom*Tree to clean up any special containers like Grid with splitters
		//or listen to on removed event on collections
		public static void ReplaceWith(this Control toBeReplaced, Control? replacement)
		{
			var parent = toBeReplaced.GetLogicalParent();
			if (parent is Panel p)
			{
				if (replacement is null)
				{
					p.Children.Remove(toBeReplaced);
				}
				else
				{
					var i = p.Children.IndexOf(toBeReplaced);
					if (i > -1)
					{
						p.Children[i] = replacement;
					}
				}
			}
			else if (parent is ContentControl cc)
			{
				cc.Content = replacement;
			}
			else if (parent is ContentPresenter cp)
			{
				cp.Content = replacement;
			}
			else if (parent is Decorator d)
			{
				d.Child = replacement;
			}
			else
				DockableControl.ReplaceControlRequested(toBeReplaced,replacement);
		}

		internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

		[DllImport("libX11.so")]
		internal static extern IntPtr XOpenDisplay(string name);
		[DllImport("libX11.so")]
		internal static extern int XCloseDisplay(IntPtr display);
		[DllImport("libX11.so")]
		internal static extern IntPtr XDefaultRootWindow(IntPtr display);
		[DllImport("libX11.so")]
		internal static extern int XQueryTree(IntPtr display, IntPtr w, out IntPtr root_return, out IntPtr parent_return, out IntPtr children_return, out int nchildren_return);
		[DllImport("USER32.DLL")]
		internal static extern bool EnumWindows(EnumWindowsProc enumFunc, IntPtr lParam);
	}
}
