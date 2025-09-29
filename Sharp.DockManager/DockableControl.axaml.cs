using Avalonia;
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
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using HarfBuzzSharp;
using System.Collections.Generic;

namespace Sharp.DockManager
{
	public enum Region
	{
		None,
		Left,
		Right,
		Top,
		Bottom,
		Center,
		Header
	}
    public abstract partial  class DockableControl : TabControl
	{
		private static Dictionary<Window,List<DockableControl>> windowDockableMapping = new();
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

		protected static List<DockableItem> selectedItems = new ();
		private static Window[] sortedWindows;
		protected static Border adornedElement = new ();
		internal static Canvas canvas = new ();
		protected ScrollViewer scroller;
		private ItemsPresenter header;
		private ContentPresenter body;

        public static readonly StyledProperty<IBrush> PreviewBrushProperty =
                    AvaloniaProperty.Register<DockableControl, IBrush>(nameof(PreviewBrush));

        public IBrush PreviewBrush
        {
            set
            {
                draggedItem.Background = value;
                adornedElement.Background = value;
                SetValue(PreviewBrushProperty, value);
            }
            get
            {
                return GetValue(PreviewBrushProperty);
            }
        }

		public static Action<Control, Control> ReplaceControlRequested
		{
			get;
			set;
		}
		
        public DockableTabViewModel TabItems { get; set; } = new();


		private static void UpdateZOrder()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				var windows = ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime).Windows.ToArray();
				Window.SortWindowsByZOrder(windows);
				windows.AsSpan().Reverse();
				sortedWindows = windows;
			}
		}
		static DockableControl()
		{
			InputElement.PointerPressedEvent.AddClassHandler<TabItem>((s, e) =>
			{
				e.Handled = true;
				PointerPressedOnTabItem(s,e);
			});
			InputElement.PointerMovedEvent.AddClassHandler<Interactive>((s, e) =>
			{
				if (selectedItems is { Count: 0 })
					return;
				PointerMoved(e);
				if(isDragging)
					DoDrag(e);
				e.Handled = true;
			});
			InputElement.PointerReleasedEvent.AddClassHandler<Interactive>((s, e) =>
			{
				if (selectedItems is { Count: 0 } || isDragging is false)
				{
                    DropFinished();
                    return;
				}
				DropTab(e);
				DropFinished();
				e.Handled = true;
			});
			
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
                Helpers.SetAsColumn(splitter, 1);
			}
			else
			{
				Helpers.SetAsRow(splitter, 1);
			}
			return splitter;
		}
		

		private static void SplitTabControl(Control existing, Control added, Region dockForAdded)
		{
			var grid=GridFactory();
			Helpers.CopyGridProperties(existing, grid);
			existing.ReplaceWith(grid);
			
			var splitter = SplitterFactory(2, dockForAdded is Region.Left or Region.Right);
			grid.Children.Add(existing);
			grid.Children.Add(splitter);
			grid.Children.Add(added);
			if (dockForAdded is Region.Left)
			{
                Helpers.SetAsColumn(added, 0);
                Helpers.SetAsColumn(existing, 2);
			}
			else if (dockForAdded is Region.Right)
			{
                Helpers.SetAsColumn(added, 2);
                Helpers.SetAsColumn(existing, 0);
			}
			else if (dockForAdded is Region.Top)
			{
                Helpers.SetAsRow(added, 0);
                Helpers.SetAsRow(existing, 2);
			}
			else
			{
                Helpers.SetAsRow(added, 2);
                Helpers.SetAsRow(existing, 0);
			}
		}
		public DockableControl()
		{
			InitializeComponent();
            SelectionChanged += DockableControl_SelectionChanged;
            TabItems.Items.CollectionChanged += Items_CollectionChanged;
            ItemsSource = TabItems.Items;
			SelectionMode = SelectionMode.Single | SelectionMode.AlwaysSelected;
        }
		internal protected void RecalculateZIndex()
		{
            var selectedFound = false;
            var count = TabItems.Items.Count;

            var index = -count;

            foreach (var item in TabItems.Items)
            {
                var container = (TabItem)this.ContainerFromItem(item);
                if (container is null)
                    return;
                container.Classes.Remove("rightToLeft");
                container.Classes.Remove("leftToRight");
                if (item == SelectedItem)
                {
                    selectedFound = true;
                    index = count;
                    container.ZIndex = index;
                }
                else if (!selectedFound)
                {
                    index++;
                    container.ZIndex = index;
                    container.Classes.Add("rightToLeft");
                }
                else if (selectedFound)
                {
                    index--;
                    container.ZIndex = index;
                    container.Classes.Add("leftToRight");
                }
            }
        }
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
			ref var dockables = ref CollectionsMarshal.GetValueRefOrAddDefault(windowDockableMapping, (Window)VisualRoot, out var exists);
			if (!exists)
				dockables = new List<DockableControl>();
			dockables.Add(this);
        }
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            ref var dockables = ref CollectionsMarshal.GetValueRefOrAddDefault(windowDockableMapping, (Window)e.Root, out var exists);
            if (exists)
				dockables.Remove(this);
        }
        protected virtual void DockableControl_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            RecalculateZIndex();
        }
		protected override void ContainerIndexChangedOverride(Control container, int oldIndex, int newIndex)
        {
            base.ContainerIndexChangedOverride(container, oldIndex, newIndex);
			RecalculateZIndex();
        }
        protected override void ContainerForItemPreparedOverride(Control container, object? item, int index)
        {
            base.ContainerForItemPreparedOverride(container, item, index);
			RecalculateZIndex();
        }
        private void Items_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (var newItem in e.NewItems)
                {
                    if (newItem is DockableItem item)
                        item.ParentCollection = TabItems.Items;
                }
            DeleteOldDockableIfEmpty();
        }
        private static void PointerPressedOnTabItem(TabItem sender, PointerPressedEventArgs e)
		{
			sourceDockable = sender.FindAncestorOfType<DockableControl>();
			sourceDockable.PointerPressedOnTabItem(sender, e);
		}
		protected virtual void PointerPressedOnTabItem(object sender, PointerPressedEventArgs e)
		{
			var s = (TabItem)sender;
			if (!DockManager.GetAllowDrag(((DockableItem)s.Header).Header))
			{
				DropFinished();
				return;
			}
			sourceDockable.SelectedItem = s.Content;
			screenMousePosOffset = s.PointToScreen(e.GetPosition(s)) - s.GetVisualParent().PointToScreen(s.Bounds.Position);
			selectedItems.Clear();
			selectedItems.Add(s.Content as DockableItem);
		}

        private static void DropFinished()
		{
			if (sourceDockable?.TabItems.Items.Count > 0)
			{
				sourceDockable.RecalculateZIndex();
			}

			sourceDockable = null;
			canvas.IsVisible = false;
			selectedItems.Clear();
			lastTrigger = default;
			screenMousePosOffset = default;
			draggedItem.IsVisible = false;
			isDragging = false;

		}
		private static void PointerMoved(PointerEventArgs e)
		{
			if (selectedItems is { Count : not 0 } && e.Properties.IsLeftButtonPressed)
			{
				if (sourceDockable.scroller is null)
					return;
					
				var scroll = sourceDockable.header.Panel;
				var tab = sourceDockable.ContainerFromIndex(sourceDockable.SelectedIndex);
				var scrollerMousePos = e.GetPosition(scroll);
				
				if (tab.Bounds.Contains(scrollerMousePos))
					return;
				//do it only once when starting the drag
				if (!isDragging)
				{
					UpdateZOrder();
					sourceDockable.OnStartDrag(e);
                }
                isDragging = true;
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
			foreach (var window in sortedWindows)
			{
				var pos = window.PointToClient(screenPos);
				if (pos is { X: 0, Y: 0 })
					continue;

				hit = window.GetVisualAt(pos, c => c is not Border) as Control;
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
						if (allow.Header && targetDockable.scroller.Bounds.Contains(dPos))
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
							if (tab is not null && DockManager.GetAllowDrop(tab))
							{
								currentTrigger = (tab, Region.Header);
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

							sourceDockable.PreparePreviewOverlay(adornerTarget, adornedElement, currentTrigger.area);
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
		//Can use this to customize if overlay should follow margins/paddings of target
		//or change color based on some condition like setting red color if target is not allowed 
		//this is called only when preview is over another window but not when over "empty" space
		protected virtual void PreparePreviewOverlay(Control adornerTarget, Control adorner, Region area)
		{
            var leftTop = new Point(0, 0);
            var leftMid = new Point(0, adornerTarget.Bounds.Height / 2);
            var rightMid = leftMid.WithX(adornerTarget.Bounds.Width);
            var topMid = new Point(adornerTarget.Bounds.Width / 2, 0);
            var bottomMid = topMid.WithY(adornerTarget.Bounds.Height);
            var bottomRight = new Point(adornerTarget.Bounds.Width, adornerTarget.Bounds.Height);

            var rcChild = area switch
            {
                Region.Left => new Rect(leftTop, bottomMid),
                Region.Right => new Rect(topMid, bottomRight),
                Region.Top => new Rect(leftTop, rightMid),
                Region.Bottom => new Rect(leftMid, bottomRight),
                Region.Center or Region.Header => new Rect(leftTop, bottomRight),
            };
            adorner.Width = rcChild.Width;
            adorner.Height = rcChild.Height;
            Canvas.SetTop(adorner, rcChild.Top);
            Canvas.SetLeft(adorner, rcChild.Left);
        }
		protected virtual void OnStartDrag(PointerEventArgs e)
		{
			if(e.KeyModifiers == KeyModifiers.Shift)
			{
				selectedItems.Clear();
				int i = 0;
				while(i < TabItems.Items.Count)
				{
					var container = ContainerFromIndex(i) as TabItem;
					if (DockManager.GetAllowDrag(((DockableItem)container.Content).Header))
					{
						container.Classes.Add("grabbed");
						selectedItems.Add((DockableItem)container.Content);
					}
					i++;
				}
			}
			else
				ContainerFromIndex(SelectedIndex).Classes.Add("grabbed");
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
		private void DeleteOldDockableIfEmpty()
		{
            var dockable = this;
            if (dockable.TabItems.Items.Count is 0)
            {
				dockable.ReplaceWith(null);
                /*var parent = dockable.GetLogicalParent();
				if (parent is Grid { Name: "dockable" } g)
				{
					var i = g.Children.IndexOf(dockable);
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
				{
                    dockable.CloseWindowRequested(win);
				}
				else
					dockable.ReplaceWith(null);*/
            }
        }
		public virtual void CloseWindowRequested(Window win)
		{
			win.Close();
		}
		private static DockableControl PrepareNewDockableControl()
		{	
			var tab = sourceDockable.CreateDockable();
			tab.Theme = sourceDockable.Theme;
			foreach (var removable in selectedItems)
			{
				sourceDockable.TabItems.Items.Remove(removable);
				tab.TabItems.Items.Add(removable);
			}
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
				if (targetDockable == sourceDockable && sourceDockable.TabItems.Items.Count == selectedItems.Count)
					return; //Trying to swap with itself, do nothing. When we are going to swap same amount of tabs as sourceDockable, the area on which tab is dropped does not matter
                if (lastTrigger.area is Region.Center)
				{
					var selected = sourceDockable.SelectedItem;
					if (targetDockable == sourceDockable)
					{   
						return; //Trying to swap with itself, do nothing
					}
					else
					{
                        foreach (var removable in selectedItems)
                        {
                            sourceDockable.TabItems.Items.Remove(removable);
                            targetDockable.TabItems.Items.Add(removable);
                        }
					}
					targetDockable.SelectedItem = selected;
				}
				else if(lastTrigger.area is Region.Header)
				{
					var selected = sourceDockable.SelectedItem;
                    if (targetDockable == sourceDockable)
                    {   //Trying to swap tabs within same DockableControl
                        var swapTo = targetDockable.IndexFromContainer(lastTrigger.control);
                        if (swapTo == targetDockable.SelectedIndex || lastTrigger.control is not TabItem)
                            return; //Trying to swap with itself, do nothing 
                        targetDockable.TabItems.Items.Move(targetDockable.SelectedIndex, swapTo);
                    }
                    else
                    {
						var insert = targetDockable.IndexFromContainer(lastTrigger.control);
						var insertOffset = 0;
                        foreach (var removable in selectedItems)
                        {
                            sourceDockable.TabItems.Items.Remove(removable);
                            targetDockable.TabItems.Items.Insert(insert+insertOffset,removable);
							insertOffset++;
                        }
                    }
                    targetDockable.SelectedItem = selected;
                }
				else
				{
					var tab = PrepareNewDockableControl();
					SplitTabControl(targetDockable, tab, lastTrigger.area);
				}
			}
		}

		public abstract DockableControl CreateDockable();
    }
}