using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using Sharp.DockManager.ViewModels;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Sharp.DockManager
{
	[Flags]
	public enum Region
	{
		None = 0,
		Left = 1 << 0,
		Right = 1 << 1,
        Top = 1 << 2,
        Bottom = 1 << 3,
        Center = 1 << 4,
        Header = 1 << 5,
		All = Left | Right | Top | Bottom | Center | Header
    }
    public abstract partial  class DockableControl : TabControl
	{
		//TODO: add removal of closing windows
		protected static Dictionary<Window,HashSet<DockableControl>> dockableCounterInWindows = new();
		protected static bool isDragging = false;
		protected static int dragDisposed = 0;
		protected static bool shiftWasPressed = false;
		protected static PointerPressedEventArgs lefTButtonPressEvent;

		protected readonly static Window draggedItem = new Window()
		{
			ShowActivated = false,
			Topmost = true,
			SystemDecorations = SystemDecorations.None,
			ExtendClientAreaTitleBarHeightHint = 0,
			ExtendClientAreaToDecorationsHint = true,
			ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome,
			ShowInTaskbar = false,
			IsHitTestVisible = false,
		};

		protected static (Control control, Region area) lastTrigger = default;
		protected internal static DockableControl sourceDockable = null;
		protected static PixelPoint screenMousePosOffset;

		protected static List<DockableItem> selectedItemsFrorDragging = new ();
		private static Window[] sortedWindows;
		protected static Border adornedElement = new ();
		protected static Canvas canvas = new ();
		protected ScrollViewer scroller;
		protected ItemsPresenter header;
		protected ContentPresenter body;

        public static readonly StyledProperty<IBrush> PreviewBrushProperty =
                    AvaloniaProperty.Register<DockableControl, IBrush>(nameof(PreviewBrush));
        public static readonly StyledProperty<IBrush> InvalidPreviewBrushProperty =
                    AvaloniaProperty.Register<DockableControl, IBrush>(nameof(InvalidPreviewBrush));
        public IBrush PreviewBrush
        {
            set
            {
                SetValue(PreviewBrushProperty, value);
            }
            get
            {
                return GetValue(PreviewBrushProperty);
            }
        }
        public IBrush InvalidPreviewBrush
        {
            set
            {
                SetValue(InvalidPreviewBrushProperty, value);
            }
            get
            {
                return GetValue(InvalidPreviewBrushProperty);
            }
        }
        public DockableTabViewModel TabItems { get; set; } = new();
		protected static void UpdateZOrder()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				var windows = ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime).Windows.ToArray();
				Window.SortWindowsByZOrder(windows);
				windows.AsSpan().Reverse();
				sortedWindows = windows;
				//prune closed windows
				foreach(var (win, _ ) in dockableCounterInWindows)
				{
					if (!sortedWindows.Contains(win))
						dockableCounterInWindows.Remove(win);
				}
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
				PointerMoved(e);
				e.Handled = true;
			});

			draggedItem.PositionChanged += DraggedItem_PositionChanged;
			adornedElement.IsVisible = true;
			canvas.Children.Add(adornedElement);
		}
		private static void DraggedItem_PositionChanged(object? sender, PixelPointEventArgs e)
		{
			if (isDragging)
				DoDrag(e);
		}

		private static void DoDrag(PixelPointEventArgs e)
		{
			var screenPos = e.Point+screenMousePosOffset;

			(Control control, Region area) currentTrigger = default;

			DockableControl targetDockable = null;
			bool isValid = true;
			foreach (var window in sortedWindows)
			{
				var pos = window.PointToClient(screenPos);
				if (window == draggedItem || !window.Bounds.Contains(pos))
					continue;

				if (dockableCounterInWindows.TryGetValue(window, out var dockables))
					foreach (var dockable in dockables)
					{
						var locPos = dockable.GetVisualParent().PointToClient(screenPos);
						if (dockable.Bounds.Contains(locPos))
						{
							targetDockable = dockable;
							break;
						}
					}
				if (targetDockable is not null)
				{

					var dropPermissions = DockManager.GetAllowDropArea(targetDockable);
					(bool Top, bool Bottom, bool Left, bool Right, bool Center, bool Header) allow =
					(
						dropPermissions.HasFlag(Region.Top),
						dropPermissions.HasFlag(Region.Bottom),
						dropPermissions.HasFlag(Region.Left),
						dropPermissions.HasFlag(Region.Right),
						dropPermissions.HasFlag(Region.Center),
						dropPermissions.HasFlag(Region.Header)
					);
					var dPos = targetDockable.PointToClient(screenPos);
					if (targetDockable.scroller.Bounds.Contains(dPos))
					{
						Control tab = null;
						var scrollerPos = targetDockable.header.Panel.PointToClient(screenPos);
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
							currentTrigger = (tab, Region.Header);
							isValid = allow.Header && DockManager.GetAllowDrop(tab);
						}
						else
						{
							currentTrigger = (null, Region.Header);
						}
					}
					else
					{
						var posInBody = targetDockable.body.PointToClient(screenPos);
						if (posInBody is { X: >= 0, Y: >= 0 })
						{
							if (posInBody.X < targetDockable.body.Bounds.Width * 0.25)
							{
								currentTrigger = (targetDockable.body, Region.Left);
								isValid = allow.Left;
							}
							else if (posInBody.X > targetDockable.body.Bounds.Width * 0.75)
							{
								currentTrigger = (targetDockable.body, Region.Right);
								isValid = allow.Right;
							}
							else if (posInBody.Y < targetDockable.body.Bounds.Height * 0.25)
							{
								currentTrigger = (targetDockable.body, Region.Top);
								isValid = allow.Top;
							}
							else if (posInBody.Y > targetDockable.body.Bounds.Height * 0.75)
							{
								currentTrigger = (targetDockable.body, Region.Bottom);
								isValid = allow.Bottom;
							}
							else
							{
								currentTrigger = (targetDockable.body, Region.Center);
								isValid = allow.Center;
							}
						}
						else
						{
							currentTrigger = (null, Region.None);
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
						targetDockable.PreparePreviewOverlay(adornerTarget, adornedElement, currentTrigger.area, isValid);
					}

				}
				break;
			}
			if (currentTrigger is { control: null, area: Region.None })
				sourceDockable.PreparePreviewOverlay(draggedItem, null, currentTrigger.area, isValid);
			lastTrigger = currentTrigger;
			if (!isValid)
				lastTrigger.area = Region.None;
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
			ref var dockables = ref CollectionsMarshal.GetValueRefOrAddDefault(dockableCounterInWindows, (Window)VisualRoot, out var exists);
			if (!exists)
				dockables = new();
			dockables.Add(this);
        }
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
			var win = (Window)e.Root;
			ref var dockables = ref CollectionsMarshal.GetValueRefOrAddDefault(dockableCounterInWindows, win, out var exists);
			if (exists)
			{
				dockables.Remove(this);
				/*if (dockables.Count == 0)
					CloseWindowRequested(win);*/
			}
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
			if (e.Properties.IsLeftButtonPressed)
			{
				var s = (TabItem)sender;
				if (!DockManager.GetAllowDrag(((DockableItem)s.Header).Header))
				{
					DropFinished();
					return;
				}
				Interlocked.Exchange(ref dragDisposed, 0);
				shiftWasPressed = e.KeyModifiers == KeyModifiers.Shift;
				sourceDockable.SelectedItem = s.Content;
				screenMousePosOffset = s.PointToScreen(e.GetPosition(s)) - s.PointToScreen(new Point(0,0));
				//selectedItemsFrorDragging.Clear();
				//selectedItemsFrorDragging.Add(s.Content as DockableItem);
				lefTButtonPressEvent = e;
				var Width = sourceDockable.Bounds.Width;
				var Height = sourceDockable.Bounds.Height;
				draggedItem.Width = Width;
				draggedItem.Height = Height;
			}
			else
				sourceDockable = null;
		}

		protected override void OnLostFocus(RoutedEventArgs e)
		{
			base.OnLostFocus(e);
			if (isDragging && Interlocked.Increment(ref dragDisposed) == 1)
			{
				DropTab(null);
				DropFinished();
			}
		}

		protected internal virtual void ReplaceControlRequested(Control toBeReplaced, Control replacement)
        {
        }


        private static void DropFinished()
		{
			sourceDockable?.OnStopDrag();
			/*foreach (var (win, counter) in dockableCounterInWindows)
			{
				if (counter.Count == 0)
				{
					dockableCounterInWindows.Remove(win);
					sourceDockable?.CloseWindowRequested(win);
				}
			}*/
			sourceDockable = null;
			canvas.IsVisible = false;
			selectedItemsFrorDragging.Clear();
			lastTrigger = default;
			screenMousePosOffset = default;
			draggedItem.IsVisible = false;
			isDragging = false;
			shiftWasPressed = false;
		}
		
		private static void PointerMoved(PointerEventArgs e)
		{
			if (sourceDockable is not null && e.Properties.IsLeftButtonPressed)
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
				draggedItem.Position = scroll.PointToScreen(scrollerMousePos) - screenMousePosOffset;
				draggedItem.BeginMoveDrag(lefTButtonPressEvent);
			}
		}
		
		/// <summary>
		/// Can use this to customize if overlay should follow margins/paddings of target
		/// or change color based on some condition like setting red color if target is not allowed.
		/// If adorner is null, that means drag is either over invalid target or performed outside any app window (over "empty" space)
		/// </summary>
		/// <param name="adornerTarget"></param>
		/// <param name="adorner"></param>
		/// <param name="area"></param>
		/// <exception cref="NotImplementedException"></exception>
		protected virtual void PreparePreviewOverlay(Control adornerTarget, Control? adorner, Region area, bool isValidDrop)
		{
			if (isValidDrop) 
			{
                draggedItem.Background = PreviewBrush;
                adornedElement.Background = PreviewBrush;
            }
			else
			{
                draggedItem.Background = InvalidPreviewBrush;
                adornedElement.Background = InvalidPreviewBrush;
            }
			var cond = adorner is not null;
			if (cond)
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
					Region.Center or Region.Header or Region.All => new Rect(leftTop, bottomRight),
					Region.None => new Rect(new Point(0, 0), new Point(0, 0)),
					_ => throw new NotImplementedException(),
				};
				adorner.Width = rcChild.Width;
				adorner.Height = rcChild.Height;
				Canvas.SetTop(adorner, rcChild.Top);
				Canvas.SetLeft(adorner, rcChild.Left);
			}

            draggedItem.IsVisible = !cond;
            canvas.IsVisible = cond;
        }
        /// <summary>
		/// Called at the end of Drag
		/// Should be overriden together with <see cref="OnStartDrag(PointerEventArgs)"/> in most cases
		/// </summary>
        protected virtual void OnStopDrag()
		{
            if (TabItems.Items.Count > 0)
            {
                RecalculateZIndex();
                int i = 0;
                while (i < TabItems.Items.Count)
                {
                    var container = ContainerFromIndex(i) as TabItem;
                    container.Classes.Remove("grabbed");
                    i++;
                }
            }
        }
        /// <summary>
		/// Called at the start of Drag (after first move of mouse not after first click!)
		/// Should be overriden together with <see cref="OnStopDrag"/> in most cases
		/// </summary>
		/// <param name="e"></param>
        protected virtual void OnStartDrag(PointerEventArgs e)
		{
			selectedItemsFrorDragging.Clear();
			if (e.KeyModifiers == KeyModifiers.Shift)
			{
				int i = 0;
				while (i < TabItems.Items.Count)
				{
					var container = ContainerFromIndex(i) as TabItem;
					if (DockManager.GetAllowDrag(((DockableItem)container.Content).Header))
					{
						container.Classes.Add("grabbed");
						selectedItemsFrorDragging.Add((DockableItem)container.Content);
					}
					i++;
				}
			}
			else
			{
				var container = ContainerFromIndex(SelectedIndex) as TabItem;
				if (DockManager.GetAllowDrag(((DockableItem)container.Content).Header))
				{
					container.Classes.Add("grabbed");
					selectedItemsFrorDragging.Add((DockableItem)container.Content);
				}
			}
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
			var root = (Window)this.VisualRoot;
			if (root is null) return;
			dockableCounterInWindows.TryGetValue(root, out var counter);
            if (dockable.TabItems.Items.Count is 0 && DockManager.GetAllowClose(dockable) && (counter.Count > 1 || DockManager.GetAllowLastClose(root)))
            {
				dockable.ReplaceWith(null);
            }
			if (counter.Count is 0 && DockManager.GetAllowLastClose(root))
			{
				CloseWindowRequested(root);
			}
		}
        /// <summary>
        /// Called whenever Dockable becomes empty and is last existing on Window and AllowLastClose plus AllowClose is true
        /// </summary>
        /// <param name="win"></param>
        public virtual void CloseWindowRequested(Window win)
		{
			win.Close();
		}
		private static DockableControl PrepareNewDockableControl(bool createdIntoNewWindow)
		{	
			var tab = sourceDockable.CreateDockable(createdIntoNewWindow);
			if (selectedItemsFrorDragging.Count > 0)
			{
				foreach (var removable in selectedItemsFrorDragging)
				{
					sourceDockable.TabItems.Items.Remove(removable);
					tab.TabItems.Items.Add(removable);
				}
			}
			return tab;
		}
		private static void DropTab(PointerReleasedEventArgs e)
		{
			if (lastTrigger.control is null)
			{
				if (draggedItem.IsVisible)
				{
					draggedItem.Hide();
					var tab = PrepareNewDockableControl(true);
					var dropWin = sourceDockable.CreateWindow(tab);
					dropWin.Show();
				}
			}
			else if(lastTrigger.area is not Region.None)
			{
				var targetDockable = lastTrigger.control.FindAncestorOfType<DockableControl>();
				if (targetDockable == sourceDockable && sourceDockable.TabItems.Items.Count == selectedItemsFrorDragging.Count)
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
                        foreach (var removable in selectedItemsFrorDragging)
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
                        foreach (var removable in selectedItemsFrorDragging)
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
					var tab = PrepareNewDockableControl(false);
					SplitTabControl(targetDockable, tab, lastTrigger.area);
				}
			}
		}
        /// <summary>
        /// Called whenever Dockable creates "real" window (as opposed to preview window) to house drag dropped Dockable (which is usually outside any existing app windows)
        /// </summary>
        /// <param name="dockable"></param>
        /// <returns></returns>
        protected virtual Window CreateWindow(DockableControl dockable)
		{
            var dropWin = new Window();

            dropWin.Position = draggedItem.Position;
            dropWin.Width = draggedItem.Width;
            dropWin.Height = draggedItem.Height;

            dropWin.Content = dockable;
			return dropWin;
        }

		/// <summary>
		/// Called whenever Dockable need to create new dockable after drag dropping TabItem. In most cases you should return self type, but this isn't hard requirement.
		/// </summary>
		/// <param name="createdIntoNewWindow"></param>
		/// <returns></returns>
		protected abstract DockableControl CreateDockable(bool createdIntoNewWindow);
    }
}