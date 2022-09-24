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
                System.Reflection.MethodBase method = new StackTrace().GetFrame(1).GetMethod();
                Console.ForegroundColor = color;
                Console.WriteLine($"[{DateTime.UtcNow:dd/MM/yyyy hh:mm:ss}] [{method.ReflectedType}.{method.Name}]: {message}");
                Logger.ExternalLog($"[{method.ReflectedType}.{method.Name}]: {message}", color);
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
