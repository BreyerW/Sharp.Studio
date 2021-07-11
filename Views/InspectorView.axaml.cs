using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Sharp.Studio.Views
{
    public class InspectorView : UserControl
    {
        public InspectorView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
