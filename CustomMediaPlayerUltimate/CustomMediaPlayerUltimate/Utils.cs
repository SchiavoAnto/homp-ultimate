using System;
using System.Collections.Generic;
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
                    info.Add("Title", prop?.ValueAsObject!.ToString()!);
                }
                catch { }
                try
                {
                    prop = shell.Properties!.System!.Music.Artist;
                    info.Add("Artist", string.Join(", ", (string[])prop?.ValueAsObject!));
                }
                catch { }
                try
                {
                    prop = shell.Properties!.System!.Music.AlbumTitle;
                    info.Add("Album", prop?.ValueAsObject!.ToString()!);
                }
                catch { }
                try
                {
                    prop = shell.Properties!.System!.Media.Year;
                    info.Add("Year", prop?.ValueAsObject!.ToString()!);
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
}
