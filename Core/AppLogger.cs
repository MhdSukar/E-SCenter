using System;

namespace ESCenter.Core
{
    public static class AppLogger
    {
        public static event Action<string, StatusLevel> StatusRaised;

        public static void Info(string msg) => Raise(msg, StatusLevel.Info);
        public static void Success(string msg) => Raise(msg, StatusLevel.Success);
        public static void Warning(string msg) => Raise(msg, StatusLevel.Warning);
        public static void Error(string msg) => Raise(msg, StatusLevel.Error);

        private static void Raise(string msg, StatusLevel level)
        {
            StatusRaised?.Invoke(msg, level);
        }
    }
}
