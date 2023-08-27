using Avalonia;
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
        public ObservableCollection<DockableItem> Items { get; set; } = new();
    }
    public struct DockableItem
    {
        /// <summary>
        /// Sets whether the width will be overridden.
        /// </summary>
       /* public static readonly AvaloniaProperty PositionProperty
                = AvaloniaProperty.Register<DockItem, Point>(
                        nameof(DockItem.Position));
        /// <summary>
        /// Sets whether the width will be overridden.
        /// </summary>
        public Point Position
        {
            get => (Point)GetValue(DockItem.PositionProperty);
            set => SetValue(DockItem.PositionProperty, value);
        }*/
        public Control Header { get; set; }
        public Control Content { get; set; }
    }
}
