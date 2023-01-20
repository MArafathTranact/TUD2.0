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

namespace TUD2._0.TwainKodak
{
    internal class HandleTwainKodakScanner : IHandleTUDCommand
    {

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hSnapshot);

        [DllImport("kernel32.dll")]
        static extern uint WTSGetActiveConsoleSessionId();
        PostImageToJpegger postImage;

        public HandleTwainKodakScanner()
        {
            postImage = new PostImageToJpegger();
        }

        public void ProcessCommandHandle(Camera camera, TudCommand command)
        {
            try
            {



                var path = ServiceConfiguration.GetFileLocation("ExecutablePath");
                var executablePath = string.Format("{0}{1}",
                                            path,
                                            @"TwainKodak.exe");

                if (!File.Exists(executablePath))
                {
                    Logger.LogWarningWithNoLock($" No Twain Kodak executable available for operation in {path}");
                }
                else
                {

                    var twainpdfPath = path + @"TwainKodak.pdf";
                    var commandStringLog = path + @"TwainKodakLog.txt";

                    var commandString = $"\"{twainpdfPath}\" \"{commandStringLog}\"";

                    ApplicationLoader.PROCESS_INFORMATION procInfo;
                    ApplicationLoader.StartProcessAndBypassUAC(executablePath, commandString, out procInfo);

                    //uint winlogonPid = 0;
                    // obtain the currently active session id; every logged on user in the system has a unique session id
                    uint dwSessionId = WTSGetActiveConsoleSessionId();

                    var appdone = false;
                    while (!appdone)
                    {

                        Process[] processes = Process.GetProcessesByName("TwainKodak");
                        if (processes != null && processes.Any())
                            appdone = false;
                        else
                            appdone = true;
                        Thread.Sleep(1000);
                    }

                    Task.Factory.StartNew(() => { postImage.LoadImageToJpegger(commandStringLog, twainpdfPath, "Kodak Doc Scanner ", command); });
                }
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Exception at HandleWaycomSignaturePad.ProcessCommandHandle : ", ex);
            }
        }
    }
}
