using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

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
	public CloseableHeader()
    {
        InitializeComponent();
		DataContext = this;
    }
}