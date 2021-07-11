using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Sharp.DockManager;

namespace Sharp.Studio.Views
{
    public class MainWindow : Window
    {
        private DockControl dockControl;
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            dockControl = this.FindControl<DockControl>(nameof(dockControl));
            var assetsView = new DockableTabControl() { Dock = Dock.Left, HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
            assetsView.AddPage(new TabItem() { Header = new TextBlock() { Text = nameof(AssetsView) }, Content = new AssetsView() });
            //var assetsView1 = new DockableTabControl() { Dock = Dock.Right};
            // assetsView1.AddPage(new TabItem() { Header = new TextBlock() { Text = nameof(AssetsView) }, Content = new AssetsView() });

            var sceneView = new DockableTabControl() { Dock = Dock.Left};
            sceneView.AddPage(new TabItem() { Header = nameof(SceneView), Content = new SceneView() });
            sceneView.AddPage(new TabItem() { Header = nameof(AssetsView), Content = new AssetsView() });
            var inspectorView = new DockableTabControl() { Dock = Dock.Right };
            inspectorView.AddPage(new TabItem() { Header = new TextBlock() { Text = nameof(InspectorView) }, Content = new InspectorView() });
            var inspectorView1 = new DockableTabControl() { IsVisible = false, Dock = Dock.Right };
            inspectorView1.AddPage(new TabItem() { Header = new TextBlock() { Text = null }, Content = null });

            dockControl.StartWithDocks(new[] { sceneView, assetsView,inspectorView });
        }
    }
}
