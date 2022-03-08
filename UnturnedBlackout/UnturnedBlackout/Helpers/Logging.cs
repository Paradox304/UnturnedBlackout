﻿using Rocket.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout
{
    public static class Logging
    {
        public static void Debug(string message)
        {
            if (Plugin.Instance.Configuration.Instance.EnableDebugLogs == true)
            {
                Logger.Log($"[DEBUG] {message}");
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