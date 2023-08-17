using Avalonia.Layout;
using System;
using Avalonia.Controls;

namespace Sharp.DockManager
{
    public interface IDockable
    {
        Control SelectedHeader { get; }
        Control SelectedBody { get; }
        Dock Dock { set; get; }
    }
}
