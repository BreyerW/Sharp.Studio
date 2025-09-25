using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Sharp.DockManager.ViewModels;
using Sharp.Studio.Views;

namespace Sharp.Studio.Models
{
	public class DockableAssets: DockableItem
	{
		public DockableAssets()
		{
			Header = new CloseableHeader() { Text="Assets", OnClickClose = CloseHeader, OnTogglePin = UnOrPinHeader };
			Content = new FirstView();
		}
		private void CloseHeader(object sender)
		{
			ParentCollection.Remove(this);
		}
        private void UnOrPinHeader(object sender)
        {
			var toggle = (ToggleButton)sender;
			if(toggle.IsChecked.GetValueOrDefault())
				ParentCollection.Move(ParentCollection.IndexOf(this),0);
        }
    }
}
