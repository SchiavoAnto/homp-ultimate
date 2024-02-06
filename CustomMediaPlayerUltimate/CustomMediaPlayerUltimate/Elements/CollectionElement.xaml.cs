using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace CustomMediaPlayerUltimate.Elements;

public partial class CollectionElement : UserControl
{
    public Action<CollectionElement> OnClickDelegate;
    public Action<CollectionElement> OnDoubleClickDelegate;

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

    public CollectionElement(Action<CollectionElement> onClickDelegate, Action<CollectionElement> onDoubleClickDelegate)
    {
        InitializeComponent();
        OnClickDelegate = onClickDelegate;
        OnDoubleClickDelegate = onDoubleClickDelegate;
    }

    private void ElementMouseUp(object sender, MouseButtonEventArgs e)
    {
        // Don't proceed if we are already focused.
        if (Focused) return;

        // If we click, we want to call the click delegate
        // (which should show this collection's songs in a list) and change state to focused.
        OnClickDelegate.Invoke(this);
        Focused = true;
    }

    private void ElementMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        OnDoubleClickDelegate.Invoke(this);
    }
}
