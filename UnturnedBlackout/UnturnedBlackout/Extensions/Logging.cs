using System;
using Rocket.Core.Logging;

namespace UnturnedBlackout.Extensions;

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

        Logger.ExternalLog(message, color);
        Plugin.Instance.Logger.Log(updatedMesage);
    }
}