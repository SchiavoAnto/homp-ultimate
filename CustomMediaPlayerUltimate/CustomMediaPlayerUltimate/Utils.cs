using System;
using System.Windows.Media.Imaging;
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
}
