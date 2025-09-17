using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
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
		public void DockableControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			/*var dock = (DockableControl)sender;
			foreach (var newSelect in e.AddedItems)
			{
				var container = dock.ContainerFromItem(newSelect);
				if (container is null)
					continue;
				container.ZIndex = 1;
			}
            foreach (var deselect in e.RemovedItems)
            {
                var container = dock.ContainerFromItem(deselect);
                if (container is null)
                    continue;
                container.ZIndex = 0;
            }*/
        }

    }
}
