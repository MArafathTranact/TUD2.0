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

        PostImageToJpegger postImage;

        public HandleFingerPrintReader()
        {
            postImage = new PostImageToJpegger();
        }
        public void ProcessCommandHandle(Camera camera, TudCommand command)
        {

            try
            {
                var path = ServiceConfiguration.GetFileLocation("ExecutablePath");
                var executablePath = path + @"HamsterFingerprintReader.exe"; //GetAppSettingValue("ExecutablePath");

                if (!File.Exists(executablePath))
                {
                    Logger.LogWarningWithNoLock($" No HamsterFingerprintReader executable available for operation in {path}");
                }
                else
                {

                    //string startupPath = Path.Combine(Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName, "HamsterFingerprintReader.exe");

                    var commandStringImage = path + @"FingerprintScanLog.jpg";//$"{Path.GetTempPath()}FingerprintScanLog.jpg";
                    var commandStringLog = path + @"FingerprintScanLog.txt";// $"{Path.GetTempPath()}FingerprintScanLog.txt";



                    var commandString = $"\"{commandStringImage}\" \"{commandStringLog}\"";

                    ApplicationLoader.PROCESS_INFORMATION procInfo;
                    ApplicationLoader.StartProcessAndBypassUAC(executablePath, commandString, out procInfo);

                    //uint winlogonPid = 0;
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
                        Thread.Sleep(1000);
                    }
                    Task.Factory.StartNew(() => { postImage.LoadImageToJpegger(commandStringLog, commandStringImage, "Finger Print ", command); });

                }
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Exception at HandleFingerPrintReader.ProcessCommandHandle : ", ex);
            }
        }
    }
}
