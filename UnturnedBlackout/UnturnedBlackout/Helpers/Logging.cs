using Rocket.Core.Logging;
using System;
using System.Diagnostics;

namespace UnturnedBlackout
{
    public static class Logging
    {
        public static void Debug(string message, ConsoleColor color = ConsoleColor.Green)
        {
            if (Plugin.Instance.Config.Base.FileData.EnableDebugLogs == true)
            {
                var method = new StackTrace().GetFrame(1).GetMethod();
                Console.ForegroundColor = color;
                var m = $"[{DateTime.UtcNow:dd/MM/yyyy hh:mm:ss}] [{method.ReflectedType}.{method.Name}]: {message}";
                Console.WriteLine(m);
                Logger.ExternalLog(m, color);
                Console.ResetColor();
            }
        }

        public static void Write(object source, object message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{source}]: {message}");
            Logger.ExternalLog(message, ConsoleColor.Red);
            Console.ResetColor();
        }
    }
}
