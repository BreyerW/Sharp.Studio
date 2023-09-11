﻿using Avalonia.Controls;
using Sharp.DockManager.ViewModels;
using Sharp.Studio.Views;

namespace Sharp.Studio.Models
{
	public class DockableAssets: DockableItem
	{
		public DockableAssets()
		{
			Header = new CloseableHeader() { Text="Assets"};
			Content = new AssetsView();
		}
	}
}
