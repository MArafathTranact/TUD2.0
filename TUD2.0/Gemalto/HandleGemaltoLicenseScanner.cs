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

namespace TUD2._0.Gemalto
{
    internal class HandleGemaltoLicenseScanner : IHandleTUDCommand
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hSnapshot);

        [DllImport("kernel32.dll")]
        static extern uint WTSGetActiveConsoleSessionId();

        PostImageToJpegger postImage;
        int _workStationId = 0;
        public HandleGemaltoLicenseScanner()
        {
            postImage = new PostImageToJpegger();
        }
        public void ProcessCommandHandle(Camera camera, TudCommand command, int workStationId)
        {
            try
            {



                var path = ServiceConfiguration.GetFileLocation("ExecutablePath");
                var executablePath = string.Format("{0}{1}",
                                            path,
                                            @"GemaltoScanner.exe");
                _workStationId = workStationId;

                if (!File.Exists(executablePath))
                {
                    Logger.LogWarningWithNoLock($" No Gemalto Scanner executable available for operation in {path}");
                }
                else
                {

                    var gemaltodocPath = path + @"GemaltoLicenseImage.jpg";
                    var commandStringLog = path + @"GemaltoScannerLog.txt";

                    var commandString = $"\"{gemaltodocPath}\" \"{commandStringLog}\"";

                    ApplicationLoader.PROCESS_INFORMATION procInfo;
                    ApplicationLoader.StartProcessAndBypassUAC(executablePath, commandString, out procInfo);

                    //uint winlogonPid = 0;
                    // obtain the currently active session id; every logged on user in the system has a unique session id
                    uint dwSessionId = WTSGetActiveConsoleSessionId();

                    var appdone = false;
                    while (!appdone)
                    {

                        Process[] processes = Process.GetProcessesByName("GemaltoScanner");
                        if (processes != null && processes.Any())
                            appdone = false;
                        else
                            appdone = true;
                        Thread.Sleep(1000);
                    }

                    var faceimagePath = string.Format("{0}{1}",
                                           path,
                                           @"GemaltoFaceImage.jpg");

                    Task.Run(() => UpdateWorkStation());

                    Task.Factory.StartNew(() => { postImage.LoadImageToJpegger(commandStringLog, gemaltodocPath, "Gemalto License Scanner ", command, faceimagePath); });
                }
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Exception at HandleGemaltoLicenseScanner.ProcessCommandHandle : ", ex);
            }
        }

        private async Task UpdateWorkStation()
        {
            try
            {
                var api = new API();
                var updateWorkStation = new UpdateWorkStation();
                api.PutRequest<UpdateWorkStation>(updateWorkStation, $"workstations/{_workStationId}");
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock(" Exception at HandleWaycomSignaturePad.UpdateWorkStation : ", ex);
            }

        }
    }
}
