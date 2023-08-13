using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;
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
        Type IStyleable.StyleKey => typeof(TabControl);
        public ObservableCollection<TabItem> _tabItems { get; set; } = new();
        public Dock Dock { get; set; }
        public Control Header { get; set; }
        public Control Body { get; set; }

        public DockableTabControl()
        {
            InitializeComponent();
            ItemsSource = _tabItems;
            //var data = new DockableTabViewModel() { _tabItems = _tabItems };
            //DataContext = data;
            Dock = Dock.Left;
        }
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            scroller = this.FindDescendantOfType<ScrollViewer>(true);
            scroller.AddHandler(PointerWheelChangedEvent, Scroller_PointerWheelChanged, RoutingStrategies.Tunnel | RoutingStrategies.Direct, handledEventsToo: true);
            //Header = scroller.Content as Control;
            Body = this.FindDescendantOfType<ContentPresenter>(true);
        }

        private void Scroller_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (e.Delta.Y > 0)
                scroller.LineRight();
            else if (e.Delta.Y < 0)
                scroller.LineLeft();
        }
        public void AddPage(TabItem item)
        {
            _tabItems.Add(item);
        }
    }
}
