﻿using System;
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

namespace TUD2._0.ESeek
{
    internal class HandleESeekLicenseScanner : IHandleTUDCommand
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hSnapshot);

        [DllImport("kernel32.dll")]
        static extern uint WTSGetActiveConsoleSessionId();

        PostImageToJpegger postImage;
        int _workStationId = 0;
        public HandleESeekLicenseScanner()
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
                                            @"ESeekScanner.exe");
                _workStationId = workStationId;

                if (!File.Exists(executablePath))
                {
                    Logger.LogWarningWithNoLock($" No Twain Kodak executable available for operation in {path}");
                }
                else
                {

                    var eSeekDocPath = path + @"ESeekImage.jpg";
                    var commandStringLog = path + @"ESeekScannerLog.txt";

                    var commandString = $"\"{eSeekDocPath}\" \"{commandStringLog}\"";

                    ApplicationLoader.PROCESS_INFORMATION procInfo;
                    ApplicationLoader.StartProcessAndBypassUAC(executablePath, commandString, out procInfo);

                    //uint winlogonPid = 0;
                    // obtain the currently active session id; every logged on user in the system has a unique session id
                    uint dwSessionId = WTSGetActiveConsoleSessionId();

                    var appdone = false;
                    while (!appdone)
                    {

                        Process[] processes = Process.GetProcessesByName("ESeekScanner");
                        if (processes != null && processes.Any())
                            appdone = false;
                        else
                            appdone = true;
                        Thread.Sleep(1000);
                    }
                    Task.Run(() => UpdateWorkStation());

                    Task.Factory.StartNew(() => { postImage.LoadImageToJpegger(commandStringLog, eSeekDocPath, "ESeek Scanner ", command); });
                }
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Exception at HandleESeekLicenseScanner.ProcessCommandHandle : ", ex);
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
                Logger.LogExceptionWithNoLock(" Exception at HandleESeekLicenseScanner.UpdateWorkStation : ", ex);
            }

        }
    }
}
