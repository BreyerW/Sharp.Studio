using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Sharp.Studio.ViewModels;
using Sharp.Studio.Views;

namespace Sharp.Studio
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
    //TODO: move to grid so that this becomes non issue
    public sealed class NoSizeDecorator
            : Decorator
    {
        /// <summary>
        /// Sets whether the width will be overridden.
        /// </summary>
        public static readonly AvaloniaProperty SizeDependencyProperty
                = AvaloniaProperty.Register<NoSizeDecorator, Control>(
                        nameof(NoSizeDecorator.SizeDependency));
        /// <summary>
        /// Sets whether the width will be overridden.
        /// </summary>
        public Control SizeDependency
        {
            get => (Control)GetValue(NoSizeDecorator.SizeDependencyProperty);
            set => SetValue(NoSizeDecorator.SizeDependencyProperty, value);
        }
        /// <summary>
        /// Sets whether the width will be overridden.
        /// </summary>
        public static readonly AvaloniaProperty DisableWidthOverrideProperty
                = AvaloniaProperty.Register<NoSizeDecorator,bool>(
                        nameof(NoSizeDecorator.DisableWidthOverride));

        /// <summary>
        /// Sets whether the width will be overridden.
        /// </summary>
        public bool DisableWidthOverride
        {
            get => (bool)GetValue(NoSizeDecorator.DisableWidthOverrideProperty);
            set => SetValue(NoSizeDecorator.DisableWidthOverrideProperty, value);
        }

        /// <summary>
        /// Sets whether the height will be overridden.
        /// </summary>
        public static readonly AvaloniaProperty DisableHeightOverrideProperty
                = AvaloniaProperty.Register<NoSizeDecorator, bool>(
                        nameof(NoSizeDecorator.DisableHeightOverride));

        /// <summary>
        /// Sets whether the height will be overridden.
        /// </summary>
        public bool DisableHeightOverride
        {
            get => (bool)GetValue(NoSizeDecorator.DisableHeightOverrideProperty);
            set => SetValue(NoSizeDecorator.DisableHeightOverrideProperty, value);
        }
        protected override Size MeasureOverride(Size constraint)
        {
            Control child = Child;
            if (child == null)
                return new Size();
            var height = child.Height;
            child.Measure(constraint);
            SizeDependency.Measure(constraint);
            child.Width = SizeDependency.DesiredSize.Width;
            return new Size(child.Width, height);/*new Size(
                    DisableWidthOverride
                            ? child.DesiredSize.Width
                            : SizeDependency.DesiredSize.Width,
                    DisableHeightOverride
                            ? child.DesiredSize.Height
                            : SizeDependency.DesiredSize.Height);*/
        }
    }
}
