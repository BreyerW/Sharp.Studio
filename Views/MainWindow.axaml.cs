using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Sharp.DockManager;
using Sharp.DockManager.ViewModels;

namespace Sharp.Studio.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //just inherit from dckablecontrol if you need this functionality
            //and sourceDockable is protected now so use that in inheritance
            //same goes for TabItem just override PrepareContainerForItem
            //DockableControl.ConfigureNewDockable += (s,source)=> {

                //if (App.Current.Resources.TryGetResource("CustomDockable", ThemeVariant.Dark, out var t) && t is ControlTheme theme)
                    //s.Theme = theme;
            //};
		}
	}
}
