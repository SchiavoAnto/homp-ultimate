using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.Generic;
using CustomMediaPlayerUltimate.DataStructures;

namespace CustomMediaPlayerUltimate.Elements;

public partial class PlaylistElement : UserControl
{
    public SongCollection Playlist { get; private set; }

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

    public bool Focused
    {
        get { return (bool)GetValue(FocusedProperty); }
        set { SetValue(FocusedProperty, value); }
    }
    public static readonly DependencyProperty FocusedProperty =
        DependencyProperty.Register("Focused", typeof(bool), typeof(PlaylistElement), new PropertyMetadata(false));

    public PlaylistElement(SongCollection playlist)
    {
        InitializeComponent();
        Playlist = playlist;
    }

    private void ElementMouseUp(object sender, MouseButtonEventArgs e)
    {
        Focused = true;
        MainWindow.Instance.PlaylistElementClick(this);
    }

    private void ElementMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        MainWindow.Instance.PlaylistElementDoubleClick(this);
    }

    private void PlayCtxClicked(object sender, RoutedEventArgs e)
    {
        MainWindow.Instance.PlaylistElementDoubleClick(this);
    }

    private void ManageSongsCtxClicked(object sender, RoutedEventArgs e)
    {
        MainWindow.Instance.PlaylistElementManageClick(this);
    }

    private void RenameCtxClicked(object sender, RoutedEventArgs e)
    {
        MainWindow.Instance.PlaylistElementRenameClick(this);
    }

    private void DeleteCtxClicked(object sender, RoutedEventArgs e)
    {
        MainWindow.Instance.PlaylistElementDeleteClick(this);
    }
}
