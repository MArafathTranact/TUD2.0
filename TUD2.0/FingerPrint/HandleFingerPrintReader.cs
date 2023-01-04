using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TUD2._0.Classes;
using TUD2._0.Interface;

namespace TUD2._0.FingerPrint
{
    internal class HandleFingerPrintReader : IHandleTUDCommand
    {

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hSnapshot);

        [DllImport("kernel32.dll")]
        static extern uint WTSGetActiveConsoleSessionId();

        public void ProcessCommandHandle(Camera camera)
        {

            try
            {
                var executablePath = @"C:\Program Files (x86)\Transact Universal Driver\HamsterFingerprintReader.exe"; //GetAppSettingValue("ExecutablePath");

                //string startupPath = Path.Combine(Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName, "HamsterFingerprintReader.exe");

                var commandStringImage = @"C:\Program Files (x86)\Transact Payment Systems,Inc\TUD2.0Installer\FingerprintScanLog.jpg";//$"{Path.GetTempPath()}FingerprintScanLog.jpg";
                var commandStringLog = @"C:\Program Files (x86)\Transact Payment Systems,Inc\TUD2.0Installer\FingerprintScanLog.txt";// $"{Path.GetTempPath()}FingerprintScanLog.txt";



                var commandString = $"\"{commandStringImage}\" \"{commandStringLog}\"";

                ApplicationLoader.PROCESS_INFORMATION procInfo;
                ApplicationLoader.StartProcessAndBypassUAC(executablePath, commandString, out procInfo);

                uint winlogonPid = 0;
                // obtain the currently active session id; every logged on user in the system has a unique session id
                uint dwSessionId = WTSGetActiveConsoleSessionId();

                var appdone = false;
                while (!appdone)
                {

                    Process[] processes = Process.GetProcessesByName("HamsterFingerprintReader");
                    if (processes != null && processes.Any())
                        appdone = false;
                    else
                        appdone = true;
                    Thread.Sleep(100);
                    //foreach (Process p in processes)
                    //{
                    //    appdone = false;
                    //}

                }

            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Exception at HandleFingerPrintReader.ProcessCommandHandle : ", ex);
            }
        }
    }
}
