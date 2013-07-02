using System.Collections.ObjectModel;

namespace Feeder.Common.Timber
{
    public class Logger
    {
        private static Logger _instance;

        private readonly ObservableCollection<LogEntry> _logger;

        public bool IsDebugEnabled { get; set; }

        public ObservableCollection<LogEntry> GetLogger()
        {
            return _logger;
        }

        public static Logger GetInstance()
        {
            return _instance ?? (_instance = new Logger());
        }

        private Logger()
        {
            _logger = new ObservableCollection<LogEntry>();
        }
         
    }
}