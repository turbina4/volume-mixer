//#define ENABLE_LOGS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace volume_mixer
{
    internal class Logger
    {
        public static void logInfo(string message, params string[] args)
        {
        #if ENABLE_LOGS
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("-> ");
            Console.Write(message);
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write(string.Join(", ", args));
            Console.WriteLine();
        #endif
        }

        public static void logInit(string message)
        {
        #if ENABLE_LOGS
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"--> INIT DONE: {message}");
        #endif
        }

        public static void logError(string message)
        {
        #if ENABLE_LOGS
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"---> ERR: {message}");
        #endif
        }

        public static void logSpecial(string message)
        {
        #if ENABLE_LOGS
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(message);
        #endif
        }
    }
}
