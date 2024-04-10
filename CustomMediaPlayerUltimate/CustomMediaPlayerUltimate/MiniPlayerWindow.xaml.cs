using System;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Threading;
using System.Windows.Media.Imaging;

namespace CustomMediaPlayerUltimate;

public partial class MiniPlayerWindow : Window
{
    public static MiniPlayerWindow? Instance;

    private Timer titleTimer = new Timer(10);
    private Timer artistTimer = new Timer(10);
    private Timer opacityTimer = new Timer(10);

    public MiniPlayerWindow()
    {
        InitializeComponent();

        Instance = this;
        if (Properties.Settings.Default.MiniplayerLastLocationX != -1)
        {
            Left = Properties.Settings.Default.MiniplayerLastLocationX;
        }
        if (Properties.Settings.Default.MiniplayerLastLocationY != -1)
        {
            Top = Properties.Settings.Default.MiniplayerLastLocationY;
        }

        titleTimer.Elapsed += (sender, e) =>
        {
            Dispatcher.Invoke(() =>
            {
                if (titleTimer.Interval == 1500)
                {
                    SongTitleLabelContainer.ScrollToHorizontalOffset(0f);
                }
                if (SongTitleLabelContainer.HorizontalOffset == SongTitleLabelContainer.ScrollableWidth)
                {
                    titleTimer.Interval = 1500;
                }
                else
                {
                    SongTitleLabelContainer.ScrollToHorizontalOffset(SongTitleLabelContainer.HorizontalOffset + 0.1);
                    titleTimer.Interval = 10;
                }
            });
        };
        artistTimer.Elapsed += (sender, e) =>
        {
            Dispatcher.Invoke(() =>
            {
                if (artistTimer.Interval == 1500)
                {
                    SongArtistLabelContainer.ScrollToHorizontalOffset(0f);
                }
                if (SongArtistLabelContainer.HorizontalOffset == SongArtistLabelContainer.ScrollableWidth)
                {
                    artistTimer.Interval = 1500;
                }
                else
                {
                    SongArtistLabelContainer.ScrollToHorizontalOffset(SongArtistLabelContainer.HorizontalOffset + 0.1);
                    artistTimer.Interval = 10;
                }
            });
        };
        opacityTimer.Elapsed += (sender, e) =>
        {
            if (opacityTimer.Interval == Properties.Settings.Default.MiniplayerFadingTimeout)
            {
                opacityTimer.Interval = 10;
            }
            Dispatcher.Invoke(() =>
            {
                if (Opacity > Properties.Settings.Default.MiniplayerMinimumOpacity)
                {
                    Opacity -= 0.01f;
                }
                else
                {
                    opacityTimer.Stop();
                }
            });
        };
    }

    private void CloseButtonClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void WindowGrabberMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        DragMove();
    }

    private void WindowGrabberMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState = WindowState.Normal;
        }
    }

    private void WindowClosing(object sender, CancelEventArgs e)
    {
        MainWindow.Instance.BackFromMiniplayer();
        Instance = null;
    }

    private void WindowMouseEnter(object sender, RoutedEventArgs e)
    {
        opacityTimer.Stop();
        Opacity = 1f;
    }

    private void WindowMouseLeave(object sender, RoutedEventArgs e)
    {
        if (!Properties.Settings.Default.MiniplayerAutoOpacity) return;
        opacityTimer.Interval = Properties.Settings.Default.MiniplayerFadingTimeout;
        opacityTimer.Start();
    }

    private void WindowLocationChanged(object sender, EventArgs e)
    {
        Properties.Settings.Default.MiniplayerLastLocationX = (int)Left;
        Properties.Settings.Default.MiniplayerLastLocationY = (int)Top;
    }

    private void WindowLoaded(object sender, RoutedEventArgs e)
    {
        opacityTimer.Interval = Properties.Settings.Default.MiniplayerFadingTimeout;
        opacityTimer.Start();
    }

    private void PlayPauseButtonClick(object sender, RoutedEventArgs e)
    {
        MainWindow.Instance.TogglePlayPause();
    }

    private void NextSongButtonClick(object sender, RoutedEventArgs e)
    {
        MainWindow.Instance.NextSongInPlaylist();
    }

    public void SetTitleText(string title)
    {
        titleTimer.Stop();
        SongTitleLabel.Content = title;
        SongTitleLabel.UpdateLayout();
        bool overflow = SongTitleLabel.ActualWidth > SongTitleLabelContainer.ActualWidth;
        SongTitleLabelOverflowShadow.Opacity = overflow ? 1f : 0f;

        Dispatcher.Invoke(() =>
        {
            SongTitleLabelContainer.ScrollToHorizontalOffset(0f);
        });
        titleTimer.Start();
    }

    public void SetArtistText(string artist)
    {
        artistTimer.Stop();
        SongArtistLabel.Content = artist;
        SongArtistLabel.UpdateLayout();
        bool overflow = SongArtistLabel.ActualWidth > SongArtistLabelContainer.ActualWidth;
        SongArtistLabelOverflowShadow.Opacity = overflow ? 1f : 0f;

        Dispatcher.Invoke(() =>
        {
            SongArtistLabelContainer.ScrollToHorizontalOffset(0f);
        });
        artistTimer.Start();
    }

    public void SetPlayPauseImage(bool playing)
    {
        BitmapImage image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri(playing ? "/Images/pause.png" : "/Images/play.png", UriKind.Relative);
        image.EndInit();

        PlayPauseButtonImage.Source = image;
    }
}
