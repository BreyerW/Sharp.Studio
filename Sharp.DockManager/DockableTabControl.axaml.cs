using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Utils;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.Styling;
using Avalonia.VisualTree;
using HarfBuzzSharp;
using Microsoft.CodeAnalysis;
using Sharp.DockManager.Behaviours;
using Sharp.DockManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Threading;

namespace Sharp.DockManager
{

    public partial class DockableTabControl : TabControl, IStyleable, IDockable
    {
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
		private static (Control control, Dock? area) lastTrigger = default;
		private static DockableTabControl sourceDockable = null;
		private static PixelPoint screenMousePosOffset;
		private static Point mousePosOffset;
		private static LinkedList<DockableItem> virtualOrderOfChildren = new ();
		private static LinkedListNode<DockableItem> selectedItem;
		private static Dictionary<Window, int> sortedWindows = new();
		internal static Border adornedElement = new Border();
		internal static Canvas canvas = new Canvas();
		internal ScrollViewer scroller;
		private ItemsPresenter header;
		private ContentPresenter body;
		Type IStyleable.StyleKey => typeof(DockableTabControl);
        public DockableTabViewModel _tabItems { get; set; } = new();
        public Dock Dock { get; set; }
        public Control SelectedHeader { get => header; }
        public Control SelectedBody { get => body; }
		/// <summary>
		/// Gets the z-order for one or more windows atomically with respect to each other. In Windows, smaller z-order is higher. If the window is not top level, the z order is returned as -1. 
		/// </summary>
		private static void UpdateZOrder()
		{
			var index = 0;
			test.EnumWindows((wnd, param) =>
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
			Helpers.TabItemXProperty.Changed.AddClassHandler<TabItem>((s, e)=>{
				if(s.IsAnimating(Helpers.TabItemXProperty))
					s.Arrange(s.Bounds.WithX(e.GetNewValue<double>()));
			});
			InputElement.PointerPressedEvent.AddClassHandler<Interactive>((s, e) =>
			{
				PointerPressed(e);
				e.Handled = true;
			});
			InputElement.PointerMovedEvent.AddClassHandler<Interactive>((s, e) =>
			{
				PointerMoved(e);
				if(draggedItem is not null)
					DoDrag(e);
				e.Handled = true;
			});
			InputElement.PointerReleasedEvent.AddClassHandler<Interactive>((s, e) =>
			{
				DropTab(e);
				DropFinished(e);
				e.Handled = true;
			});
			adornedElement.Background = Brushes.PaleVioletRed;
			adornedElement.Opacity = 0.33;
			adornedElement.IsVisible = true;
			canvas.Children.Add(adornedElement);
		}
		public DockableTabControl()
		{
			InitializeComponent();
			ItemsSource = _tabItems.Items;
			//DataContext = _tabItems.Items;
			Dock = Dock.Left;
			Background = Brushes.Transparent;
		}
		private static DockSplitter CreateSplitter(Dock dockPos)
		{
			var gridSplitter = new DockSplitter();
			if (dockPos is Dock.Left or Dock.Right)
			{
				gridSplitter.Width = 5;
				gridSplitter.HorizontalAlignment = HorizontalAlignment.Center;
				gridSplitter.VerticalAlignment = VerticalAlignment.Stretch;
			}
			else
			{
				gridSplitter.Height = 5;
				gridSplitter.HorizontalAlignment = HorizontalAlignment.Stretch;
				gridSplitter.VerticalAlignment = VerticalAlignment.Center;
			}
			gridSplitter.Background = new SolidColorBrush(Colors.Red);
			return gridSplitter;
		}
		private static void PointerPressed(PointerPressedEventArgs e)
		{
			var s = ((Control)e.Source).FindAncestorOfType<TabItem>(true);
			if (s is null)
				return;
			sourceDockable = s.FindAncestorOfType<DockableTabControl>();	
			var pos = e.GetPosition(sourceDockable.scroller);
			mousePosOffset = pos;//-s.TranslatePoint(default, scroller).GetValueOrDefault();
			screenMousePosOffset = s.PointToScreen(e.GetPosition(s)) - s.GetVisualParent().PointToScreen(s.Bounds.Position);
			
			foreach (var item in sourceDockable._tabItems.Items)
			{
				var node = virtualOrderOfChildren.AddLast(item);
				if(node.Value == s.Content)
					selectedItem=node;
			}
			e.Pointer.Capture(sourceDockable);
		}
		protected static void DropFinished(PointerReleasedEventArgs e)
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
				sourceDockable._tabItems.Items.Move(sourceDockable.SelectedIndex, newIndex);
				sourceDockable.SelectedIndex = newIndex;
				virtualOrderOfChildren.Clear();
			}
			sourceDockable = null;
			canvas.IsVisible = false;
			draggedItem = null;
			selectedItem = null;
			lastTrigger = (null, null);
			e.Pointer.Capture(null);
		}
		protected override Size ArrangeOverride(Size finalSize)
		{
			Debug.WriteLine("arrange");
			return base.ArrangeOverride(finalSize);
		}
		//TODO: maybe go back to using global classhandlers
		private static async void PointerMoved(PointerEventArgs e)
		{
			var lifetime = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime);
			if (selectedItem is not null && draggedItem is null && e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
			{
				if (sourceDockable.scroller is null)
					return;
				Point scrollerMousePos = e.GetPosition(sourceDockable.scroller);
				Point mousePos = e.GetPosition(sourceDockable);
				var screenPos = sourceDockable.PointToScreen(mousePos);
				if (sourceDockable.scroller.Bounds.Contains(scrollerMousePos))
				{
					if (sourceDockable._tabItems.Items.Count is 1)
						return;
					var tabItem = sourceDockable.ContainerFromIndex(sourceDockable.SelectedIndex);
					var prevTabItem = sourceDockable.ContainerFromItem(selectedItem.Previous?.Value);
					var nextTabItem = sourceDockable.ContainerFromItem(selectedItem.Next?.Value);

					var mouseDelta = scrollerMousePos.X - mousePosOffset.X;
					mousePosOffset = scrollerMousePos;
					tabItem.Arrange(tabItem.Bounds.WithX(tabItem.Bounds.X+ mouseDelta));
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
						
						await swapAnimation.RunAsync(tabToBeAnimated);
					}
					return;
				}
				virtualOrderOfChildren.Clear();
				UpdateZOrder();
				
				var panel = sourceDockable.FindAncestorOfType<DockControl>();
				var attachedToWin = panel.FindAncestorOfType<Window>();
				var Width = sourceDockable.Bounds.Width;
				var Height = sourceDockable.Bounds.Height;

				if (sourceDockable._tabItems.Items.Count is 1)
				{
					if (panel.Children.Count is 1)
					{
						var mainWin = lifetime.MainWindow;

						if (attachedToWin != mainWin)
							draggedItem = attachedToWin;
					}
					else
					{
						/*var index = panel.Children.IndexOf(sourceDockable);
						DockSplitter splitter = null;
						if (index is 0 && panel.Children.Count > 2)
							splitter = panel.Children[index + 1] as DockSplitter;
						else if (index is 0)
							splitter = null;
						else if (index > 0 && index == panel.Children.Count - 1)
							splitter = panel.Children[index - 1] as DockSplitter;
						else
							splitter = panel.Children[index - 1] is DockSplitter sp && DockPanel.GetDock(sp) == sourceDockable.Dock ? sp : panel.Children[index + 1] as DockSplitter;
						*/panel.Children.Remove(sourceDockable);
						//panel.Children.Remove(splitter);
						sourceDockable._tabItems.Items.Remove(selectedItem.Value);
					}
				}
				else
					sourceDockable._tabItems.Items.Remove(selectedItem.Value);
				
				if (draggedItem is null)
				{
					draggedItem = new Window();
					draggedItem.Show();
					var docker = new DockControl();
					var tab = new DockableTabControl() { Dock = Dock.Left };
					tab._tabItems.Items.Add(selectedItem.Value);
					sourceDockable = tab;
					docker.StartWithDocks(new[] { tab });
					draggedItem.Content = docker;
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
			(Control control, Dock? area) currentTrigger = default;

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
					var d = hit.FindAncestorOfType<DockableTabControl>(true);
					if (d is not null)
					{
						var dPos = e.GetPosition(d);
						if (d.scroller.Bounds.Contains(dPos))
						{
							Control tab = null;
							var scrollerPos = e.GetPosition(d.scroller);
							foreach (var t in d.header.Panel.Children)
							{
								if (t.Bounds.Contains(scrollerPos))
								{
									tab = t;
									break;
								}
							}
							if (tab is not null)
							{
								var i = d.IndexFromContainer(tab);
								draggedItem.Close();
								sourceDockable._tabItems.Items.Remove(selectedItem.Value);
								draggedItem = null;
								d._tabItems.Items.Insert(i, selectedItem.Value);
								d.SelectedItem = selectedItem.Value;
								sourceDockable = d;
								//mousePosOffset = e.GetPosition(sourceDockable.scroller);//-s.TranslatePoint(default, scroller).GetValueOrDefault();
								//screenMousePosOffset = t.PointToScreen(e.GetPosition(t)) - t.GetVisualParent().PointToScreen(t.Bounds.Position);
								
								foreach (var item in sourceDockable._tabItems.Items)
								{
									var node = virtualOrderOfChildren.AddLast(item);
									if (node.Value == selectedItem.Value)
										selectedItem = node;
								}
								e.Pointer.Capture(sourceDockable);
								break;
							}
							else
							{
								var posInHeader = e.GetPosition(d.header);
								//loop through all header children bounds.width and compare then decide left or right
								currentTrigger = (d.scroller, Dock.Left);
							}
						}
						else
						{
							var posInBody = e.GetPosition(d.body);
							if (posInBody.X >= 0 && posInBody.Y >= 0)
							{
								if (posInBody.X < d.body.Bounds.Width * 0.25)
									currentTrigger = (d.body, Dock.Left);
								else if (posInBody.X > d.body.Bounds.Width * 0.75)
									currentTrigger = (d.body, Dock.Right);
								else if (posInBody.X > d.body.Bounds.Width * 0.25 && posInBody.X < d.body.Bounds.Width * 0.75 && posInBody.Y < d.body.Bounds.Height * 0.5)
									currentTrigger = (d.body, Dock.Top);
								else
									currentTrigger = (d.body, Dock.Bottom);
							}
							else
								break;
						}
					}
					//Debug.WriteLine("area: " + currentTrigger.area);

					var dControl = d.FindAncestorOfType<DockPanel>();
					if (dControl is null or { Children: null or { Count: 0 } })
						break;
					var adornerLayer = AdornerLayer.GetAdornerLayer(dControl);
					var canvasParent = canvas.GetLogicalParent<AdornerLayer>();
					canvasParent?.Children.Remove(canvas);
					adornerLayer.Children.Add(canvas);
					AdornerLayer.SetAdornedElement(canvas, dControl);
					var children = dControl.Children;
					var index = children.IndexOf(d);
					var dockUnderMouse = DockPanel.GetDock(d);
					var stopIndex = GetTentativeStopIndex(dockUnderMouse, currentTrigger.area.Value, index);
					var arrangeSize = dControl.Bounds;
					double accumulatedLeft = 0;
					double accumulatedTop = 0;
					double accumulatedRight = 0;
					double accumulatedBottom = 0;

					Rect rcChild = default;

					for (int i = 0; i < stopIndex; ++i)
					{
						var child = children[i];
						if (child == null)
							continue;
						CalculateArrangement(child, arrangeSize, ref accumulatedLeft, ref accumulatedRight, ref accumulatedTop, ref accumulatedBottom);
					}
					canvas.IsVisible = true;
					rcChild = CalculateArrangement(sourceDockable, arrangeSize, ref accumulatedLeft, ref accumulatedRight, ref accumulatedTop, ref accumulatedBottom, dControl.LastChildFill && stopIndex == children.Count);
					adornedElement.Width = rcChild.Width;
					adornedElement.Height = rcChild.Height;
					Canvas.SetTop(adornedElement, rcChild.Top);
					Canvas.SetLeft(adornedElement, rcChild.Left);
					break;
				}
			}
			if (currentTrigger.control is null)
				canvas.IsVisible = false;
			lastTrigger = currentTrigger;
		}

		
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
			body = this.FindDescendantOfType<ContentPresenter>();
			scroller = this.FindDescendantOfType<ScrollViewer>();
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
				draggedItem.ExtendClientAreaToDecorationsHint = false;
				draggedItem.Topmost = false;
				draggedItem.Opacity = 1;
				draggedItem.ShowInTaskbar = true;
			}
			else
			{
				draggedItem.Close();

				var sourceDockControl = sourceDockable.FindAncestorOfType<DockControl>();
				var targetDockable = lastTrigger.control.FindAncestorOfType<DockableTabControl>();
				if (lastTrigger.control.Name is "PART_ItemsPresenter")
				{
					if (sourceDockable.VisualChildren.Count is 1)
						sourceDockControl.Children.Remove(sourceDockable);
					sourceDockable._tabItems.Items.Remove(selectedItem.Value);
					targetDockable._tabItems.Items.Add(selectedItem.Value);
				}
				else
				{
					var targetDockControl = targetDockable.FindAncestorOfType<DockControl>();
					var index = targetDockControl.Children.IndexOf(targetDockable);
					var dockUnderMouse = DockPanel.GetDock(targetDockControl.Children[index] as Control);
					if (sourceDockable.VisualChildren.Count is 1)
					{
						sourceDockControl.Children.Remove(sourceDockable);
						/*if (index == targetDockControl.Children.Count-1 && lastTrigger.area is Dock.Right or Dock.Bottom)
                            targetDockControl.Children.Add(sourceDockable);
                        else*/
						PlaceDraggedItemIntoDockControl(dockUnderMouse, lastTrigger.area.Value, targetDockControl.Children, index);
					}
					else
					{
						sourceDockable._tabItems.Items.Remove(selectedItem.Value);
						var newTab = new DockableTabControl();
						newTab._tabItems.Items.Add(selectedItem.Value);
						/*if (index == targetDockControl.Children.Count-1 && lastTrigger.area is Dock.Right or Dock.Bottom)
                            targetDockControl.Children.Add(newTab);
                        else*/
						PlaceDraggedItemIntoDockControl(dockUnderMouse, lastTrigger.area.Value, targetDockControl.Children, index);
					}
				}
			}
		}
		private static Rect CalculateArrangement(Control child, in Rect arrangeSize, ref double accumulatedLeft, ref double accumulatedRight, ref double accumulatedTop, ref double accumulatedBottom, bool fillChildren = false)
		{
			Size childDesiredSize = child.DesiredSize;
			var rcChild = new Rect(
				accumulatedLeft,
				accumulatedTop,
				Math.Max(0.0, arrangeSize.Width - (accumulatedLeft + accumulatedRight)),
				Math.Max(0.0, arrangeSize.Height - (accumulatedTop + accumulatedBottom)));
			
			if (fillChildren)
				return rcChild;

			switch (DockPanel.GetDock(child))
			{
				case Dock.Left:
					accumulatedLeft += childDesiredSize.Width;
					rcChild = rcChild.WithWidth(childDesiredSize.Width);
					break;

				case Dock.Right:
					accumulatedRight += childDesiredSize.Width;
					rcChild = rcChild.WithX(Math.Max(0.0, arrangeSize.Width - accumulatedRight));
					rcChild = rcChild.WithWidth(childDesiredSize.Width);
					break;

				case Dock.Top:
					accumulatedTop += childDesiredSize.Height;
					rcChild = rcChild.WithHeight(childDesiredSize.Height);
					break;

				case Dock.Bottom:
					accumulatedBottom += childDesiredSize.Height;
					rcChild = rcChild.WithY(Math.Max(0.0, arrangeSize.Height - accumulatedBottom));
					rcChild = rcChild.WithHeight(childDesiredSize.Height);
					break;
			}
			return rcChild;
		}
		
		private static int PlaceDraggedItemIntoDockControl(Dock dockUnderMouse, Dock areaUnderMouse, Controls targetChildrens, int index)
		{
			(int finalIndex, Dock dock) = (dockUnderMouse, areaUnderMouse) switch
			{
				(Dock.Left, Dock.Right) => (index + 1, Dock.Left),
				(Dock.Right, Dock.Right) or (Dock.Left, Dock.Left) or (Dock.Top, Dock.Top) or (Dock.Bottom, Dock.Bottom) => (index, dockUnderMouse),
				(Dock.Top, Dock.Bottom) or (Dock.Bottom, Dock.Top) => (index + 1, areaUnderMouse),
				_ => (index, areaUnderMouse),
			};
			var parent = sourceDockable;
			targetChildrens.Insert(finalIndex, parent);
			((IDockable)parent).Dock = dock;
			DockPanel.SetDock(parent, dock);
			//Debug.WriteLine("docks: " + dockUnderMouse + " " + areaUnderMouse + " " + finalIndex + " " + targetChildrens.Count + " " + index);
			return finalIndex;
		}
		private static int GetTentativeStopIndex(Dock dockUnderMouse, Dock areaUnderMouse, int index)
		{
			(int finalIndex, Dock dock) = (dockUnderMouse, areaUnderMouse) switch
			{
				(Dock.Left, Dock.Right) => (index + 1, Dock.Left),
				(Dock.Right, Dock.Right) or (Dock.Left, Dock.Left) or (Dock.Top, Dock.Top) or (Dock.Bottom, Dock.Bottom) => (index, dockUnderMouse),
				(Dock.Top, Dock.Bottom) or (Dock.Bottom, Dock.Top) => (index + 1, areaUnderMouse),
				_ => (index, areaUnderMouse),
			};
			var parent = sourceDockable;
			//TODO fix this this shouldnt be possible
			/*if (parent is null)
			{
				parent = new DockableTabControl();
				parent._tabItems.Add(selectedTab);
			}*/
			((IDockable)parent).Dock = dock;
			DockPanel.SetDock(parent, dock);
			return finalIndex;
		}
		public void AddPage(Control header, Control content)
        {
            /*if (item.Header is string)
            {
                var copy = item.Header;
                var panel = new StackPanel() { Orientation = Orientation.Horizontal };
                panel.Children.Add(new Button() { Content = copy });
                item.Header = panel;
            }*/
            _tabItems.Items.Add(new DockableItem() { Header = header, Content = content});
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