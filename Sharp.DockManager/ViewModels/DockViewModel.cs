using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharp.DockManager.ViewModels
{
    public class DockViewModel : ViewModelBase
    {
        public ObservableCollection<IDockable> docked = new();
        public RowDefinitions rows = new RowDefinitions();
        //public ColumnDefinitions cols = new ColumnDefinitions();
        public string cols = "*,Auto,*,Auto,*";

        public DockViewModel()
        {
            docked.CollectionChanged += Docked_CollectionChanged;
        }
        private void Docked_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            //if (docked.Count is 0)
            //  grid.Children.Remove(e.OldItems[e.OldStartingIndex] as Control);
        }
    }
}
