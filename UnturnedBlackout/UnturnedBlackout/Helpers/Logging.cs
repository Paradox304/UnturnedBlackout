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
                Logger.Log($"[{method.ReflectedType}.{method.Name}]: {message}");
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
