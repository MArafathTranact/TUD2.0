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

namespace TUD2._0.Topaz
{
    internal class HandleTopazSignaturePad : IHandleTUDCommand
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hSnapshot);

        [DllImport("kernel32.dll")]
        static extern uint WTSGetActiveConsoleSessionId();
        PostImageToJpegger postImage;
        public HandleTopazSignaturePad()
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
                                            @"TopazSigPad.exe");

                if (!File.Exists(executablePath))
                {
                    Logger.LogWarningWithNoLock($" No Topaz sig pad executable available for operation in {path}");
                }
                else
                {

                    var imagePath = path + @"sig_with_text.jpg";
                    var commandStringLog = path + @"TopazSigPadLog.txt";
                    var contract = string.IsNullOrWhiteSpace(camera.contract_text) ? "The undersigned covenants with the buy that he or she is the lawful owner of the above described mechandise, the the same is free from all encumbrances that the undersigned will warrant and defend the sale of the said property " : camera.contract_text;

                    var commandString = $"\"{imagePath}\" \"{commandStringLog}\" \"{contract}\"";

                    ApplicationLoader.PROCESS_INFORMATION procInfo;
                    ApplicationLoader.StartProcessAndBypassUAC(executablePath, commandString, out procInfo);

                    //uint winlogonPid = 0;
                    // obtain the currently active session id; every logged on user in the system has a unique session id
                    uint dwSessionId = WTSGetActiveConsoleSessionId();

                    var appdone = false;
                    while (!appdone)
                    {

                        Process[] processes = Process.GetProcessesByName("TopazSigPad");
                        if (processes != null && processes.Any())
                            appdone = false;
                        else
                            appdone = true;
                        Thread.Sleep(1000);
                    }

                    Task.Factory.StartNew(() => { postImage.LoadImageToJpegger(commandStringLog, imagePath, "Topaz sig pad ", command); });
                }
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Exception at HandleWaycomSignaturePad.ProcessCommandHandle : ", ex);
            }
        }
    }
}
