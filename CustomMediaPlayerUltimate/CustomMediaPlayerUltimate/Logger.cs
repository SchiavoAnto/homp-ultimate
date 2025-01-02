using System;
using System.IO;
using System.Diagnostics;

namespace CustomMediaPlayerUltimate;

public static class Logger
{
    private static readonly string padString = "".PadLeft(37);

    public static void Init()
    {
        try
        {
            File.Delete($"{AppDomain.CurrentDomain.BaseDirectory}homp.log");
        }
        catch { }
        Trace.Listeners.Add(new TextWriterTraceListener($"{AppDomain.CurrentDomain.BaseDirectory}homp.log"));
        Trace.IndentSize = 4;
        Trace.AutoFlush = true;
        Trace.UseGlobalLock = true;
    }

    public static void Log(string message)
    {
        Trace.WriteLine($"[{DateTime.Now:R}][LOG] {message}");
    }

    public static void Warn(string message)
    {
        Trace.WriteLine($"[{DateTime.Now:R}][WRN] {message}");
    }

    public static void Error(string message)
    {
        Trace.WriteLine($"[{DateTime.Now:R}][ERR] {message}");
    }

    public static void Exception(string message, Exception ex)
    {
        Trace.WriteLine($"[{DateTime.Now:R}][EXC] {message}");
        Trace.WriteLine(ex.Message.Insert(0, padString));
        Trace.Indent();
        string[] exceptionLines = ex.StackTrace?.Split('\n') ?? ["!! NO STACK TRACE !!"];
        for (int i = 0; i < exceptionLines.Length; i++)
        {
            Trace.WriteLine(exceptionLines[i].Insert(0, padString));
        }
        Trace.Unindent();
    }
}
