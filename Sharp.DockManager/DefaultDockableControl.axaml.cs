using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Sharp.DockManager;

public sealed partial class DefaultDockableControl : DockableControl
{
    private static Window mainWindow;

    public DefaultDockableControl()
    {
        InitializeComponent();
    }
    public DefaultDockableControl(Window mainWindow) : this()
    {
        DefaultDockableControl.mainWindow = mainWindow;
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        mainWindow ??= (Window)VisualRoot;
    }
    public override void CloseWindowRequested(Window win)
    {
        if (win != mainWindow)
            win.Close();
    }
    public override DockableControl CreateDockable(bool createdIntoNewWindow)
    {
        return new DefaultDockableControl();
    }
}