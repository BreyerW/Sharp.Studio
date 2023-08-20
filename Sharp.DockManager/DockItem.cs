using Avalonia;
using Avalonia.Controls;

namespace Sharp.DockManager
{
    public class DockItem : TabItem
    {
        /// <summary>
        /// Sets whether the width will be overridden.
        /// </summary>
        public static readonly AvaloniaProperty PositionProperty
                = AvaloniaProperty.Register<DockItem, Point>(
                        nameof(DockItem.Position));
        /// <summary>
        /// Sets whether the width will be overridden.
        /// </summary>
        public Point Position
        {
            get => (Point)GetValue(DockItem.PositionProperty);
            set => SetValue(DockItem.PositionProperty, value);
        }
        public Control Header { get; set; }
        public Control Content { get; set; }
    }
}
