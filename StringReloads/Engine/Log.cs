﻿using StringReloads;
using StringReloads.Engine;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

public static class Log
{
    public static void Trace(string Line, string[] Format = null, [CallerMemberName] string CallerMethod = null, [CallerLineNumber] int CallerLine = 0, [CallerFilePath] string CallerFile = null)
    {
        WriteLine(Line, LogLevel.Trace, Format, CallerMethod, CallerLine, CallerFile);
    }

    public static void Debug(string Line, string[] Format = null, [CallerMemberName] string CallerMethod = null, [CallerLineNumber] int CallerLine = 0, [CallerFilePath] string CallerFile = null)
    {
        WriteLine(Line, LogLevel.Debug, Format, CallerMethod, CallerLine, CallerFile);
    }
    public static void Information(string Line, params string[] Format)
    {
        if (!Config.Default.Log)
            return;

        var Color = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        WriteLine(Line, LogLevel.Information, Format, null, 0, null, ConsoleColor.Green);
        Console.ForegroundColor = Color;
    }
    public static void Warning(string Line, string[] Format = null, [CallerMemberName] string CallerMethod = null, [CallerLineNumber] int CallerLine = 0, [CallerFilePath] string CallerFile = null)
    {
        if (!Config.Default.Log)
            return;

        var Color = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        WriteLine(Line, LogLevel.Warning, Format, CallerMethod, CallerLine, CallerFile, ConsoleColor.Yellow);
        Console.ForegroundColor = Color;
    }

    public static void Error(string Line, string[] Format = null, [CallerMemberName] string CallerMethod = null, [CallerLineNumber] int CallerLine = 0, [CallerFilePath] string CallerFile = null)
    {
        if (!Config.Default.Log)
            return;

        var Color = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkRed;
        WriteLine(Line, LogLevel.Error, Format, CallerMethod, CallerLine, CallerFile, ConsoleColor.DarkRed);
        Console.ForegroundColor = Color;
    }

    public static void Critical(string Line, string[] Format = null, [CallerMemberName] string CallerMethod = null, [CallerLineNumber] int CallerLine = 0, [CallerFilePath] string CallerFile = null)
    {
        if (!Config.Default.Log)
            return;

        var Color = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Magenta;
        WriteLine(Line, LogLevel.Critical, Format, CallerMethod, CallerLine, CallerFile, ConsoleColor.Magenta);
        Console.ForegroundColor = Color;
        Environment.Exit(1);
    }

    static unsafe void WriteLine(string Line, LogLevel Level, string[] Format, string CallerMethod, int CallerLine, string CallerFile, ConsoleColor? InitialForeColor = null)
    {
        if (!Config.Default.Log)
            return;

        if (Level < Config.Default.LogLevel)
            return;

        if (hConsole == null && Config.Default.LogLevel < LogLevel.Information)
        {
            if (Config.Default.Debug || Config.Default.MainWindow != null)
            {
                AllocConsole();
                Console.OutputEncoding = Encoding.Unicode;
                hConsole = GetConsoleWindow();
                ShowWindow(hConsole, SW_SHOW);

                if (InitialForeColor != null)
                    Console.ForegroundColor = InitialForeColor.Value;
            }
        }

        string RstLine = Format == null ? Line : string.Format(Line, Format);
        if (Level < LogLevel.Warning)
           RstLine = $"[{GetLogLevelName(Level)}] {RstLine}";
        else
           RstLine = $"[{GetLogLevelName(Level)}] [{Path.GetFileName(CallerFile)}:{CallerLine}({CallerMethod})]\n{RstLine}";

        if (Config.Default.LogFile) {
            WriteFile(RstLine);
        }

        Console.WriteLine(RstLine);
    }

    static void WriteFile(string Line)
    {
        if (LogStream == null)
        {
            LogStream = File.CreateText(Path.Combine(EntryPoint.ApplicationDirectory, "SRL.log"));
        }
        LogStream.WriteLine(Line);
        LogStream.Flush();
    }

    static TextWriter LogStream = null;

    const uint SW_SHOW = 5;

    static unsafe void* hConsole = null;

    [DllImport("kernel32.dll")]
    static unsafe extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    static unsafe extern void* GetConsoleWindow();

    [DllImport("user32.dll")]
    static unsafe extern bool ShowWindow(void* hWnd, uint nCmdShow);

    static string GetLogLevelName(LogLevel Level) => Level switch
    {
        LogLevel.Trace => "TRC",
        LogLevel.Debug => "DBG",
        LogLevel.Information => "INF",
        LogLevel.Warning => "WAR",
        LogLevel.Error => "ERR",
        LogLevel.Critical => "CRI",
        _ => throw new Exception("Invalid Log Level")
    };

    public enum LogLevel : int { 
        Trace = 0,
        Debug = 1,
        Information = 2,
        Warning = 3,
        Error = 4,
        Critical = 5
    }
}