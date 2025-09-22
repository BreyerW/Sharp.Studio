using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

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
    public Action<object, Avalonia.Interactivity.RoutedEventArgs> OnClick;
    private TabItem tab;
    private Path tabShape;
    private IBrush oldBrush;

    public CloseableHeader()
    {
        InitializeComponent();
		DataContext = this;
    }


    private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        OnClick?.Invoke(sender,e);
    }
    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
    }
}