using Avalonia;
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
using Sharp.DockManager.Behaviours;
using Sharp.DockManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Sharp.DockManager
{

    public partial class DockableTabControl : TabControl, IStyleable, IDockable
    {
		private static Window draggedItem = null;
		private static (Control control, Dock? area) lastTrigger = default;
		private static TabItem selectedTab = null;
		private static Point lastMousePos;
		private static PixelPoint mousePosOffset;
		private static Dictionary<Window, int> sortedWindows = new();
		internal static Border adornedElement = new Border();
		internal static Canvas canvas = new Canvas();
		internal ScrollViewer scroller;
        Type IStyleable.StyleKey => typeof(DockableTabControl);
        public ObservableCollection<TabItem> _tabItems { get; set; } = new();
        public Dock Dock { get; set; }
        public Control SelectedHeader { get => SelectedItem as Control; }
        public Control SelectedBody { get => SelectedContent as Control; }
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
			adornedElement.Background = Brushes.PaleVioletRed;
			adornedElement.Opacity = 0.33;
			adornedElement.IsVisible = true;
			canvas.Children.Add(adornedElement);
			InputElement.PointerPressedEvent.AddClassHandler<TabItem>((s, e) =>
			{
				lastMousePos = e.GetPosition(s);
				mousePosOffset = s.PointToScreen(e.GetPosition(s)) - s.GetVisualParent().PointToScreen(s.Bounds.Position);
				selectedTab = s;
			}, handledEventsToo: true);
			/*InputElement.PointerReleasedEvent.AddClassHandler<Window>((s, e) =>
			{
				OnTabDrop(null,e);
			}, handledEventsToo: true);*/
			InputElement.PointerMovedEvent.AddClassHandler<Window>((s, e) =>
			{
				var lifetime = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime);
				if (draggedItem is null && e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
				{
					var scroller = selectedTab.FindAncestorOfType<ScrollViewer>(true);
					if (scroller is null)
						return;
					Point scrollerMousePos = e.GetPosition(scroller);

					if (scroller.Bounds.Contains(scrollerMousePos))
					{
						return;
					}
					UpdateZOrder();

					Point mousePos = e.GetPosition(selectedTab);
					var screenPos = selectedTab.PointToScreen(mousePos);
					var dockable = selectedTab.FindAncestorOfType<IDockable>(true) as DockableTabControl;
					var panel = dockable.FindAncestorOfType<DockControl>(true);
					var attachedToWin = panel.FindAncestorOfType<Window>();
					var Width = selectedTab.DesiredSize.Width;
					var Height = selectedTab.DesiredSize.Height;

					
					if (dockable._tabItems.Count is 1)
					{
						if (panel.Children.Count is 1)
						{
							var mainWin = lifetime.MainWindow;

							if (attachedToWin != mainWin)
								draggedItem = attachedToWin;
						}
						else
						{
							var index = panel.Children.IndexOf(dockable);
							DockSplitter splitter = null;
							if (index is 0 && panel.Children.Count > 2)
								splitter = panel.Children[index + 1] as DockSplitter;
							else if (index is 0)
								splitter = null;
							else if (index > 0 && index == panel.Children.Count - 1)
								splitter = panel.Children[index - 1] as DockSplitter;
							else
								splitter = panel.Children[index - 1] is DockSplitter sp && DockPanel.GetDock(sp) == dockable.Dock ? sp : panel.Children[index + 1] as DockSplitter;
							panel.Children.Remove(dockable);
							panel.Children.Remove(splitter);
							
						}
					}
					dockable._tabItems.Remove(selectedTab);
					if (draggedItem is null)
					{
						draggedItem = new Window();
						var docker = new DockControl();
						var tab = new DockableTabControl() { Dock = Dock.Left };
						tab.AddPage(selectedTab);
						docker.StartWithDocks(new[] { tab });
						draggedItem.Content = docker;
						draggedItem.ShowInTaskbar = true;
						//draggedItem.Width = Width;
						//draggedItem.Height = Height;
						draggedItem.Show();
					}
					draggedItem.SizeToContent = SizeToContent.WidthAndHeight;
					draggedItem.SystemDecorations = SystemDecorations.BorderOnly;
					draggedItem.ExtendClientAreaToDecorationsHint = true;
					draggedItem.PositionChanged += DraggedItem_PositionChanged;
				}
				if (draggedItem is not null)
				{
					Point mousePos = e.GetPosition(draggedItem);
					var screenPos = draggedItem.PointToScreen(mousePos);
					draggedItem.Position = screenPos - mousePosOffset;
				}
			}, handledEventsToo: true);
		}

		public DockableTabControl()
        {
            InitializeComponent();
            ItemsSource = _tabItems;
            Dock = Dock.Left;
            DraggableTabsBehaviours.SetIsSet(this,true);
			DraggableTabsBehaviours.SetOnDrop(this, OnTabDrop);
		}
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            scroller = this.FindDescendantOfType<ScrollViewer>();
            scroller.PointerWheelChanged += Scroller_PointerWheelChanged;
			DraggableTabsBehaviours.SetDragBounds(this, scroller.Bounds);
		}
		private void Scroller_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (e.Delta.Y > 0)
               scroller.LineLeft();
            else if (e.Delta.Y < 0)
                scroller.LineRight();
        }
		private static void OnTabDrop(object sender, PointerReleasedEventArgs e)
		{
			if (draggedItem is null)
				return;

			draggedItem.PositionChanged -= DraggedItem_PositionChanged;
			if (lastTrigger.control is null)
			{
				draggedItem.SystemDecorations = SystemDecorations.Full;
				draggedItem.ExtendClientAreaToDecorationsHint = false;
			}
			else
			{
				draggedItem.Close();

				var sourceDockable = selectedTab.FindAncestorOfType<DockableTabControl>();
				//if (sourceDockable.IsVisualAncestorOf(lastTrigger.control)) break;//continue;
				var sourceDockControl = sourceDockable.FindAncestorOfType<DockControl>();
				var targetDockable = lastTrigger.control.FindAncestorOfType<DockableTabControl>();
				//var childrens = new Controls(targetDockControl.Children); 
				if (lastTrigger.control.Name is "PART_ItemsPresenter")
				{
					if (sourceDockable._tabItems.Count is 1)
						sourceDockControl.Children.Remove(sourceDockable);
					sourceDockable._tabItems.Remove(selectedTab);
					targetDockable.AddPage(selectedTab);
				}
				else
				{
					var targetDockControl = targetDockable.FindAncestorOfType<DockControl>();
					var index = targetDockControl.Children.IndexOf(targetDockable);
					var dockUnderMouse = DockPanel.GetDock(targetDockControl.Children[index] as Control);
					if (sourceDockable._tabItems.Count is 1)
					{
						sourceDockControl.Children.Remove(sourceDockable);
						/*if (index == targetDockControl.Children.Count-1 && lastTrigger.area is Dock.Right or Dock.Bottom)
                            targetDockControl.Children.Add(sourceDockable);
                        else*/
						PlaceDraggedItemIntoDockControl(dockUnderMouse, lastTrigger.area.Value, targetDockControl.Children, index);
					}
					else
					{
						sourceDockable._tabItems.Remove(selectedTab);
						var newTab = new DockableTabControl();
						newTab._tabItems.Add(selectedTab);
						/*if (index == targetDockControl.Children.Count-1 && lastTrigger.area is Dock.Right or Dock.Bottom)
                            targetDockControl.Children.Add(newTab);
                        else*/
						PlaceDraggedItemIntoDockControl(dockUnderMouse, lastTrigger.area.Value, targetDockControl.Children, index);
					}

				}
			}
			canvas.IsVisible = false;
			draggedItem = null;
			selectedTab.IsSelected = true;
			selectedTab = null;
			lastTrigger = (null, null);
		}
		private static void DraggedItem_PositionChanged(object? sender, PixelPointEventArgs e)
		{
			var screenPos = e.Point + mousePosOffset;
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
						if (d.SelectedHeader.IsVisualAncestorOf(hit))
						{
							var posInHeader = d.SelectedHeader.PointToClient(e.Point);
							//loop through all header children bounds.width and compare then decide left or right
							currentTrigger = (d.SelectedHeader, Dock.Left);
						}
						else
						{
							var maybePosInBody = window.TranslatePoint(pos, d.SelectedBody);
							if (maybePosInBody.HasValue)
							{
								var posInBody = maybePosInBody.Value;
								if (posInBody.X < d.SelectedBody.Bounds.Width * 0.25)
									currentTrigger = (d.SelectedBody, Dock.Left);
								else if (posInBody.X > d.SelectedBody.Bounds.Width * 0.75)
									currentTrigger = (d.SelectedBody, Dock.Right);
								else if (posInBody.X > d.SelectedBody.Bounds.Width * 0.25 && posInBody.X < d.SelectedBody.Bounds.Width * 0.75 && posInBody.Y < d.SelectedBody.Bounds.Height * 0.5)
									currentTrigger = (d.SelectedBody, Dock.Top);
								else
									currentTrigger = (d.SelectedBody, Dock.Bottom);
							}
						}

					}
					Debug.WriteLine("area: " + currentTrigger.area);

					var dControl = d.FindAncestorOfType<DockPanel>();
					if (dControl is null or { Children: null or { Count: 0 } })
						break;
					var adornerLayer = AdornerLayer.GetAdornerLayer(dControl);
					var canvasParent = canvas.GetLogicalParent<AdornerLayer>();
					if (canvasParent is not null)
						canvasParent.Children.Remove(canvas);
					adornerLayer.Children.Add(canvas);
					AdornerLayer.SetAdornedElement(canvas, dControl);
					var children = dControl.Children;
					var index = children.IndexOf(d);
					var dockUnderMouse = DockPanel.GetDock(children[index] as Control);
					var stopIndex = GetTentativeStopIndex(dockUnderMouse, currentTrigger.area.Value, children, index);
					var arrangeSize = dControl.Bounds;
					//int totalChildrenCount = children.Count;
					//int nonFillChildrenCount = totalChildrenCount - (dControl.LastChildFill ? 1 : 0);

					double accumulatedLeft = 0;
					double accumulatedTop = 0;
					double accumulatedRight = 0;
					double accumulatedBottom = 0;

					Rect rcChild=default;
					
					for (int i = 0; i < stopIndex; ++i)
					{
						var child = children[i];
						if (child == null)
							continue;

						CalculateArrangement(child,arrangeSize, ref accumulatedLeft, ref accumulatedRight, ref accumulatedTop, ref accumulatedBottom);
						
					}
					rcChild=CalculateArrangement(selectedTab.FindAncestorOfType<DockableTabControl>(), arrangeSize, ref accumulatedLeft, ref accumulatedRight, ref accumulatedTop, ref accumulatedBottom);
					canvas.IsVisible = true;
					if (dControl.LastChildFill && stopIndex == children.Count)
					{
						adornedElement.Width = double.NaN;
						adornedElement.Height = double.NaN;
					}
					else
					{
						adornedElement.Width = rcChild.Width;
						adornedElement.Height = rcChild.Height;
					}
					

					Canvas.SetTop(adornedElement, rcChild.Top);
					Canvas.SetLeft(adornedElement, rcChild.Left);
					break;
				}
			}
			if (currentTrigger.control is null)
				canvas.IsVisible = false;
			lastTrigger = currentTrigger;
		}
		private static Rect CalculateArrangement(Control child, in Rect arrangeSize, ref double accumulatedLeft, ref double accumulatedRight, ref double accumulatedTop, ref double accumulatedBottom)
		{
			Size childDesiredSize = child.DesiredSize;
			var rcChild = new Rect(
				accumulatedLeft,
				accumulatedTop,
				Math.Max(0.0, arrangeSize.Width - (accumulatedLeft + accumulatedRight)),
				Math.Max(0.0, arrangeSize.Height - (accumulatedTop + accumulatedBottom)));


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
			var parent = selectedTab.FindAncestorOfType<DockableTabControl>();

			/*if (parent is null)
			{
				parent = new DockableTabControl();
				parent._tabItems.Add(selectedTab);
			}*/
			targetChildrens.Insert(finalIndex, parent);
			((IDockable)parent).Dock = dock;
			DockPanel.SetDock(parent, dock);
			Debug.WriteLine("docks: " + dockUnderMouse + " " + areaUnderMouse + " " + finalIndex + " " + targetChildrens.Count + " " + index);
			return finalIndex;
		}
		private static int GetTentativeStopIndex(Dock dockUnderMouse, Dock areaUnderMouse, Controls targetChildrens, int index)
		{
			(int finalIndex, Dock dock) = (dockUnderMouse, areaUnderMouse) switch
			{
				(Dock.Left, Dock.Right) => (index + 1, Dock.Left),
				(Dock.Right, Dock.Right) or (Dock.Left, Dock.Left) or (Dock.Top, Dock.Top) or (Dock.Bottom, Dock.Bottom) => (index, dockUnderMouse),
				(Dock.Top, Dock.Bottom) or (Dock.Bottom, Dock.Top) => (index + 1, areaUnderMouse),
				_ => (index, areaUnderMouse),
			};
			var parent = selectedTab.FindAncestorOfType<DockableTabControl>();
			//TODO fix this this shouldnt be possible
			if (parent is null)
			{
				parent = new DockableTabControl();
				parent._tabItems.Add(selectedTab);
			}
			((IDockable)parent).Dock = dock;
			DockPanel.SetDock(parent, dock);
			return finalIndex;

			/**/
			//targetChildrens.Insert(finalIndex, parent);
			//((IDockable)parent).Dock = dock;
			//DockPanel.SetDock(parent, dock);
			//Debug.WriteLine("docks: " + dockUnderMouse + " " + areaUnderMouse + " " + finalIndex + " " + targetChildrens.Count + " " + index);
			//return finalIndex;
		}
		public void AddPage(TabItem item)
        {
            /*if (item.Header is string)
            {
                var copy = item.Header;
                var panel = new StackPanel() { Orientation = Orientation.Horizontal };
                panel.Children.Add(new Button() { Content = copy });
                item.Header = panel;
            }*/
            _tabItems.Add(item);
        }
    }
}
