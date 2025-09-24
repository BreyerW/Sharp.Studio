using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Sharp.DockManager;

public sealed partial class DefaultDockableControl : DockableControl
{
    public DefaultDockableControl()
    {
        InitializeComponent();
    }

    public override DockableControl CreateDockable()
    {
        return new DefaultDockableControl();
    }
}