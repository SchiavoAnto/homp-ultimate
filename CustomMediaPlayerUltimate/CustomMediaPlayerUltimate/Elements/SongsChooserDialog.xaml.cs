using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Collections.Generic;
using CustomMediaPlayerUltimate.DataStructures;

namespace CustomMediaPlayerUltimate.Elements;

public partial class SongsChooserDialog : Window
{
    private Brush itemBackgroundBrush = new SolidColorBrush(Color.FromRgb(48, 48, 48));

    public List<Song> Result = new List<Song>();

    public SongsChooserDialog(SongCollection source, SongCollection destination)
    {
        InitializeComponent();

        foreach (Song song in source.Songs.Values)
        {
            bool isAlreadyPresent = destination.Songs.ContainsKey(song.FilePath);
            if (isAlreadyPresent) Result.Add(song);

            StackPanel stackPanel = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                Background = itemBackgroundBrush,
                Height = 30d,
                Margin = new Thickness(5, 5, 5, 0)
            };
            CheckBox checkBox = new CheckBox()
            {
                IsChecked = isAlreadyPresent,
                Content = song.FileName,
                Foreground = Brushes.WhiteSmoke,
                Margin = new Thickness(5, 7, 5, 7),
            };
            checkBox.Checked += (s, e) =>
            {
                Result.Add(song);
            };
            checkBox.Unchecked += (s, e) =>
            {
                Result.Remove(song);
            };
            
            stackPanel.Children.Add(checkBox);

            ContainerPanel.Children.Add(stackPanel);
        }
    }

    private void CancelButtonClick(object? sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ConfirmButtonClick(object? sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
