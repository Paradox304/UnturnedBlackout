using Rocket.Core.Logging;
using System;
using System.Diagnostics;

namespace UnturnedBlackout
{
    public static class Logging
    {
        public static void Debug(string message)
        {
            if (Plugin.Instance.Config.Base.FileData.EnableDebugLogs == true)
            {
                var method = new StackTrace().GetFrame(1).GetMethod();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[{method.ReflectedType}.{method.Name}]: {message}");
                Console.ResetColor();
                Logger.ExternalLog(message, ConsoleColor.Green);
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
