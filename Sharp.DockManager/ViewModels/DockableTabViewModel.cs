using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharp.DockManager.ViewModels
{
    public class DockableTabViewModel : ViewModelBase
    {
        public ObservableCollection<TabItem> _tabItems { get; set; }
    }
}
