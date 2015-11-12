using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PushbulletSharp;
using PushbulletSharp.Models.Requests;

namespace dlm
{
    internal static class Logger
    {
        private static readonly object _locker = new object();
        private static Dictionary<Level, string> _levelDescriptions;

        public enum Level
        {
            Trace,
            Debug,
            Information,
            Warning,
            Error,
        }

        private static void Log(Level level, string messageFormat, params object[] args)
        {
            //skip if logging level is higher
            if (level < Settings.LogLevel)
                return;

            if (_levelDescriptions == null)
            {
                _levelDescriptions = new Dictionary<Level, string>();
                _levelDescriptions.Add(Level.Trace, "TRC");
                _levelDescriptions.Add(Level.Debug, "DEB");
                _levelDescriptions.Add(Level.Information, "INF");
                _levelDescriptions.Add(Level.Warning, "WAR");
                _levelDescriptions.Add(Level.Error, "ERR");
            }

            string formattedMessage = string.Format("{0:yyyy-MM-dd HH:mm:ss}\t{1}\t{2}",
                DateTime.Now,
                _levelDescriptions[level],
                string.Format(messageFormat, args));

            lock (_locker)
            {
                File.AppendAllText(Settings.LogFilePath, formattedMessage + Environment.NewLine);
            }

            UI.Instance.SetLogEntry(string.Format("[{0:HH:mm:ss}] [{1}]: {2}", DateTime.Now, _levelDescriptions[level], string.Format(messageFormat, args)));
        }

        public static void Trace(string messageFormat, params object[] args)
        {
            Logger.Log(Level.Trace, messageFormat, args);
        }

        public static void Debug(string messageFormat, params object[] args)
        {
            Logger.Log(Level.Debug, messageFormat, args);
        }

        public static void Info(string messageFormat, params object[] args)
        {
            Logger.Log(Level.Information, messageFormat, args);
        }

        public static void Error(string messageFormat, params object[] args)
        {
            Logger.Log(Level.Error, messageFormat, args);
        }

        public static void Warn(string messageFormat, params object[] args)
        {
            Logger.Log(Level.Warning, messageFormat, args);
        }

        public static void Notify(string messageFormat, params object[] args)
        {

#if DEBUG
            return;
#endif
            PushbulletClient client = new PushbulletClient(Settings.PushbulletApiKey);

            //If you don't know your device_iden, you can always query your devices
            var userDevices = client.CurrentUsersDevices();

            //search for specified device, otherwise send to all devices
            var recipientDevices = new List<PushbulletSharp.Models.Responses.Device>();

            bool isUserSpecifiedDeviceFound = false;
            if (!string.IsNullOrWhiteSpace(Settings.PushbulletDeviceName))
            {
                foreach (var device in userDevices.Devices)
                {
                    if (device.Nickname.ToLowerInvariant().Contains(Settings.PushbulletDeviceName.ToLower()))
                    {
                        recipientDevices.Add(device);
                        isUserSpecifiedDeviceFound = true;
                        break;
                    }
                }
            }

            if (!isUserSpecifiedDeviceFound)
            {
                recipientDevices.AddRange(userDevices.Devices);
            }

            foreach (var device in recipientDevices)
            {
                if (device != null)
                {
                    var request = new PushNoteRequest()
                    {
                        DeviceIden = device.Iden,
                        Title = "Notification from dlm",
                        Body = string.Format(messageFormat, args)
                    };
                    var response = client.PushNote(request);
                }
            }

        }

    }
}
