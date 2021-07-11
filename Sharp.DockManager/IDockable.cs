using Avalonia.Layout;
using System;
using Avalonia.Controls;

namespace Sharp.DockManager
{
    public interface IDockable
    {
        Control Header { set; get; }
        Control Body { set; get; }
        Dock Dock { set; get; }
    }
}
