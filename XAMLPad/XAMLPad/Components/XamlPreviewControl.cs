using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace XAMLPad.Components;

public class XamlPreviewControl : ContentControl
{
    public static readonly DependencyProperty XamlTextProperty =
        DependencyProperty.Register (
            nameof (XamlText),
            typeof (string),
            typeof (XamlPreviewControl),
            new PropertyMetadata (null, OnXamlTextChanged));

    public string XamlText
    {
        get => (string)GetValue (XamlTextProperty);
        set => SetValue (XamlTextProperty, value);
    }

    private static void OnXamlTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (XamlPreviewControl)d;
        control.RenderXaml (e.NewValue as string);
    }

    private void RenderXaml(string xaml)
    {
        if (string.IsNullOrWhiteSpace (xaml))
        {
            Content = null;
            return;
        }

        try
        {
            var ui = (FrameworkElement)XamlReader.Load (xaml);
            Content = ui;
        }
        catch (Exception ex)
        {
            Content = CreateErrorView (ex);
        }
    }

    private UIElement CreateErrorView(Exception ex)
    {
        return new Border
        {
            Background = new SolidColorBrush (Colors.DarkRed),
            Padding = new Thickness (10),
            Child = new TextBlock
            {
                Text = ex.Message,
                Foreground = new SolidColorBrush (Colors.White),
                TextWrapping = TextWrapping.Wrap
            }
        };
    }
}
