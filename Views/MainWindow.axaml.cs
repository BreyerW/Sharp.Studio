using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Sharp.DockManager;
using Sharp.DockManager.ViewModels;

namespace Sharp.Studio.Views
{
    public partial class MainWindow : Window
    {
        //private DockControl dockControl;
        public MainWindow()
        {
            InitializeComponent();
            var assetsView = new DockableTabControl() { Dock = Dock.Left, HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
            assetsView.AddPage(new TextBlock() { Text = nameof(AssetsView) },  new AssetsView() );
            //var assetsView1 = new DockableTabControl() { Dock = Dock.Right};
            // assetsView1.AddPage(new TabItem() { Header = new TextBlock() { Text = nameof(AssetsView) }, Content = new AssetsView() });

            var sceneView = new DockableTabControl() { Dock = Dock.Left };
            sceneView.AddPage(new TextBlock() { Text = nameof(SceneView) }, new SceneView() );
            sceneView.AddPage(new TextBlock() { Text = nameof(AssetsView) }, new AssetsView());
            var inspectorView = new DockableTabControl() { Dock = Dock.Right };
            inspectorView.AddPage(new TextBlock() { Text = nameof(InspectorView) }, new InspectorView());
            var inspectorView1 = new DockableTabControl() { IsVisible = true, Dock = Dock.Right };
            //inspectorView1.AddPage(new TabItem() { Header = null , Content = null });

            dockControl.StartWithDocks(new[] { sceneView, assetsView, inspectorView });

        }

        
    }
}
