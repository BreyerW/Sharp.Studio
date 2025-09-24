using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Sharp.DockManager;
using Sharp.Studio.ViewModels;

namespace Sharp.Studio.Views;

public sealed partial class ComplexDockable : DockableControl
{
    protected override Type StyleKeyOverride => typeof(ComplexDockable);
    public ComplexDockable()
    {
        InitializeComponent();
    }

    public override DockableControl CreateDockable()
    {
        return new ComplexDockable();
    }
    public ICommand? AddTab { get; }
}