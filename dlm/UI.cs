using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dlm
{
    internal sealed class UI
    {
        private static volatile UI _instance;
        private static object _syncRoot = new Object();

        public int ActiveDownloadsCount { get; set; }
        private long _totalDownloadedBytesCount;
        private long _totalBytesCount;
        private DateTime _downloadsBeginTime { get; set; }
        private string _currentActivity;
        private Queue<string> _lastLogEntries = new Queue<string>();

        private Dictionary<string, FileInfo> _activeDownloads = new Dictionary<string, FileInfo>();
        private System.Timers.Timer _timer;


        private DateTime _lastKeepAliveTime;

        public static UI Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_syncRoot) //https://msdn.microsoft.com/en-us/library/ff650316.aspx
                    {
                        if (_instance == null)
                            _instance = new UI();
                    }
                }
                return _instance;
            }
        }

        private UI()
        {
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();

        }

        void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.UpdateUI();
        }

        public void SetTotalBytesToReceive(IList<FileInfo> files)
        {
            this._totalBytesCount = files.Select(f => f.Length).Sum();
        }

        public void DownloadProgress(FileInfo file)
        {
            if (_activeDownloads.Count == 0)
                _downloadsBeginTime = DateTime.Now;

            lock (_syncRoot)
            {
                if (!_activeDownloads.ContainsKey(file.Name))
                {
                    _activeDownloads.Add(file.Name, file);
                }
            }

            _totalDownloadedBytesCount = _activeDownloads.Select(kvp => kvp.Value.DownloadedBytes).Sum();
        }

        public void SetCurrentActivity(string messageFormat, params object[] args)
        {
            _currentActivity = string.Format(messageFormat, args);
        }

        public void SetLogEntry(string logEntry)
        {
            _lastLogEntries.Enqueue(logEntry);
            if (_lastLogEntries.Count > 5)
                _lastLogEntries.Dequeue();
        }

        private void UpdateUI()
        {
            _lastKeepAliveTime = DateTime.Now;
            int maxRowLength = Console.BufferWidth;

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("************************************************************************");
            sb.AppendLine("dlm 1.0");
            sb.AppendLine("************************************************************************");
            sb.AppendLine();

            //environment
            sb.AppendLine("Environment:");
            sb.AppendLine(Truncate("- Machine name: " + Settings.MachineName, maxRowLength));
            sb.AppendLine(Truncate("- Local path: " + Settings.LocalPath, maxRowLength));
            if (Settings.CanUseRemotePath)
                sb.AppendLine(Truncate("- Remote path: " + Settings.RemotePath, maxRowLength));
            else
                sb.AppendLine(Truncate("- Remote path not in use", maxRowLength));
            sb.AppendLine();


            //last log lines
            sb.AppendLine("Last log entries:");
            if (_lastLogEntries.Count > 0)
            {
                foreach (var logEntry in _lastLogEntries)
                {
                    sb.AppendLine(Truncate("- " + logEntry, maxRowLength));
                }
            }
            else
            {
                sb.AppendLine("- No entry to show");
            }
            sb.AppendLine();

            //active downloads
            sb.AppendLine("Active downloads:");
            if (_activeDownloads.Count > 0)
            {
                foreach (var activeDownload in _activeDownloads)
                {
                    //if (activeDownload.Value.DownloadPercentage < 100)
                    //{
                    //speed
                    var secondsElapsed = DateTime.Now.Subtract(activeDownload.Value.DownloadBeginTime).TotalSeconds;
                    var bytesPerSecond = activeDownload.Value.DownloadedBytes / secondsElapsed;

                    //eta
                    var remainingBytes = activeDownload.Value.Length - activeDownload.Value.DownloadedBytes;
                    var remainingSeconds = remainingBytes / bytesPerSecond;

                    sb.AppendLine(Truncate(string.Format("{0:###}% {1:######} kbps, {2:#0}:{3:##} remaining: {4}",
                        activeDownload.Value.DownloadPercentage,
                        bytesPerSecond / 1024,
                        remainingSeconds / 60,
                        remainingSeconds % 60,
                        activeDownload.Value.Name), maxRowLength));
                    //}
                }
            }
            else
            {
                sb.AppendLine("- No active downloads");
            }
            sb.AppendLine();

            //overall performances
            sb.AppendLine("Overall performance:");
            if (_downloadsBeginTime > DateTime.MinValue
                && _totalBytesCount > 0)
            {
                var MBreceived = _totalDownloadedBytesCount / 1024 / 1024;
                var MBremaining = (_totalBytesCount - _totalDownloadedBytesCount) / 1024 / 1024;
                var totalPercentage = 100 * _totalDownloadedBytesCount / _totalBytesCount;
                var totalSecondsElapsed = DateTime.Now.Subtract(_downloadsBeginTime).TotalSeconds;
                var totalBytesPerSecond = _totalDownloadedBytesCount / totalSecondsElapsed;
                var totalRemainingSeconds = MBremaining / totalBytesPerSecond;
                sb.AppendLine(string.Format("{0} MB received, {1} MB remaining, {2}% completed at {3} MBps",
                    MBreceived, MBremaining, totalPercentage, totalBytesPerSecond / 1024 / 1024));
            }
            else
            {
                sb.AppendLine("- No data available");
            }
            sb.AppendLine();


            //current activity
            sb.AppendLine("Status: " + _currentActivity);

            Console.Clear();
            Console.Write(sb.ToString());
        }

        public bool IsAlive
        {
            get
            {
                return DateTime.Now.Subtract(this._lastKeepAliveTime).TotalSeconds < 2000;
            }
        }

        private string Truncate(string s, int maxLenght)
        {
            if (s.Length > maxLenght)
                return s.Substring(0, maxLenght - 5) + "...";
            else
                return s;
        }
    }
}
