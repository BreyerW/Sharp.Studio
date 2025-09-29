using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Sharp.DockManager;

namespace Sharp.Studio.Views;

public partial class CloseableHeader : UserControl
{
	public static readonly StyledProperty<string> TextProperty =
					AvaloniaProperty.Register<CloseableHeader, string>(nameof(Text));
	public string Text
    {
		get
		{
			return GetValue(TextProperty);
		}
		set
		{
			SetValue(TextProperty, value);
		}
	}
    public static readonly StyledProperty<float> RotationProperty =
                    AvaloniaProperty.Register<CloseableHeader, float>(nameof(Rotation));
    public float Rotation
    {
        get
        {
            return GetValue(RotationProperty);
        }
        set
        {
            SetValue(RotationProperty, value);
        }
    }
    public Action<object> OnClickClose;
    public Action<object> OnTogglePin;

    public CloseableHeader()
    {
        InitializeComponent();
		DataContext = this;
    }


    private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        OnClickClose?.Invoke(sender);
    }
    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
    }
    private void ToggleButton_Checked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DockManager.DockManager.SetAllowDrag(this, false);
        OnTogglePin?.Invoke(sender);
    }

    private void ToggleButton_Unchecked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DockManager.DockManager.SetAllowDrag(this, true);
        OnTogglePin?.Invoke(sender);
    }
}