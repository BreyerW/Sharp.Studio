using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Sharp.DockManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Threading;
using System.Globalization;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml;
using System.Reflection;
using Avalonia.Platform;

namespace Sharp.DockManager
{
	public enum Region
	{
		None,
		Left,
		Right,
		Top,
		Bottom,
		Center
	}
	public partial class DockableControl : TabControl, IStyleable
	{
		private static bool isDragging = false;
		
		private readonly static Window draggedItem = new Window() {
			ShowActivated = false,
			Topmost = true,
			SystemDecorations = SystemDecorations.None,
			ExtendClientAreaTitleBarHeightHint = 0,
			ExtendClientAreaToDecorationsHint = true,
			ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome,
			ShowInTaskbar = false
		};

		private static (Control control, Region area) lastTrigger = default;
		protected static DockableControl sourceDockable = null;
		private static PixelPoint screenMousePosOffset;

		private static DockableItem selectedItem;
		private static Dictionary<Window, int> sortedWindows = new();
		internal static Border adornedElement = new Border();
		internal static Canvas canvas = new Canvas();
		internal ScrollViewer scroller;
		private ItemsPresenter header;
		private ContentPresenter body;
		private static IntPtr displayHandle;

		Type IStyleable.StyleKey => typeof(DockableControl);
		public static Action<Control, Control> ReplaceControlRequested
		{
			get;
			set;
		}
		public static SolidColorBrush PreviewBrush
		{
			set => draggedItem.Background = value;
		}
		public DockableTabViewModel TabItems { get; set; } = new();
		private static int index = 0;

		/// <summary>
		/// Gets the z-order for one or more windows atomically with respect to each other. In Windows, smaller z-order is higher. If the window is not top level, the z order is returned as -1. 
		/// </summary>
		private static void UpdateZOrder()
		{
			index = 0;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
			{
				if (displayHandle == IntPtr.Zero)
					displayHandle = Helpers.XOpenDisplay(null);

				var root = Helpers.XDefaultRootWindow(displayHandle);
				Helpers.XQueryTree(displayHandle, root, out _, out _, out var ptrToChilds, out var count);
				if (count > 0)
				{
					var childs = MemoryMarshal.CreateReadOnlySpan(ref ptrToChilds, count);
					foreach (var child in childs)
					{
						TestWindowHandle(child);
					}
				}
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
			}
			else
				Helpers.EnumWindows((wnd, param) =>
				{
					return TestWindowHandle(wnd);
				}, IntPtr.Zero);
		}
		private static bool TestWindowHandle(IntPtr wnd)
		{
			if (draggedItem.TryGetPlatformHandle()?.Handle == wnd)
			{
				//skip draggable window
				index++;
				return true;
			}
			var lifetime = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime);
			foreach (var wind in lifetime.Windows)
				if (wind != draggedItem && wind.TryGetPlatformHandle().Handle == wnd)
					CollectionsMarshal.GetValueRefOrAddDefault(sortedWindows, wind, out _) = index;
			index++;
			return true;
		}
		static DockableControl()
		{
			PreviewBrush = new SolidColorBrush(Colors.PaleVioletRed, 0.33);
			InputElement.PointerPressedEvent.AddClassHandler<TabItem>((s, e) =>
			{
				e.Handled = true;
				if (DockManager.GetAllowDrag(s) is false)
					return;
			//use hittesting and if tab item is found capture its panel
				PointerPressedOnTabItem(e);
			});
			InputElement.PointerMovedEvent.AddClassHandler<Interactive>((s, e) =>
			{
				if (selectedItem is null)
					return;
				PointerMoved(e);
				if(isDragging)
					DoDrag(e);
				e.Handled = true;
			});
			InputElement.PointerReleasedEvent.AddClassHandler<Interactive>((s, e) =>
			{
				if (selectedItem is null || isDragging is false)
					return;
				DropTab(e);
				DropFinished(e);
				e.Handled = true;
			});

			adornedElement.Background = Brushes.PaleVioletRed;
			adornedElement.Opacity = 0.33;
			adornedElement.IsVisible = true;
			canvas.Children.Add(adornedElement);
		}
		private static Grid GridFactory() => new Grid() { Name="dockable", RowDefinitions = new("*,Auto,*"), ColumnDefinitions = new("*,Auto,*") };
		private static GridSplitter SplitterFactory(double thickness, bool column)
		{
			var splitter = new GridSplitter()
			{
				Width = column ? thickness : double.NaN,
				Height = column ? double.NaN : thickness,
				MinWidth = 0,
				MinHeight = 0,
				ResizeDirection = column ? GridResizeDirection.Columns : GridResizeDirection.Rows
			};
			if (column)
			{
				SetAsColumn(splitter, 1);
			}
			else
			{
				SetAsRow(splitter, 1);
			}
			return splitter;
		}
		private static void SetAsColumn(Control c, int column)
		{
			Grid.SetRow(c, 0);
			Grid.SetRowSpan(c, 3);
			Grid.SetColumn(c, column);
			Grid.SetColumnSpan(c, 1);
		}
		private static void SetAsRow(Control c, int row)
		{
			Grid.SetRow(c, row);
			Grid.SetRowSpan(c, 1);
			Grid.SetColumn(c, 0);
			Grid.SetColumnSpan(c, 3);
		}

		private static void SplitTabControl(Control existing, Control added, Region dockForAdded)
		{
			var grid=GridFactory();

			existing.ReplaceWith(grid);
			Helpers.CopyGridProperties(existing, grid);
			var splitter = SplitterFactory(2, dockForAdded is Region.Left or Region.Right);
			grid.Children.Add(existing);
			grid.Children.Add(splitter);
			grid.Children.Add(added);
			if (dockForAdded is Region.Left)
			{
				SetAsColumn(added, 0);
				SetAsColumn(existing, 2);
			}
			else if (dockForAdded is Region.Right)
			{
				SetAsColumn(added, 2);
				SetAsColumn(existing, 0);
			}
			else if (dockForAdded is Region.Top)
			{
				SetAsRow(added, 0);
				SetAsRow(existing, 2);
			}
			else
			{
				SetAsRow(added, 2);
				SetAsRow(existing, 0);
			}
		}
		public DockableControl()
		{
			InitializeComponent();
			ItemsSource = TabItems.Items;
			Background = new SolidColorBrush(new Color(255,56,56,56));
		}
		//https://github.com/sskodje/wpfchrometabs-mvvm/blob/master/ChromeTabs/TabShape.cs
		private Geometry GetGeometry()
		{
			/*if (TabShapePath != null)
			{
				return TabShapePath.Data;
			}*/
			double width = DesiredSize.Width - 1;

			double height = 25;
			double x1 = width - 15;
			double x2 = width - 10;
			double x3 = width - 5;
			double x4 = width - 2.5;
			double x5 = width;

			return Geometry.Parse(string.Format(CultureInfo.InvariantCulture, "M0,{5} C2.5,{5} 5,0 10,0 15,0 {0},0 {1},0 {2},0 {3},{5} {4},{5}", x1, x2, x3, x4, x5, height));
		}
		private static void PointerPressedOnTabItem(PointerPressedEventArgs e)
		{
			var s = ((Control)e.Source).FindAncestorOfType<TabItem>(true);
			if (s is null)
				return;
			s.ZIndex = int.MaxValue;
			sourceDockable = s.FindAncestorOfType<DockableControl>();
			sourceDockable.SelectedItem = s.Content;
			screenMousePosOffset = s.PointToScreen(e.GetPosition(s)) - s.GetVisualParent().PointToScreen(s.Bounds.Position);
			selectedItem = s.Content as DockableItem;
		}
		
		private static void DropFinished(PointerReleasedEventArgs e)
		{
			sourceDockable = null;
			canvas.IsVisible = false;
			selectedItem = null;
			lastTrigger = default;
			screenMousePosOffset = default;
			draggedItem.IsVisible = false;
			isDragging = false;
		}
		private static async void PointerMoved(PointerEventArgs e)
		{
			if (selectedItem is not null && e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
			{
				if (sourceDockable.scroller is null)
					return;
					
				var scroll = sourceDockable.header.Panel;
				var tab = sourceDockable.ContainerFromIndex(sourceDockable.SelectedIndex);
				var scrollerMousePos = e.GetPosition(scroll);
				
				if (tab.Bounds.Contains(scrollerMousePos))
					return;
				isDragging = true;
				UpdateZOrder();
				
				var Width = sourceDockable.Bounds.Width;
				var Height = sourceDockable.Bounds.Height;

				draggedItem.Width = Width;
				draggedItem.Height = Height;
			}
		}
		private static void DoDrag(PointerEventArgs e)
		{
			Point mousePos = e.GetPosition(draggedItem);
			var screenPos = draggedItem.PointToScreen(mousePos);
			draggedItem.Position = screenPos - screenMousePosOffset;
			(Control control, Region area) currentTrigger = default;

			Control? hit = null;
			foreach (var (window, zorder) in sortedWindows.OrderBy(key => key.Value))
			{
				var pos = window.PointToClient(screenPos);
				if (pos is { X: 0, Y: 0 })
					continue;

				hit = window.GetVisualAt(pos, c => c is not Border /*AdornerLayer.GetAdornedElement(c as Visual) is null*/) as Control;
				if (hit is not null)
				{
					var targetDockable = hit.FindAncestorOfType<DockableControl>(true);
					
					if (targetDockable is not null)
					{
						(bool Top, bool Bottom, bool Left, bool Right, bool Center, bool Header) allow =
						(
							DockManager.GetAllowTopDrop(targetDockable),
							DockManager.GetAllowBottomDrop(targetDockable),
							DockManager.GetAllowLeftDrop(targetDockable),
							DockManager.GetAllowRightDrop(targetDockable),
							DockManager.GetAllowCenterDrop(targetDockable),
							DockManager.GetAllowHeaderDrop(targetDockable)
						);
						var noRegionIsAllowed = !allow.Top && !allow.Bottom && !allow.Left && !allow.Right && !allow.Center && !allow.Header;
						if (noRegionIsAllowed is true)
						{
							currentTrigger = (null, Region.None);
							break;
						}
						var dPos = e.GetPosition(targetDockable);
						if (targetDockable.scroller.Bounds.Contains(dPos))
						{
							Control tab = null;
							var scrollerPos = e.GetPosition(targetDockable.header.Panel);
							foreach (var t in targetDockable.header.Panel.Children)
							{
								if (t.Bounds.Contains(scrollerPos))
								{
									tab = t;
									break;
								}
							}
							if (allow.Header && tab is not null && DockManager.GetAllowDrop(tab))
							{
								currentTrigger = (tab, Region.Center);
							}
							else
							{
								currentTrigger = (null, Region.None);
								break;
							}
						}
						else
						{
							var posInBody = e.GetPosition(targetDockable.body);
							if (posInBody is { X: >= 0, Y: >= 0 })
							{
								if (sourceDockable == targetDockable && allow.Center && sourceDockable.Items.Count is 1)
									currentTrigger = (targetDockable.body, Region.Center);
								else if (allow.Left && posInBody.X < targetDockable.body.Bounds.Width * 0.25)
									currentTrigger = (targetDockable.body, Region.Left);
								else if (allow.Right && posInBody.X > targetDockable.body.Bounds.Width * 0.75)
									currentTrigger = (targetDockable.body, Region.Right);
								else if (allow.Top && posInBody.Y < targetDockable.body.Bounds.Height * 0.25)
									currentTrigger = (targetDockable.body, Region.Top);
								else if (allow.Bottom && posInBody.Y > targetDockable.body.Bounds.Height * 0.75)
									currentTrigger = (targetDockable.body, Region.Bottom);
								else if (allow.Center)
									currentTrigger = (targetDockable.body, Region.Center);
							}
							else
							{
								currentTrigger = (null, Region.None);
								break;
							}
						}

						if (currentTrigger.area is not Region.None)
						{
							var adornerTarget = currentTrigger.control is TabItem ? currentTrigger.control : targetDockable;
							var adornerLayer = AdornerLayer.GetAdornerLayer(adornerTarget);
							var canvasParent = canvas.GetLogicalParent<AdornerLayer>();
							canvasParent?.Children.Remove(canvas);
							adornerLayer.Children.Add(canvas);
							AdornerLayer.SetAdornedElement(canvas, adornerTarget);

							var leftTop = new Point(0,0);
							var leftMid = new Point(0, adornerTarget.Bounds.Height / 2);
							var rightMid = leftMid.WithX(adornerTarget.Bounds.Width);
							var topMid = new Point(adornerTarget.Bounds.Width / 2, 0);
							var bottomMid = topMid.WithY(adornerTarget.Bounds.Height);
							var bottomRight = new Point(adornerTarget.Bounds.Width, adornerTarget.Bounds.Height);
						
							var rcChild = currentTrigger.area switch
							{
								Region.Left => new Rect(leftTop, bottomMid),
								Region.Right => new Rect(topMid, bottomRight),
								Region.Top => new Rect(leftTop, rightMid),
								Region.Bottom => new Rect(leftMid, bottomRight),
								Region.Center => new Rect(leftTop, bottomRight),
							};
							adornedElement.Width = rcChild.Width;
							adornedElement.Height = rcChild.Height;
							Canvas.SetTop(adornedElement, rcChild.Top);
							Canvas.SetLeft(adornedElement, rcChild.Left);
						}
						break;
					}
				}
			}
			var cond = currentTrigger.control is null;
			canvas.IsVisible = !cond;
			draggedItem.IsVisible = cond;
			lastTrigger = currentTrigger;
		}
		
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
			body = e.NameScope.Find<ContentPresenter>("PART_SelectedContentHost");
			scroller = e.NameScope.Find<ScrollViewer>("scroller");
			header = (ItemsPresenter)scroller.Content;
            scroller.PointerWheelChanged += Scroller_PointerWheelChanged;
		}
		private void Scroller_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (e.Delta.Y > 0)
               scroller.LineLeft();
            else if (e.Delta.Y < 0)
               scroller.LineRight();
        }
		private static void DeleteOldDockableIfEmpty()
		{
			if (sourceDockable.TabItems.Items.Count is 0 )
			{
				var parent = sourceDockable.GetLogicalParent();
				if (parent is Grid { Name: "dockable" } g)
				{
					var i = g.Children.IndexOf(sourceDockable);
					var replacement = g.Children[i is 0 ? 2 : 0];
					Helpers.CopyGridProperties(g, replacement);
					g.Children.Clear();
					if (g.GetLogicalParent<Grid>() is { Name: "dockable" } g2)
					{
						var ind = g2.Children.IndexOf(g);
						g2.Children[ind] = replacement;
					}
					else
					{
						g.ReplaceWith(replacement);
					}
				}
				else if (parent is Window win)
					win.Close();
				else
					sourceDockable.ReplaceWith(null);
			}
		}
		private static DockableControl PrepareNewDockableControl()
		{
			sourceDockable.TabItems.Items.Remove(selectedItem);
			DeleteOldDockableIfEmpty();
			var tab = new DockableControl();
			tab.TabItems.Items.Add(selectedItem);
			return tab;
		}
		private static void DropTab(PointerReleasedEventArgs e)
		{
			if (lastTrigger.control is null)
			{
				draggedItem.Hide();
				var dropWin = new Window();
				dropWin.Show();
				dropWin.Position = draggedItem.Position;
				dropWin.Width = draggedItem.Width;
				dropWin.Height = draggedItem.Height;
				var tab = PrepareNewDockableControl();
				dropWin.Content = tab;
			}
			else if(lastTrigger.area is not Region.None)
			{
				var targetDockable = lastTrigger.control.FindAncestorOfType<DockableControl>();
				if (lastTrigger.area is Region.Center)
				{
					if (targetDockable == sourceDockable)
					{   //Trying to swap tabs within same DockableControl
						var swapTo = targetDockable.IndexFromContainer(lastTrigger.control);
						if (swapTo == targetDockable.SelectedIndex || lastTrigger.control is not TabItem)
							return; //Trying to swap with itself, do nothing 
						targetDockable.TabItems.Items.Move(targetDockable.SelectedIndex, swapTo);
					}
					else
					{
						sourceDockable.TabItems.Items.Remove(selectedItem);
						var insert = targetDockable.IndexFromContainer(lastTrigger.control);
						targetDockable.TabItems.Items.Insert(insert,selectedItem);
						DeleteOldDockableIfEmpty();
					}
					targetDockable.SelectedItem = selectedItem;
					var count = targetDockable.Items.Count;
					foreach (var item in targetDockable.header.Panel.Children)
						item.ZIndex = count--;
				}
				else
				{
					var tab = PrepareNewDockableControl();
					SplitTabControl(targetDockable,tab, lastTrigger.area);
				}
			}
		}
    }
}