using Avalonia;
using Avalonia.Controls;

namespace Sharp.DockManager.ViewModels
{
	public class DockableItem
    {
    //maybe add isremovable and ismoveable/sortable or just movable
		public Control Header { get; set; }
        public Control Content { get; set; }

    }
}
