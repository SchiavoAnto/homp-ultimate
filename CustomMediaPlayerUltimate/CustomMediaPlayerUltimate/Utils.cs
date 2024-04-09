using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;

namespace CustomMediaPlayerUltimate;

internal class Utils
{
    public static Dictionary<string, string> GetMediaInformation(string filename)
    {
        Dictionary<string, string> info = new();
        try
        {
            using (ShellObject shell = ShellObject.FromParsingName(filename)!)
            {
                IShellProperty prop;
                try
                {
                    prop = shell.Properties!.System!.Title!;
                    info.Add("Title", prop?.ValueAsObject?.ToString() ?? "Unknown Title");
                }
                catch { }
                try
                {
                    prop = shell.Properties!.System!.Music.Artist;
                    info.Add("Artist", string.Join(", ", (string[])prop?.ValueAsObject!) ?? "Unknown Artist");
                }
                catch { }
                try
                {
                    prop = shell.Properties!.System!.Music.AlbumTitle;
                    info.Add("Album", prop?.ValueAsObject?.ToString() ?? "Unknown Album");
                }
                catch { }
                try
                {
                    prop = shell.Properties!.System!.Media.Year;
                    info.Add("Year", prop?.ValueAsObject?.ToString() ?? "");
                } catch { }
                try
                {
                    prop = shell.Properties!.System!.Media.Duration!;
                    TimeSpan span = TimeSpan.FromMicroseconds((ulong)prop?.ValueAsObject! / 10);
                    info.Add("Duration", span.ToString("m':'ss"));
                }
                catch { }
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
