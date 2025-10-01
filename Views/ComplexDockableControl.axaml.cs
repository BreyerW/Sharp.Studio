using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Sharp.DockManager;
using Sharp.DockManager.ViewModels;
using Sharp.Studio.Models;
using Sharp.Studio.ViewModels;

namespace Sharp.Studio.Views;

public sealed partial class ComplexDockable : DockableControl
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

        public void Execute(object parameter) => _execute(parameter);

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
    private static Window mainWindow;

    public static readonly StyledProperty<ICommand> AddTabCommandProperty =
        AvaloniaProperty.Register<ComplexDockable, ICommand>(nameof(AddTabCommand));

    public ICommand? AddTabCommand
    {
        get => GetValue(AddTabCommandProperty);
        set => SetValue(AddTabCommandProperty, value);
    }
    public ComplexDockable()
    {
        InitializeComponent();
        DataContext = new ComplexDockableViewModel();
        AddTabCommand = new RelayCommand(AddTab);
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        mainWindow ??= (Window)VisualRoot;
    }
    private void AddTab(object parameter)
    {
        var factory = (Func<DockableItem>)parameter;
        TabItems.Items.Add(factory());
    }
    public override DockableControl CreateDockable(bool createdIntoNewWindow)
    {
        return new ComplexDockable();
    }
    public override void CloseWindowRequested(Window win)
    {
        if (win != mainWindow)
            win.Close();
    }
}