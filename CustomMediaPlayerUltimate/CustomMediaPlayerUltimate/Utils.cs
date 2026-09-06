using System;
using System.Linq;
using TagLib.Id3v2;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using CustomMediaPlayerUltimate.DataStructures;

namespace CustomMediaPlayerUltimate;

internal class Utils
{
    public static (bool Success, Exception? Exception) GetMediaInformation(Song song)
    {
        try
        {
            using (TagLib.File file = TagLib.File.Create(song.FilePath))
            {
                song.Title = file.Tag.Title ?? song.FilePath;
                song.Artists = file.Tag.Performers ?? ["Unknown Artist"];
                song.Artist = string.Join(", ", song.Artists);
                song.Album = file.Tag.Album;
                song.AlbumArtists = file.Tag.AlbumArtists ?? ["Unknown Artist"];
                song.AlbumArtist = string.Join(", ", song.AlbumArtists);
                song.Year = (int)file.Tag.Year;
                song.TrackNumber = (int)file.Tag.Track;
                song.Genres = file.Tag.Genres;
                song.Duration = file.Properties.Duration;

                IEnumerable<PopularimeterFrame>? popms =
                    ((Tag)file.GetTag(TagLib.TagTypes.Id3v2)).GetFrames<PopularimeterFrame>();
                if (popms is not null)
                {
                    if (popms.Count() > 0)
                        song.Rating = popms.Max(popm => popm.Rating);
                }
            }
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex);
        }
    }

    public static BitmapImage ConstructImageFromPath(string path, UriKind mode = UriKind.Relative)
    {
        BitmapImage image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri(path, mode);
        image.EndInit();

        return image;
    }

    public static string Pluralize(int number, string singular, string plural)
    {
        return $"{number} {(number == 1 ? singular : plural)}";
    }

    public static Point GetRelativePosition(Control control)
    {
        UIElement? container = VisualTreeHelper.GetParent(control) as UIElement;
        if (container is null) return new Point(0f, 0f);
        Point relativeLocation = control.TranslatePoint(new Point(0, 0), container);
        return relativeLocation;
    }

    public static void SetTabItemPosition(Rectangle rect, Control control)
    {
        Point loc = GetRelativePosition(control);
        rect.SetValue(Canvas.LeftProperty, loc.X + 10);
    }

    public static void AnimateTabPosition(Control target, Rectangle indicator, DoubleAnimation animation)
    {
        Point position = GetRelativePosition(target);
        animation.From = (double)indicator.GetValue(Canvas.LeftProperty);
        animation.To = position.X + 10;
        indicator.BeginAnimation(Canvas.LeftProperty, animation);
    }

    public static void AnimateTabSize(Control target, Rectangle indicator, ScaleTransform transform, DoubleAnimation animation)
    {
        animation.From = transform.ScaleX;
        animation.To = target.ActualWidth;
        transform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
    }
}
