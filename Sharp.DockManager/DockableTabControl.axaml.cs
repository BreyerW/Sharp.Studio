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
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Sharp.DockManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Sharp.DockManager
{

    public partial class DockableTabControl : TabControl, IStyleable, IDockable
    {
        private ScrollViewer scroller;
        Type IStyleable.StyleKey => typeof(DockableTabControl);
        public ObservableCollection<TabItem> _tabItems { get; set; } = new();
        public Dock Dock { get; set; }
        public Control SelectedHeader { get => SelectedItem as Control; }
        public Control SelectedBody { get => SelectedContent as Control; }

        private Control partSelectedControlHost;
        private Control grid;
        public DockableTabControl()
        {
            InitializeComponent();
            ItemsSource = _tabItems;
            Dock = Dock.Left; 
        }
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            scroller = this.FindDescendantOfType<ScrollViewer>();
            scroller.PointerWheelChanged += Scroller_PointerWheelChanged;
        }

        private void Scroller_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (e.Delta.Y > 0)
               scroller.LineLeft();
            else if (e.Delta.Y < 0)
                scroller.LineRight();
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
