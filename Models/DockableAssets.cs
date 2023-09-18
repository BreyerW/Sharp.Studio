using Avalonia.Controls;
using Sharp.DockManager.ViewModels;
using Sharp.Studio.Views;

namespace Sharp.Studio.Models
{
	public class DockableAssets: DockableItem
	{
		public DockableAssets()
		{
			Header = new CloseableHeader() { Text="Assets"};
			Header.PointerPressed += Header_PointerPressed;
			Content = new AssetsView();
		}

		private void Header_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
		{
			//e.Handled = true;
		}
	}
}
