using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Sharp.Studio.Views
{
    public class SceneView : UserControl
    {
        public SceneView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
