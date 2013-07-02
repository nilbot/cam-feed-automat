using System;
using System.ComponentModel;
using Feeder.Common.Helpers;


namespace Feeder.Common.Timber
{
    public static class LoggerExtension
    {
        public static void AddEntry(this Logger logger, string sender, string message)
        {
            logger.GetLogger().Add(new LogEntry{DateTime = DateTime.UtcNow, Module = sender, Message = message});
        }

        public static void Log(this DebugMode m, string s, params object[] objs)
        {
            var _result = m.Description();
            if (_result == null)
                throw new ArgumentNullException("m", @"mode is not defined.");
            //return _result + s;
            var _str = String.Format(s, objs);
            Logger.GetInstance().AddEntry(_result, _str);
        }
    }

    public enum DebugMode
    {
        [Description("[DEBUG] ")]
        Debug,
        [Description("[INFO] ")]
        Info,
        [Description("[ERROR] ")]
        Error,
    }
}