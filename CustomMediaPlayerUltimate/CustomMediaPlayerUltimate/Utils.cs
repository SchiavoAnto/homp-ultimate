using System;
using System.IO;
using System.Windows.Media.Imaging;
using CustomMediaPlayerUltimate.DataStructures;

namespace CustomMediaPlayerUltimate;

internal class Utils
{
    public static bool GetMediaInformation(Song song)
    {
        if (!File.Exists(song.FilePath)) return false;
        try
        {
            using (TagLib.File file = TagLib.File.Create(song.FilePath))
            {
                song.Title = file.Tag.Title ?? song.FilePath;
                song.Artist = string.Join(", ", file.Tag.Performers ?? ["Unknown Artist"]);
                song.Album = file.Tag.Album;
                song.Year = file.Tag.Year.ToString();
                song.Duration = file.Properties.Duration;
            }
            return true;
        }
        catch
        {
            return false;
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
}
