using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Sharp.Studio.Views
{
    public class AssetsView : UserControl
    {
        public AssetsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
