using System;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows;

namespace CustomMediaPlayerUltimate.Elements;

public partial class CollectionElement : UserControl
{
    public Action<CollectionElement> OnClickDelegate;

    private string _text = string.Empty;
    public string Text
    {
        get { return _text; }
        set
        {
            _text = value;
            ItemTextLabel.Content = value;
        }
    }

    private bool _focused = false;
    public bool Focused
    {
        get { return _focused; }
        set
        {
            _focused = value;
            FocusedIndicatorImage.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            //GridContainer.Background = value ? Brushes.Gray : new SolidColorBrush(Color.FromRgb(48, 48, 48));
        }
    }
    public bool FocusedProperty
    {
        get { return (bool)GetValue(FocusedPropertyProperty); }
        set { SetValue(FocusedPropertyProperty, value); }
    }
    public static readonly DependencyProperty FocusedPropertyProperty =
        DependencyProperty.Register("FocusedProperty", typeof(bool), typeof(CollectionElement), new PropertyMetadata(false));

    public CollectionElement(Action<CollectionElement> action)
    {
        InitializeComponent();
        OnClickDelegate = action;
    }

    private void ElementMouseUp(object sender, MouseButtonEventArgs e)
    {
        OnClickDelegate.Invoke(this);
        Focused = true;
    }
}
