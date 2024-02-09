using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.Generic;

namespace CustomMediaPlayerUltimate.Elements;

public partial class PlaylistElement : UserControl
{
    public Action<PlaylistElement> OnClickDelegate;
    public Action<PlaylistElement> OnDoubleClickDelegate;
    public Action OnManageSongsCtxClickedDelegate;
    public Action OnRenameCtxClickedDelegate;
    public Action OnDeleteCtxClickedDelegate;

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

    private Dictionary<string, RoutedEventHandler> _contextMenuOptions = new();
    public Dictionary<string, RoutedEventHandler> ContextMenuOptions
    {
        get { return _contextMenuOptions; }
        set
        {
            _contextMenuOptions = value;
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
        DependencyProperty.Register("FocusedProperty", typeof(bool), typeof(PlaylistElement), new PropertyMetadata(false));

    public PlaylistElement(Action<PlaylistElement> onClickDelegate, Action<PlaylistElement> onDoubleClickDelegate, Action onManageSongsCtxClickedDelegate, Action onRenameCtxClickedDelegate, Action onDeleteCtxClickedDelegate)
    {
        InitializeComponent();
        OnClickDelegate = onClickDelegate;
        OnDoubleClickDelegate = onDoubleClickDelegate;
        OnManageSongsCtxClickedDelegate = onManageSongsCtxClickedDelegate;
        OnRenameCtxClickedDelegate = onRenameCtxClickedDelegate;
        OnDeleteCtxClickedDelegate = onDeleteCtxClickedDelegate;
    }

    private void ElementMouseUp(object sender, MouseButtonEventArgs e)
    {
        OnClickDelegate.Invoke(this);
        Focused = true;
    }

    private void ElementMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        OnDoubleClickDelegate.Invoke(this);
    }

    private void ManageSongsCtxClicked(object sender, RoutedEventArgs e)
    {
        OnManageSongsCtxClickedDelegate.Invoke();
    }

    private void RenameCtxClicked(object sender, RoutedEventArgs e)
    {
        OnRenameCtxClickedDelegate.Invoke();
    }

    private void DeleteCtxClicked(object sender, RoutedEventArgs e)
    {
        OnDeleteCtxClickedDelegate.Invoke();
    }
}
