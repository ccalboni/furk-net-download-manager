using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace dlm
{
    public static class Settings
    {
        public static readonly string FurkApiEndpoint = "https://www.furk.net/api";

        public static string ProgramBasePath;
        public static string FurkApiKey;
        public static string PushbulletApiKey;
        public static string PushbulletDeviceName;
        public static List<string> IncludeTorrentsWithKeywords;
        public static List<string> ExcludeTorrentsWithKeywords;
        public static List<string> IncludeFilesWithKeywords;
        public static List<string> ExcludeFilesWithKeywords;
        public static int MaxFileSize;
        public static int MaxTorrentSize;
        public static int MaxConcurrentDownloads;
        public static string LocalPath;
        public static string RemotePath;
        public static bool CanUseRemotePath;
        public static string MachineName;
        public static string LogFilePath;
        public static bool MustSimulateDownloads;
        public static List<string> DownloadsHistory = new List<string>();
        internal static Logger.Level LogLevel;

        private static INIFile IniManager;
        private static readonly string IniFileName = "dlm.ini";
        private static readonly string LogFileName = "dlm_log.txt";
        private static readonly string[] SplitCharacters = new string[] { @" ", ",", @"\", ";", ".", ":" };
        private static readonly string DefaultDownloadsFolderName = Path.Combine("Downloads", "Furk.net");


        internal static class IniKeys
        {
            internal static readonly string IncludeTorrentsWithKeywords = "IncludeTorrentsWithKeywords";
            internal static readonly string ExcludeTorrentsWithKeywords = "ExcludeTorrentsWithKeywords";
            internal static readonly string IncludeFilesWithKeywords = "IncludeFilesWithKeywords";
            internal static readonly string ExcludeFilesWithKeywords = "ExcludeFilesWithKeywords";
            internal static readonly string MaxFileSize = "MaxFileSize";
            internal static readonly string MaxTorrentSize = "MaxTorrentSize";
            internal static readonly string MaxConcurrentDownloads = "MaxConcurrentDownloads";
            internal static readonly string FurkApiKey = "FurkApiKey";
            internal static readonly string FurkApiEndpoint = "FurkApiEndpoint";
            internal static readonly string PushbulletApiKey = "PushbulletApiKey";
            internal static readonly string PushbulletDeviceName = "PushbulletDeviceName";
            internal static readonly string LocalPath = "LocalPath";
            internal static readonly string RemotePath = "RemotePath";
            internal static readonly string HistoryItemsCount = "HistoryItemsCount";
            internal static readonly string HistoryItemPrefix = "HistoryItem_";
            internal static readonly string LogLevel = "LogLevel";
        }

        internal static class IniSections
        {
            internal static readonly string Options = "Options";
            internal static readonly string ApplicationData = "ApplicationData";
            internal static readonly string ComputerNamePrefix = "ComputerName:";
        }

        /// <summary>
        /// Initialize environment, loads settings from INI file
        /// </summary>
        public static bool Init()
        {
            //where are we running?
            Settings.ProgramBasePath = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));

            //path where log file is saved, inside program's folder, can't be specified by user
            Settings.LogFilePath = Path.Combine(Settings.ProgramBasePath, LogFileName);

            //if INI doesn't exists, create a new empty text file
            var iniFilePath = Path.Combine(Settings.ProgramBasePath, IniFileName);
            if (!File.Exists(iniFilePath))
            {
                File.AppendAllText(iniFilePath, string.Empty);
            }

            //set the object that will do IO to the INI file
            IniManager = new INIFile((iniFilePath), true, true);

            //log level: if not specified, set on warning and above
            var logLevel = IniManager.GetValue(IniSections.Options, IniKeys.LogLevel, "war").ToLowerInvariant().Substring(0, 3);
            switch (logLevel)
            {
                case "trc":
                case "tra":
                    Settings.LogLevel = Logger.Level.Trace;
                    break;
                case "inf":
                    Settings.LogLevel = Logger.Level.Information;
                    break;
                case "err":
                    Settings.LogLevel = Logger.Level.Error;
                    break;
                case "deb":
                case "dbg":
                    Settings.LogLevel = Logger.Level.Debug;
                    break;
                case "war":
                case "wrn":
                default:
                    Settings.LogLevel = Logger.Level.Warning;
                    break;
            }

#if DEBUG
            //setting log level to trace during development
            Settings.LogLevel = Logger.Level.Trace;
#endif

            //the furk api key is mandatory, otherwise we can't connect to the seedbox
            Settings.CreateIniKeyIfEmpty(IniSections.Options, IniKeys.FurkApiKey);

            Settings.FurkApiKey = IniManager.GetValue(IniSections.Options, IniKeys.FurkApiKey, string.Empty);
            if (string.IsNullOrWhiteSpace(Settings.FurkApiKey))
            {
                Logger.Error("Furk API key is missing in configuration file. It must be specified in [Options] section, FurkApiKey=your_api_key. Program will now shut down.");
                return false;
            }
            Logger.Trace("Loaded setting: {0}.{1}={2}", IniSections.Options, IniKeys.FurkApiKey, Settings.FurkApiKey);

            //from here on, every parameter is optional; we add the keys to the file without any specified value

            //when the following keywords are found within a torrent name, torrent is processed (when empty doesn't have any effect)
            Settings.CreateIniKeyIfEmpty(IniSections.Options, IniKeys.IncludeTorrentsWithKeywords);
            Settings.IncludeTorrentsWithKeywords = Settings.ConvertIniValueToList(IniSections.Options, IniKeys.IncludeTorrentsWithKeywords);

            //when the following keywords are found within a torrent name, torrent is excluded (when empty doesn't have any effect)
            Settings.CreateIniKeyIfEmpty(IniSections.Options, IniKeys.ExcludeTorrentsWithKeywords);
            Settings.ExcludeTorrentsWithKeywords = Settings.ConvertIniValueToList(IniSections.Options, IniKeys.ExcludeTorrentsWithKeywords);

            //when the following keywords are found within a file name, file is processed (when empty doesn't have any effect)
            Settings.CreateIniKeyIfEmpty(IniSections.Options, IniKeys.IncludeFilesWithKeywords);
            Settings.IncludeFilesWithKeywords = Settings.ConvertIniValueToList(IniSections.Options, IniKeys.IncludeFilesWithKeywords);

            //when the following keywords are found within a file name, file is excluded (when empty doesn't have any effect)
            Settings.CreateIniKeyIfEmpty(IniSections.Options, IniKeys.ExcludeFilesWithKeywords);
            Settings.ExcludeFilesWithKeywords = Settings.ConvertIniValueToList(IniSections.Options, IniKeys.ExcludeFilesWithKeywords);

            //how many downloads must run at the same time? Default is 1
            Settings.CreateIniKeyIfEmpty(IniSections.Options, IniKeys.MaxConcurrentDownloads);
            Settings.MaxConcurrentDownloads = IniManager.GetValue(IniSections.Options, IniKeys.MaxConcurrentDownloads, 1);

            //what's the maximum size for a torrent to be processed?
            Settings.CreateIniKeyIfEmpty(IniSections.Options, IniKeys.MaxTorrentSize);
            Settings.MaxTorrentSize = IniManager.GetValue(IniSections.Options, IniKeys.MaxTorrentSize, -1);

            //what's the maximum size for a file to be processed?
            Settings.CreateIniKeyIfEmpty(IniSections.Options, IniKeys.MaxFileSize);
            Settings.MaxFileSize = IniManager.GetValue(IniSections.Options, IniKeys.MaxFileSize, -1);

            //program can notify via PushBullet; in this case, an API key is required
            Settings.CreateIniKeyIfEmpty(IniSections.Options, IniKeys.PushbulletApiKey);
            Settings.PushbulletApiKey = IniManager.GetValue(IniSections.Options, IniKeys.PushbulletApiKey, string.Empty);

            //when PushBullet is enabled, messages can be sent on only one device instead of every registered device
            Settings.CreateIniKeyIfEmpty(IniSections.Options, IniKeys.PushbulletDeviceName);
            Settings.PushbulletDeviceName = IniManager.GetValue(IniSections.Options, IniKeys.PushbulletDeviceName, string.Empty);


            //machine specific options
            Settings.MachineName = Environment.MachineName;
            var computerSpecificSection = string.Format("{0}{1}", IniSections.ComputerNamePrefix, Settings.MachineName);

            //machine specific temporary ad final path
            Settings.CreateIniKeyIfEmpty(computerSpecificSection, IniKeys.LocalPath);
            var userLocalPath = IniManager.GetValue(computerSpecificSection, IniKeys.LocalPath, string.Empty);
            Settings.CreateIniKeyIfEmpty(computerSpecificSection, IniKeys.RemotePath);
            var userRemotePath = IniManager.GetValue(computerSpecificSection, IniKeys.RemotePath, string.Empty);

            //default local path
            var defaultLocalPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Settings.DefaultDownloadsFolderName);

            var tryUseDefaultLocalPath = false;
            //user specified a local path
            if (!string.IsNullOrWhiteSpace(userLocalPath))
            {
                //check if that path exists
                if (Directory.Exists(userLocalPath))
                {
                    //specified path exists, check if it's writeable
                    if (Settings.IsDirectoryWriteable(userLocalPath))
                    {
                        Settings.LocalPath = userLocalPath;
                    }
                    else
                    {
                        //if a path is provided and exists but can't be written, fall back to default
                        Logger.Error("Provided LocalPath [{0}] can't be written, default will be used [{1}] ", userLocalPath, defaultLocalPath);
                        tryUseDefaultLocalPath = true;
                    }
                }
                else
                {
                    //if provided path does not exists, fall back to default
                    Logger.Error("Provided LocalPath [{0}] does not exists, default will be used [{1}] ", userLocalPath, defaultLocalPath);
                    tryUseDefaultLocalPath = true;
                }
            }
            else
            {
                //if nothing is provided, fall back to default
                tryUseDefaultLocalPath = true;
            }

            //if default local path should be used, check if it's writeable
            if (tryUseDefaultLocalPath)
            {
                Settings.LocalPath = defaultLocalPath;
                if (!Directory.Exists(defaultLocalPath))
                {
                    //try creating directory
                    try
                    {
                        Directory.CreateDirectory(defaultLocalPath);
                        Logger.Info("Default local path '{0}' succesfully created", defaultLocalPath);
                    }
                    catch
                    {
                        Logger.Error("Error creating default local path '{0}', program can't continue", defaultLocalPath);
                        return false;
                    }

                }
                if (!Settings.IsDirectoryWriteable(defaultLocalPath))
                {
                    Logger.Error("Default LocalPath [{0}] can't be written, program can't continue", defaultLocalPath);
                    return false;
                }
            }

            //if user also specified a remote path, check it
            if (!string.IsNullOrWhiteSpace(userRemotePath))
            {
                if (Directory.Exists(userRemotePath))
                {
                    if (Settings.IsDirectoryWriteable(userRemotePath))
                    {
                        Settings.RemotePath = userRemotePath;
                        Settings.CanUseRemotePath = true;
                    }
                    else
                    {
                        Logger.Error("Provided RemotePath [{0}] is reachable but can't be written, setting will be ignored", userRemotePath);
                    }
                }
                else
                {
                    Logger.Warn("Provided RemotePath [{0}] does not exists or it's not reachable at the moment, setting will be ignored", userRemotePath);
                }
            }

            //when path parsing and checking is finished, notify paths currently in use
            Logger.Info("Local path is {0}", Settings.LocalPath);
            if (Settings.CanUseRemotePath)
                Logger.Info("Remote path is {0}", Settings.RemotePath);
            else
                Logger.Info("Remote path is not in use");

            //history items

            Settings.CreateIniKeyIfEmpty(IniSections.ApplicationData, IniKeys.HistoryItemsCount);
            var historyItemsCount = IniManager.GetValue(IniSections.ApplicationData, IniKeys.HistoryItemsCount, 0);
            for (int i = 0; i < historyItemsCount; i++)
            {
                var historyItem = IniManager.GetValue(IniSections.ApplicationData, IniKeys.HistoryItemPrefix + i.ToString(), string.Empty);
                Settings.DownloadsHistory.Add(historyItem);
            }

            return true;

        }

        private static List<string> ConvertIniValueToList(string section, string key)
        {
            var values = IniManager.GetValue(section, key, string.Empty).Split(SplitCharacters, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<string>();
            foreach (var value in values)
            {
                list.Add(value.Trim().ToLowerInvariant());
            }
            return list;
        }

        public static void AddHistoryItem(string historyItem)
        {
            lock (IniManager)
            {
                var historyItemsCount = IniManager.GetValue(IniSections.ApplicationData, IniKeys.HistoryItemsCount, 0);
                historyItemsCount++;
                IniManager.SetValue(IniSections.ApplicationData, IniKeys.HistoryItemsCount, historyItemsCount);
                IniManager.SetValue(IniSections.ApplicationData, (IniKeys.HistoryItemPrefix + historyItemsCount.ToString()).Trim(Environment.NewLine.ToCharArray()),
                    historyItem.Trim(Environment.NewLine.ToCharArray()));
                DownloadsHistory.Add(historyItem);
            }
        }

        /// <summary>
        /// Creates an empty entry in the INI file if the provided section-key couple can't be found
        /// </summary>
        private static void CreateIniKeyIfEmpty(string section, string key)
        {
            var guid = Guid.NewGuid().ToString();
            var value = IniManager.GetValue(section, key, guid);
            if (value.Equals(guid)) //if the INI manager couldn't find the key and assigned the default value
            {
                IniManager.SetValue(section, key, string.Empty);
                Logger.Trace("INI key not found, default will be used: {0}.{1}", section, key);
            }
        }

        /// <summary>
        /// Check if directory is writeable by the program
        /// </summary>
        private static bool IsDirectoryWriteable(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return false;

            var tempFile = Path.GetRandomFileName();
            var fullPath = Path.Combine(folderPath, tempFile);

            try
            {
                File.AppendAllText(fullPath, "");
                File.Delete(fullPath);
                return true;
            }
            catch
            {
            }
            return false;
        }
    }
}
