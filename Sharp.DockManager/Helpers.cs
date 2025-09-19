using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.LogicalTree;

namespace Sharp.DockManager
{
	public static class Helpers
	{
		public static void CopyGridProperties(this Control source, Control copyTo)
		{
			Grid.SetColumn(copyTo, Grid.GetColumn(source));
			Grid.SetColumnSpan(copyTo, Grid.GetColumnSpan(source));
			Grid.SetRow(copyTo, Grid.GetRow(source));
			Grid.SetRowSpan(copyTo, Grid.GetRowSpan(source));
		}
        public static void SetAsColumn(Control c, int column)
        {
            Grid.SetRow(c, 0);
            Grid.SetRowSpan(c, 3);
            Grid.SetColumn(c, column);
            Grid.SetColumnSpan(c, 1);
        }
        public static void SetAsRow(Control c, int row)
        {
            Grid.SetRow(c, row);
            Grid.SetRowSpan(c, 1);
            Grid.SetColumn(c, 0);
            Grid.SetColumnSpan(c, 3);
        }
        //can always use DetachedFrom*Tree to clean up any special containers like Grid with splitters
        //or listen to on removed event on collections
        public static void ReplaceWith(this Control toBeReplaced, Control? replacement)
		{
			var parent = toBeReplaced.GetLogicalParent();
			if (parent is Panel p)
			{
				if (replacement is null)
				{
					p.Children.Remove(toBeReplaced);
				}
				else
				{
					var i = p.Children.IndexOf(toBeReplaced);
					if (i > -1)
					{
						p.Children[i] = replacement;
					}
				}
			}
			else if (parent is ContentControl cc)
			{
				cc.Content = replacement;
			}
			else if (parent is ContentPresenter cp)
			{
				cp.Content = replacement;
			}
			else if (parent is Decorator d)
			{
				d.Child = replacement;
			}
			else
				DockableControl.ReplaceControlRequested(toBeReplaced,replacement);
		}
	}
}
