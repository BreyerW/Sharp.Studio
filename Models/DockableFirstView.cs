using Avalonia.Controls.Primitives;
using Sharp.DockManager.ViewModels;
using Sharp.Studio.Views;

namespace Sharp.Studio.Models
{
	public class DockableFirstView: DockableItem
	{
		public DockableFirstView()
		{
			Header = new CloseableHeader() { Text="First View", OnClickClose = CloseHeader, OnTogglePin = UnOrPinHeader };
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
