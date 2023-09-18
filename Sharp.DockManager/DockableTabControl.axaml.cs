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
using System.ComponentModel;
using Avalonia.Threading;
using System.Diagnostics;
using System.Globalization;
using Sharp.Studio.Views;
using System.Net.Security;

namespace Sharp.DockManager
{
	enum Region
	{
		None,
		Invalid,
		Left,
		Right,
		Top,
		Bottom,
		Center
	}
	public partial class DockableTabControl : TabControl, IStyleable, IDockable
	{
		private static double autoScrollZone = 33;
		private static readonly DispatcherTimer timer = new DispatcherTimer();
		private const int animDuration = 200;
		private static readonly Animation swapAnimation = new Animation
		{
			Easing = new CubicEaseOut(),
			Duration = TimeSpan.FromMilliseconds(animDuration),
			PlaybackDirection = PlaybackDirection.Normal,
			FillMode = FillMode.None,

			Children =
			{
				new KeyFrame
				{
					KeyTime = TimeSpan.FromMilliseconds(0),
					Setters = {
						new Setter(Helpers.TabItemXProperty,0)
					}
				},
				new KeyFrame
				{

					KeyTime = TimeSpan.FromMilliseconds(animDuration),
					Setters = {
						new Setter(Helpers.TabItemXProperty,0)
					}
				}
			}
		};
		private static Window draggedItem = null;
		private static CancellationTokenSource cts = null;
		private static (Control control, Region area) lastTrigger = default;
		private static DockableTabControl sourceDockable = null;
		private static PixelPoint screenMousePosOffset;
		private static Point mousePosOffset;
		private static PointerEventArgs pointer;

		private static LinkedList<DockableItem> virtualOrderOfChildren = new();
		private static LinkedListNode<DockableItem> selectedItem;
		private static Dictionary<Window, int> sortedWindows = new();
		internal static Border adornedElement = new Border();
		internal static Canvas canvas = new Canvas();
		internal ScrollViewer scroller;
		private ItemsPresenter header;
		private ContentPresenter body;
		Type IStyleable.StyleKey => typeof(DockableTabControl);
		public static Action<Control, Control> ReplaceControlRequested
		{
			get;
			set;
		}
		public static Action<DockableTabControl, DockableTabControl> ConfigureNewDockable
		{
			get;
			set;
		}
		public DockableTabViewModel TabItems { get; set; } = new();
		public Dock Dock { get; set; }
		public Control SelectedHeader { get => header; }
		public Control SelectedBody { get => body; }
		public static bool AlignAttachmentToEdge
		{
			get; set;
		}
		public static bool SwapControlOrderInHeader
		{
			get; set;
		}
		public static readonly StyledProperty<Control> OptionalHeaderAttachmentProperty =
					AvaloniaProperty.Register<DockableTabControl, Control>(nameof(OptionalHeaderAttachment));
		public Control OptionalHeaderAttachment
		{
			get
			{
				return GetValue(OptionalHeaderAttachmentProperty);
			}
			set
			{
				SetValue(OptionalHeaderAttachmentProperty, value);
			}
		}
		public static bool HideContentWhenDrag
		{
			get;
			set;
		}
		/// <summary>
		/// Gets the z-order for one or more windows atomically with respect to each other. In Windows, smaller z-order is higher. If the window is not top level, the z order is returned as -1. 
		/// </summary>
		private static void UpdateZOrder()
		{
			var index = 0;
			Helpers.EnumWindows((wnd, param) =>
			{
				var lifetime = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime);
				foreach (var wind in lifetime.Windows)
					if (wind.TryGetPlatformHandle().Handle == wnd)
						CollectionsMarshal.GetValueRefOrAddDefault(sortedWindows, wind, out _) = index;
				index++;
				return true;
			}, IntPtr.Zero);
		}
		static DockableTabControl()
		{
			timer.Stop();
			timer.Interval = TimeSpan.FromSeconds(1 / 60);
			timer.Tick+=ScrollHeader;

			Helpers.TabItemXProperty.Changed.AddClassHandler<TabItem>((s, e)=>{
				if(s.IsAnimating(Helpers.TabItemXProperty))
					s.Arrange(s.Bounds.WithX(e.GetNewValue<double>()));
			});
			InputElement.PointerPressedEvent.AddClassHandler<Interactive>((s, e) =>
			{
			//use hittesting and if tab item is found capture its panel
				PointerPressedOnTabItem(e);
				e.Handled = true;
			});
			InputElement.PointerMovedEvent.AddClassHandler<Interactive>((s, e) =>
			{
				Debug.WriteLine("capture: "+e.Pointer.Captured);
				if (selectedItem is null)
					return;
				pointer = e;
				PointerMoved(e);
				if(draggedItem is not null)
					DoDrag(e);
				e.Handled = true;
			});
			InputElement.PointerReleasedEvent.AddClassHandler<Interactive>((s, e) =>
			{
				Debug.WriteLine("release capture");
				//e.Pointer.Capture(null);
				if (selectedItem is null)
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
		public DockableTabControl()
		{
			InitializeComponent();
			ConfigureNewDockable?.Invoke(this, sourceDockable);
			ItemsSource = TabItems.Items;
			//DataContext = _tabItems.Items;
			Background = new SolidColorBrush(new Color(255,56,56,56));
		}

		private static void ScrollHeader(object sender, EventArgs e)
		{
			var scroll = sourceDockable.scroller;
			var panel = sourceDockable.header.Panel;
			var scrollerMousePos = pointer.GetPosition(scroll);
			if (scrollerMousePos.X < autoScrollZone)
			{
				double offsetChange = (autoScrollZone - scrollerMousePos.X)/100;
				scroll.Offset = scroll.Offset.WithX(scroll.Offset.X - offsetChange);
			}
			else if (scrollerMousePos.X > scroll.Bounds.Width - autoScrollZone)
			{
				double offsetChange = (scrollerMousePos.X - (scroll.Bounds.Width - autoScrollZone))/100;
				scroll.Offset = scroll.Offset.WithX(scroll.Offset.X + offsetChange);
			}
			var tabItem = sourceDockable.ContainerFromIndex(sourceDockable.SelectedIndex);
			scrollerMousePos = pointer.GetPosition(panel);
			var expectedTabLoc = scrollerMousePos - mousePosOffset;
			tabItem.Arrange(tabItem.Bounds.WithX(expectedTabLoc.X));
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
			sourceDockable = s.FindAncestorOfType<DockableTabControl>();
			sourceDockable.SelectedItem = s.Content;
			var pos = e.GetPosition(sourceDockable.scroller);
			mousePosOffset = pos-s.TranslatePoint(default, sourceDockable.scroller).GetValueOrDefault();
			screenMousePosOffset = s.PointToScreen(e.GetPosition(s)) - s.GetVisualParent().PointToScreen(s.Bounds.Position);
			
			foreach (var item in sourceDockable.TabItems.Items)
			{
				var node = virtualOrderOfChildren.AddLast(item);
				if(node.Value == s.Content)
					selectedItem=node;
			}
		}
		
		private static void DropFinished(PointerReleasedEventArgs e)
		{
			if (virtualOrderOfChildren.Count > 0)
			{
				var newIndex = 0;
				foreach (var item in virtualOrderOfChildren)
				{
					if (item == selectedItem.Value)
						break;
					newIndex++;
				}
				if (sourceDockable.SelectedIndex != newIndex)
				{
					sourceDockable.TabItems.Items.Move(sourceDockable.SelectedIndex, newIndex);
					sourceDockable.SelectedIndex = newIndex;
				}
				else
					sourceDockable.header.Panel.InvalidateArrange();
				virtualOrderOfChildren.Clear();
				
			}
			var count = sourceDockable.Items.Count;
			foreach (var item in sourceDockable.header.Panel.Children)
				item.ZIndex = count--;
			sourceDockable = null;
			canvas.IsVisible = false;
			draggedItem = null;
			selectedItem = null;
			lastTrigger = default;
			mousePosOffset = default;
			screenMousePosOffset = default;
			timer.Stop();
		}
		private static async void PointerMoved(PointerEventArgs e)
		{
			var lifetime = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime);
			if (selectedItem is not null && draggedItem is null && e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
			{
				if (sourceDockable.scroller is null)
					return;
					
				var scroll = sourceDockable.scroller;
				var panel = sourceDockable.header.Panel;
				var scrollerMousePos = pointer.GetPosition(scroll);
				
				if (scroll.Bounds.Contains(scrollerMousePos))
				{
					if (sourceDockable.TabItems.Items.Count is 1)
						return;

					if ((scrollerMousePos.X < autoScrollZone) || (scrollerMousePos.X > scroll.Bounds.Width - autoScrollZone))
					{
						if(!timer.IsEnabled)
							timer.Start();
					}
					else if (timer.IsEnabled)
						timer.Stop();
					if (cts is null)
						cts = new();
					var tabItem = sourceDockable.ContainerFromIndex(sourceDockable.SelectedIndex);
					var prevTabItem = sourceDockable.ContainerFromItem(selectedItem.Previous?.Value);
					var nextTabItem = sourceDockable.ContainerFromItem(selectedItem.Next?.Value);
					var sMousePos = pointer.GetPosition(panel);
					var expectedTabLoc = sMousePos - mousePosOffset;
					tabItem.Arrange(tabItem.Bounds.WithX(expectedTabLoc.X));
					TabItem tabToBeAnimated = null;
					
					if (nextTabItem is TabItem next && !next.IsAnimating(Helpers.TabItemXProperty) && tabItem.Bounds.Center.X > next.Bounds.X)
					{
						tabToBeAnimated = next;
						selectedItem.SwapWith(selectedItem.Next);
					}
                    else if (prevTabItem is TabItem prev && !prev.IsAnimating(Helpers.TabItemXProperty) && tabItem.Bounds.Center.X < prev.Bounds.X+prev.Bounds.Width)
					{
						tabToBeAnimated = prev;
						selectedItem.Previous.SwapWith(selectedItem);
					}
                    if (tabToBeAnimated is not null)
					{
						double destination = 0;
						foreach (var child in virtualOrderOfChildren)
						{
							if (child == tabToBeAnimated.Content)
								break;
							destination += sourceDockable.ContainerFromItem(child).Bounds.Width;
						}
						(swapAnimation.Children[0].Setters[0] as Setter).Value = tabToBeAnimated.Bounds.X;
						(swapAnimation.Children[1].Setters[0] as Setter).Value = destination;
						
						await swapAnimation.RunAsync(tabToBeAnimated, cts.Token);

					}
					return;
				}
				timer.Stop();
				virtualOrderOfChildren.Clear();
				//make sure nothing holds reference to old virtualOrderOfChildren nodes
				selectedItem = new LinkedListNode<DockableItem>(selectedItem.Value);
				UpdateZOrder();
				
				var parent = sourceDockable.GetLogicalParent();
				var attachedToWin = sourceDockable.VisualRoot as Window;
				var Width = sourceDockable.Bounds.Width;
				var Height = sourceDockable.Bounds.Height;

				if (sourceDockable is { TabItems.Items.Count: 1 })
				{
					if (parent is Grid { Name: "dockable" } g)
					{
						sourceDockable.TabItems.Items.Remove(selectedItem.Value);
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
					else if (parent is Window)
					{
						draggedItem = attachedToWin;
					}
					else
						sourceDockable.ReplaceWith(null);
				}
				else if (sourceDockable is { TabItems.Items.Count: > 1 })
				{
					sourceDockable.TabItems.Items.Remove(selectedItem.Value);
					//need to cancel all swap animations in flight to ensure any TabItem
					//never gets stuck in shifted state due to animation when leaving ScrollViewer
					cts?.Cancel();
				}
				else
				{
					cts = null;
					return;
				}
				cts = null;
				if (draggedItem is null)
				{
					draggedItem = new Window();
					draggedItem.Show(lifetime.MainWindow);
					var tab = new DockableTabControl() { Dock = Dock.Left };
					tab.TabItems.Items.Add(selectedItem.Value);
					tab.SelectedIndex = 0;
					sourceDockable = tab;
					draggedItem.Content = tab;
					draggedItem.Width = Width;
					draggedItem.Height = Height;
				}
				draggedItem.Topmost = true;
				//draggedItem.SystemDecorations = SystemDecorations.BorderOnly;
				draggedItem.ExtendClientAreaToDecorationsHint = true;
				draggedItem.ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome;
				draggedItem.ShowInTaskbar = false;
				draggedItem.Opacity = 0.5;
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
				if (window == draggedItem)
					continue;
				var pos = window.PointToClient(screenPos);
				if (pos is { X: 0, Y: 0 })
					continue;

				hit = window.GetVisualAt(pos, c => c is not Border /*AdornerLayer.GetAdornedElement(c as Visual) is null*/) as Control;
				if (hit is not null)
				{
					var targetDockable = hit.FindAncestorOfType<DockableTabControl>(true);
					
					if (targetDockable is not null)
					{
						var dPos = e.GetPosition(targetDockable);
						if (targetDockable.scroller.Bounds.Contains(dPos))
						{
							Control tab = null;
							var scrollerPos = e.GetPosition(targetDockable.scroller);
							foreach (var t in targetDockable.header.Panel.Children)
							{
								if (t.Bounds.Contains(scrollerPos))
								{
									tab = t;
									break;
								}
							}
							if (tab is not null)
							{
								var i = targetDockable.IndexFromContainer(tab);
								draggedItem.Close();
								sourceDockable.TabItems.Items.Remove(selectedItem.Value);
								draggedItem = null;
								targetDockable.TabItems.Items.Insert(i, selectedItem.Value);
								targetDockable.SelectedItem = selectedItem.Value;
								sourceDockable = targetDockable;

								foreach (var item in sourceDockable.TabItems.Items)
								{
									var node = virtualOrderOfChildren.AddLast(item);
									if (node.Value == selectedItem.Value)
										selectedItem = node;
								}
								Debug.WriteLine("take capture");
								e.Pointer.Capture(sourceDockable);
								break;
							}
							else
							{
								currentTrigger = (null, Region.None);
							}
						}
						else
						{
							var posInBody = e.GetPosition(targetDockable.body);
							//Debug.WriteLine("area: " + posInBody);
							if (posInBody is { X: >= 0, Y: >= 0 })
							{
								if (posInBody.X < targetDockable.body.Bounds.Width * 0.25)
									currentTrigger = (targetDockable.body, Region.Left);
								else if (posInBody.X > targetDockable.body.Bounds.Width * 0.75)
									currentTrigger = (targetDockable.body, Region.Right);
								else if (posInBody.Y < targetDockable.body.Bounds.Height * 0.25)
									currentTrigger = (targetDockable.body, Region.Top);
								else if (posInBody.Y > targetDockable.body.Bounds.Height * 0.75)
									currentTrigger = (targetDockable.body, Region.Bottom);
								else
									currentTrigger = (targetDockable.body, Region.Center);
							}
							else
								break;
							

						}

						

						if (currentTrigger.area is not Region.None or Region.Invalid)
						{
							var adornerLayer = AdornerLayer.GetAdornerLayer(targetDockable);
							var canvasParent = canvas.GetLogicalParent<AdornerLayer>();
							canvasParent?.Children.Remove(canvas);
							adornerLayer.Children.Add(canvas);
							AdornerLayer.SetAdornedElement(canvas, targetDockable);
							canvas.IsVisible = true;

							var leftTop = new Point(0,0);
							var leftMid = new Point(0, targetDockable.Bounds.Height / 2);
							var rightMid = leftMid.WithX(targetDockable.Bounds.Width);
							var topMid = new Point(targetDockable.Bounds.Width / 2, 0);
							var bottomMid = topMid.WithY(targetDockable.Bounds.Height);
							var bottomRight = new Point(targetDockable.Bounds.Width, targetDockable.Bounds.Height);
						
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
							//Debug.WriteLine("target: " + rcChild);
						}
						break;
					}
				}
			}
			if (currentTrigger.control is null)
				canvas.IsVisible = false;
			lastTrigger = currentTrigger;
		}

		
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
			body = e.NameScope.Find<ContentPresenter>("PART_SelectedContentHost");
			if (this.VisualRoot == draggedItem)
				body.IsVisible = !HideContentWhenDrag;
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
		private static void DropTab(PointerReleasedEventArgs e)
		{
			if (draggedItem is null)
				return;

			if (lastTrigger.control is null)
			{
				sourceDockable.SelectedBody.IsVisible = true;
				draggedItem.ExtendClientAreaToDecorationsHint = false;
				draggedItem.Topmost = false;
				draggedItem.Opacity = 1;
				draggedItem.ShowInTaskbar = true;
			}
			else if(lastTrigger.area is not Region.Invalid or Region.None)
			{
				draggedItem.Close();

				var targetDockable = lastTrigger.control.FindAncestorOfType<DockableTabControl>();
				if (lastTrigger.control.Name is "PART_ItemsPresenter" || lastTrigger.area is Region.Center)
				{
					draggedItem.Content = null;
					sourceDockable.TabItems.Items.Remove(selectedItem.Value);
					targetDockable.TabItems.Items.Add(selectedItem.Value);
					targetDockable.SelectedItem = selectedItem.Value;
				}
				else
				{
					//if (sourceDockable.VisualChildren.Count is 1)
					{
						draggedItem.Content = null;
					}
					/*else
					{
						sourceDockable.TabItems.Items.Remove(selectedItem.Value);
						var newTab = new DockableTabControl();
						newTab.TabItems.Items.Add(selectedItem.Value);
						sourceDockable = newTab;
					}*/
					SplitTabControl(targetDockable,sourceDockable, lastTrigger.area);
				}
				sourceDockable.SelectedBody.IsVisible = true;
			}
		}
    }
}
/*
 double prevEdgePosition = 0;
					double nextEdgePosition = 0;
					var prevEdgeFound = false;

					foreach (var child in virtualOrderOfChildren)
					{
						var container = sourceDockable.ContainerFromItem(child);
						if (container == nextTabItem)
							break;
						if (!prevEdgeFound)
						{
							prevEdgePosition += container.Bounds.X;
							prevEdgeFound = container == prevTabItem;
						}
						nextEdgePosition += container.Bounds.X;
					}
					prevEdgePosition += prevTabItem is null ? 0 : prevTabItem.Bounds.Width;
 */