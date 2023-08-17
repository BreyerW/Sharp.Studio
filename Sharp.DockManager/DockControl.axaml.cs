using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Layout;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.VisualTree;
using System.Linq;
using Avalonia.Input;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using Avalonia.Platform;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Avalonia.LogicalTree;

namespace Sharp.DockManager
{
    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    class test
    {
        
        //TODO: XQueryTree for X11 based Linux and for macos NSWindow.orderedIndex
        [DllImport("USER32.DLL")]
        public static extern bool EnumWindows(EnumWindowsProc enumFunc, IntPtr lParam);
    }
    public partial class DockControl : DockPanel
    {
        
        private static Window draggedItem = null;
        private static (Control control, Dock? area) lastTrigger = default;
        private static TabItem selectedTab = null;
        private static Point lastMousePos;
        private static PixelPoint mousePosOffset;
        private static Dictionary<Window,int> sortedWindows = new ();
        internal Border adornedElement = new Border();
        internal Canvas canvas = new Canvas();
        //turn into colection populated on attachedtotree and removed on detached
        internal static List<Canvas> activeElements = new();
        public static double accidentalMovePrevention = 7;
        public static bool preventEmptyDockControlInMainWindow = false;
        public ObservableCollection<IDockable> docked = new();
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

        static DockControl()
        {
            PointerPressedEvent.AddClassHandler<Control>((s, e) =>
            {
                lastMousePos = e.GetPosition(s);
            });
            PointerMovedEvent.AddClassHandler<Control>((s, e) =>
 {

     var lifetime = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime);
     if (draggedItem is null && e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
     {

         selectedTab = s.FindAncestorOfType<TabItem>(true);
         if (selectedTab is null) return;

         Point mousePos = e.GetPosition(s);
         if (Math.Abs(mousePos.X - lastMousePos.X) < accidentalMovePrevention && Math.Abs(mousePos.Y - lastMousePos.Y) < accidentalMovePrevention) return;

         var screenPos = s.PointToScreen(mousePos);
         var dockable = selectedTab.FindAncestorOfType<IDockable>(true) as DockableTabControl;
         var panel = dockable.FindAncestorOfType<DockControl>(true);
         var attachedToWin = panel.FindAncestorOfType<Window>();
         var relativePos = s.GetVisualParent().PointToScreen(s.Bounds.Position);
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


         //lastTrigger = draggedItem;
         if (draggedItem is null)
         {
             draggedItem = new Window() { ShowInTaskbar = true };
         }
         var docker = new DockControl();
         var tab = new DockableTabControl() { Dock = Dock.Left};
         tab.AddPage(selectedTab);
         docker.StartWithDocks(new[] { tab });

         draggedItem.Content = docker;
         mousePosOffset = screenPos - relativePos.WithY(relativePos.Y);
         draggedItem.Position = new PixelPoint(relativePos.X, relativePos.Y - 33);
         draggedItem.SizeToContent = SizeToContent.WidthAndHeight;
         draggedItem.SystemDecorations = SystemDecorations.BorderOnly;
         //draggedItem.Width = s.Bounds.Width;
         //draggedItem.Height = s.Bounds.Height;
         draggedItem.ShowInTaskbar = false;
         draggedItem.Show();
         draggedItem.PositionChanged += DraggedItem_PositionChanged;
         draggedItem.BeginMoveDrag(new PointerPressedEventArgs(draggedItem, e.Pointer, draggedItem, default, e.Timestamp, e.GetCurrentPoint(s).Properties, e.KeyModifiers));

     }
 });
        }

        public DockControl()
        {
            InitializeComponent();
            LastChildFill = true;
        }
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            adornedElement.Background = Brushes.PaleVioletRed;
            adornedElement.Opacity = 0.33;
            adornedElement.IsVisible = true;


            canvas.Children.Add(adornedElement);
            activeElements.Add(canvas);
            var adornerLayer = AdornerLayer.GetAdornerLayer(this);
            adornerLayer.Children.Add(canvas);
            AdornerLayer.SetAdornedElement(canvas, this);
            foreach (var c in activeElements)
                c.IsVisible = false;
        }
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            activeElements.Remove(canvas);
        }
        private static void DraggedItem_PositionChanged(object? sender, PixelPointEventArgs e)
        {
            //TODO: do it only once after window is dragged off
            UpdateZOrder();
            var screenPos = e.Point + mousePosOffset;
            (Control control, Dock? area) currentTrigger = default;

            Control? hit = null;
            foreach (var (window, zorder) in sortedWindows.OrderBy(key=> key.Value))
            {
                if (window == draggedItem) continue;
                var pos = window.PointToClient(screenPos);
                if (pos is { X: 0, Y: 0 }) continue;

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

                    var dControl = d.FindAncestorOfType<DockControl>();
                    if (dControl is null or { Children: null or { Count: 0 } })
                        break;
                    var children = new Controls(dControl.Children);
                    var index = children.IndexOf(d);
                    var dockUnderMouse = DockPanel.GetDock(dControl.Children[index] as Control);
                    var stopIndex = PlaceDraggedItemIntoDockControl(dockUnderMouse, currentTrigger.area.Value, children, index);
                    var arrangeSize = dControl.Bounds;
                    int totalChildrenCount = children.Count;
                    int nonFillChildrenCount = totalChildrenCount - (dControl.LastChildFill ? 1 : 0);

                    double accumulatedLeft = 0;
                    double accumulatedTop = 0;
                    double accumulatedRight = 0;
                    double accumulatedBottom = 0;

                    for (int i = 0; i < totalChildrenCount; ++i)
                    {
                        var child = children[i];
                        if (child == null)
                        { continue; }

                        Size childDesiredSize = child.DesiredSize;
                        Rect rcChild = new Rect(
                            accumulatedLeft,
                            accumulatedTop,
                            Math.Max(0.0, arrangeSize.Width - (accumulatedLeft + accumulatedRight)),
                            Math.Max(0.0, arrangeSize.Height - (accumulatedTop + accumulatedBottom)));

                        if (i < nonFillChildrenCount)
                        {
                            switch (DockPanel.GetDock((Control)child))
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
                        }
                        if (i == stopIndex)
                        {
                            var adornerLayer = AdornerLayer.GetAdornerLayer(dControl);

                            if (adornerLayer != null)
                            {
                                foreach (var c in activeElements)
                                    c.IsVisible = false;
                                dControl.adornedElement.GetVisualParent().IsVisible = true;
                                dControl.adornedElement.Width = rcChild.Width;
                                dControl.adornedElement.Height = rcChild.Height;

                                Canvas.SetTop(dControl.adornedElement, rcChild.Top);
                                Canvas.SetLeft(dControl.adornedElement, rcChild.Left);
                            }
                            break;
                        }
                    }

                    break;
                }
            }
            if (currentTrigger.control is null)
                foreach (var c in activeElements)
                    c.IsVisible = false;
            lastTrigger = currentTrigger;
        }
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (draggedItem is null) return;

            draggedItem.PositionChanged -= DraggedItem_PositionChanged;
            if (lastTrigger.control is null)
            {
                draggedItem.SystemDecorations = SystemDecorations.Full;
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
                        else*/ PlaceDraggedItemIntoDockControl(dockUnderMouse, lastTrigger.area.Value, targetDockControl.Children, index);
                    }
                    else
                    {
                        sourceDockable._tabItems.Remove(selectedTab);
                        var newTab = new DockableTabControl();
                        newTab._tabItems.Add(selectedTab);
                        /*if (index == targetDockControl.Children.Count-1 && lastTrigger.area is Dock.Right or Dock.Bottom)
                            targetDockControl.Children.Add(newTab);
                        else*/ PlaceDraggedItemIntoDockControl(dockUnderMouse, lastTrigger.area.Value, targetDockControl.Children, index);
                    }

                }
            }
            foreach (var c in activeElements)
                c.IsVisible = false;
            draggedItem = null;
            selectedTab.IsSelected = true;
            selectedTab = null;
            lastTrigger = (null, null);
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
            var parent=selectedTab.FindAncestorOfType<DockableTabControl>();
            parent._tabItems.Remove(selectedTab);
            selectedTab = new TabItem() { Header = selectedTab.Header, Content = selectedTab.Content };
            parent._tabItems.Add(selectedTab);
            targetChildrens.Insert(finalIndex, parent);
            ((IDockable)parent).Dock = dock;
            DockPanel.SetDock(parent, dock);
            Debug.WriteLine("docks: " + dockUnderMouse + " " + areaUnderMouse + " " + finalIndex + " " + targetChildrens.Count + " " + index);
            return finalIndex;
        }
        private static int copyPlaceDraggedItemIntoDockControl(Dock dockUnderMouse, Dock areaUnderMouse, Controls targetChildrens, int index)
        {
            (int finalIndex, Dock dock) = (dockUnderMouse, areaUnderMouse) switch
            {
                (Dock.Left, Dock.Right) => (index + 1, Dock.Left),
                (Dock.Right, Dock.Right) or (Dock.Left, Dock.Left) or (Dock.Top, Dock.Top) or (Dock.Bottom, Dock.Bottom) => (index, dockUnderMouse),
                (Dock.Top, Dock.Bottom) or (Dock.Bottom, Dock.Top) => (index + 1, areaUnderMouse),
                _ => (index, areaUnderMouse),
            };
            var parent = selectedTab.FindAncestorOfType<DockableTabControl>();
            
            targetChildrens.Insert(finalIndex, parent);
            ((IDockable)parent).Dock = dock;
            DockPanel.SetDock(parent, dock);
            Debug.WriteLine("docks: " + dockUnderMouse + " " + areaUnderMouse + " " + finalIndex + " " + targetChildrens.Count + " " + index);
            return finalIndex;
        }
        /// <summary>
        /// DockPanel computes a position and final size for each of its children based upon their
        /// <see cref="Dock" /> enum and sizing properties.
        /// </summary>
        /// <param name="arrangeSize">Size that DockPanel will assume to position children.</param>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            var children = Children;
            int totalChildrenCount = children.Count;
            int nonFillChildrenCount = totalChildrenCount - (LastChildFill ? 1 : 0);

            double accumulatedLeft = 0;
            double accumulatedTop = 0;
            double accumulatedRight = 0;
            double accumulatedBottom = 0;

            for (int i = 0; i < totalChildrenCount; ++i)
            {
                var child = children[i];
                if (child == null)
                { continue; }

                Size childDesiredSize = child.DesiredSize;
                Rect rcChild = new Rect(
                    accumulatedLeft,
                    accumulatedTop,
                    Math.Max(0.0, arrangeSize.Width - (accumulatedLeft + accumulatedRight)),
                    Math.Max(0.0, arrangeSize.Height - (accumulatedTop + accumulatedBottom)));

                if (i < nonFillChildrenCount)
                {
                    switch (DockPanel.GetDock((Control)child))
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
                }

                child.Arrange(rcChild);
            }

            return arrangeSize;
        }
        public void StartWithDocks<T>(T[] docks, params float[] proportions) where T : Control, IDockable
        {

            var i = 0;
            foreach (var dock in docks)
            {
                if (i > 0 && i < docks.Length)
                {
                    //var gridSplitter = CreateSplitter(dock.Dock);
                    //  DockPanel.SetDock(gridSplitter, dock.Dock);
                    //    Children.Add(gridSplitter);
                }
                DockPanel.SetDock(dock, dock.Dock);
                Children.Add(dock);
                //docked.Add(dock);

                i++;
            }
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
    }
}
