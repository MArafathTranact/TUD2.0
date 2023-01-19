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

namespace TUD2._0.Waycom
{
    internal class HandleWaycomSignaturePad : IHandleTUDCommand
    {

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hSnapshot);

        [DllImport("kernel32.dll")]
        static extern uint WTSGetActiveConsoleSessionId();
        PostImageToJpegger postImage;

        public HandleWaycomSignaturePad()
        {
            postImage = new PostImageToJpegger();
        }

        public void ProcessCommandHandle(Camera camera, TudCommand command)
        {
            try
            {


                var Disclaimer = string.IsNullOrWhiteSpace(camera.contract_text) ? "This is valid contract for customer ." : camera.contract_text;

                var path = ServiceConfiguration.GetFileLocation("ExecutablePath");
                var executablePath = string.Format("{0}{1}",
                                            path,
                                            @"ScrapDragon.Signature.Wacom.exe");

                if (!File.Exists(executablePath))
                {
                    Logger.LogWarningWithNoLock($" No Wacom executable available for operation in {path}");
                }
                else
                {


                    //string startupPath = Path.Combine(Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName, "HamsterFingerprintReader.exe");
                    var logPath = string.Format("{0}{1}",
                                            path,
                                            @"SignatureLog.txt");

                    var signaturePath = string.Format("{0}",
                                                     path.TrimEnd(new char[] { '\\' }));
                    var signaturePathJpegger = string.Format("{0}",
                                                  path.TrimEnd(new char[] { '\\' }));

                    var commandString = string.Format("\"{0}\" \"{1}\" \"{2}\" \"{3}\"",
                                                                     Disclaimer,
                                                                     signaturePath,
                                                                     signaturePathJpegger,
                                                                     logPath);

                    ApplicationLoader.PROCESS_INFORMATION procInfo;
                    ApplicationLoader.StartProcessAndBypassUAC(executablePath, commandString, out procInfo);

                    //uint winlogonPid = 0;
                    // obtain the currently active session id; every logged on user in the system has a unique session id
                    uint dwSessionId = WTSGetActiveConsoleSessionId();

                    var appdone = false;
                    while (!appdone)
                    {

                        Process[] processes = Process.GetProcessesByName("ScrapDragon.Signature.Wacom");
                        if (processes != null && processes.Any())
                            appdone = false;
                        else
                            appdone = true;
                        Thread.Sleep(1000);
                    }

                    Task.Factory.StartNew(() => { postImage.LoadImageToJpegger(logPath, path + "sig_with_text.jpg", "Waycom Signature ", command); });
                }
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Exception at HandleWaycomSignaturePad.ProcessCommandHandle : ", ex);
            }

        }
    }
}
