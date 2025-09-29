using Avalonia.Controls;
using Sharp.DockManager.ViewModels;
using Sharp.Studio.Views;

namespace Sharp.Studio.Models
{
	public class DockableSecondView: DockableItem
	{
		public DockableSecondView()
		{
			Header = new CloseableHeader() { Text = "Second view"};
			Content = new SecondView();
		}
	}
}
