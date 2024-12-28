using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace CustomMediaPlayerUltimate;

internal class Utils
{
    public static Dictionary<string, string> GetMediaInformation(string filename)
    {
        Dictionary<string, string> info = new();
        if (!File.Exists(filename)) return info;
        try
        {
            using (TagLib.File file = TagLib.File.Create(filename))
            {
                info.Add("Title", file.Tag.Title ?? filename);
                info.Add("Artist", string.Join(", ", file.Tag.Performers ?? ["Unknown Artist"]));
                info.Add("Album", file.Tag.Album ?? "Unknown Album");
                info.Add("Year", file.Tag.Year.ToString());
                info.Add("Duration", file.Properties.Duration.ToString("m':'ss"));
            }
        }
        catch { }
        return info;
    }

    public static BitmapImage ConstructImageFromPath(string path, UriKind mode = UriKind.Relative)
    {
        BitmapImage image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri(path, mode);
        image.EndInit();

        return image;
    }
}
