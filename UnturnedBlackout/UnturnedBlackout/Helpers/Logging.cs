using System;

namespace UnturnedBlackout.Helpers;

public static class Logging
{
    public static void Debug(string message, ConsoleColor color = ConsoleColor.Green)
    {
        var updatedMesage = $"[{DateTime.UtcNow:dd/MM/yyyy hh:mm:ss}]: {message}";
        if (Plugin.Instance.Config.Base.FileData.EnableDebugLogs)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(updatedMesage);
            Console.ResetColor();
        }

        Plugin.Instance.Logger.Log(updatedMesage);
    }
}