using Avalonia.Controls;
using Sharp.DockManager.ViewModels;
using Sharp.Studio.Views;

namespace Sharp.Studio.Models
{
	public class DockableScene: DockableItem
	{
		public DockableScene()
		{
			Header = new TextBlock() { Text="Scene" };
			Content = new SceneView();
		}
	}
}
