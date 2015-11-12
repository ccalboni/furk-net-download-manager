using System;
using System.Runtime.InteropServices;

namespace dlm
{
    class Program
    {
        [DllImport("libc")]
        private static extern int system(string exec);

        public static void Main(string[] args)
        {
            //TODO: the console resize works only under Windows
            if (Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                system(@"printf '\e[8;100;100t'");
            }
            else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Console.WindowWidth = (int)(Console.LargestWindowWidth * 0.8);
                Console.WindowHeight = (int)(Console.LargestWindowHeight * 0.8);
                Console.BufferWidth = Console.WindowWidth;
                Console.BufferHeight = Console.WindowHeight;
            }

            UI.Instance.SetCurrentActivity("Program started on machine: " + Environment.MachineName);

            if (Settings.Init())
            {
                Settings.MustSimulateDownloads = true;
                var downloadProcess = new Process();
                downloadProcess.Run();
            }
            UI.Instance.SetCurrentActivity("Program has ended. Strike any key to close.");
            Console.ReadLine();
        }


    }

}
