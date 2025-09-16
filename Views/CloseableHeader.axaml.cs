using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace Sharp.Studio.Views;
//have look in future: https://www.sharpgis.net/post/Rotating-Elements-in-XAML-While-Maintaining-Proper-Flow
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

    public static readonly StyledProperty<string> PathDataProperty =
                    AvaloniaProperty.Register<CloseableHeader, string>(nameof(PathData));
    public string PathData
    {
        get
        {
            return GetValue(PathDataProperty);
        }
        set
        {
            SetValue(PathDataProperty, value);
        }
    }

    public CloseableHeader()
    {
        InitializeComponent();
		DataContext = this;
    }

    private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        OnClick?.Invoke(sender,e);
    }
    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        var tab = this.FindAncestorOfType<TabItem>();
        if (tab == null)
            return;
        double width = tab.DesiredSize.Width - 1;
        
        double height = tab.DesiredSize.Height;
        double x1 = width - 15;
        double x2 = width - 10;
        double x3 = width - 5;
        double x4 = width - 2.5;
        double x5 = width;
        PathData = string.Format(CultureInfo.InvariantCulture, "M0,{5} C2.5,{5} 5,0 10,0 15,0 {0},0 {1},0 {2},0 {3},{5} {4},{5}", x1, x2, x3, x4, x5, height);

    }
}