using System.Collections;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Microsoft.VisualBasic;

namespace Sharp.DockManager.ViewModels
{
	public class DockableItem
    {
        public ICollection<DockableItem> ParentCollection { get; set; }
		public Control Header { get; set; }
        public Control Content { get; set; }
    }
}
