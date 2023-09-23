using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Sharp.DockManager;
using Sharp.DockManager.ViewModels;

namespace Sharp.Studio.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DockableControl.HideContentWhenDrag = true;
            DockableControl.ConfigureNewDockable += (s,source)=> { 
            
            };
		}
	}
}
