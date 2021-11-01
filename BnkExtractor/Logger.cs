using System;

namespace BnkExtractor
{
    public static class Logger
    {
        public static event Action<string> Info;
        public static event Action<string> Warning;
        public static event Action<string> Error;
        public static event Action<string> Fatal;
        public static event Action<string> Verbose;
        public static event Action<string> Debug;

        static Logger()
        {
            Info += s => Console.WriteLine($"Info: {s}");
            Warning += s => Console.WriteLine($"Warning: {s}");
            Error += s => Console.WriteLine($"Error: {s}");
            Fatal += s => Console.WriteLine($"Fatal: {s}");
            Verbose += s => Console.WriteLine($"Verbose: {s}");
#if DEBUG
            Debug += s => Console.WriteLine($"Debug: {s}");
#endif
        }

        internal static void LogInfo(string message) => Info?.Invoke(message);
        internal static void LogWarning(string message) => Warning?.Invoke(message);
        internal static void LogError(string message) => Error?.Invoke(message);
        internal static void LogFatal(string message) => Fatal?.Invoke(message);
        internal static void LogVerbose(string message) => Verbose?.Invoke(message);
        internal static void LogDebug(string message) => Debug?.Invoke(message);

        public static void ClearLoggingEvents()
        {
            Info = null;
            Warning = null;
            Error = null;
            Fatal = null;
            Verbose = null;
            Debug = null;
        }
    }
}
