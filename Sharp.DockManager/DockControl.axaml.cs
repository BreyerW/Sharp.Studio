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
using Sharp.DockManager.Behaviours;
using SharpHook;
using Avalonia.Threading;

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
		public DockControl()
        {
            InitializeComponent();
            LastChildFill = true;
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
