using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TUD2._0.Cameras;
using TUD2._0.Classes;
using TUD2._0.Interface;
namespace TUD2._0.WeightScale
{
    internal class HandleScaleReader : IHandleTUDCommand
    {
        PostImageToJpegger postImage;
        int _workStationId = 0;
        private SerialPort _cPort;
        //private int BaudRate = int.Parse(ServiceConfiguration.GetFileLocation("BaudRate"));
        //private string DataStop = ServiceConfiguration.GetFileLocation("DataStop");
        //private int ScaleParity = int.Parse(ServiceConfiguration.GetFileLocation("ScaleParity"));
        //private int ComPort = int.Parse(ServiceConfiguration.GetFileLocation("ComPort"));
        //private int BufferSize = int.Parse(ServiceConfiguration.GetFileLocation("BufferSize"));
        //private int? WeightBeginPosition = int.Parse(ServiceConfiguration.GetFileLocation("WeightBeginPosition"));
        //private int? WeightEndPosition = int.Parse(ServiceConfiguration.GetFileLocation("WeightEndPosition"));
        //private int? MotionPosition = int.Parse(ServiceConfiguration.GetFileLocation("MotionPosition"));
        //private int? UnitsPosition = int.Parse(ServiceConfiguration.GetFileLocation("UnitsPosition"));
        //private int? ModePosition = int.Parse(ServiceConfiguration.GetFileLocation("ModePosition"));
        //private int? StartOfText = int.Parse(ServiceConfiguration.GetFileLocation("StartOfText"));
        //private int? NoMotionChar = int.Parse(ServiceConfiguration.GetFileLocation("NoMotionChar"));
        //private int? LbUnitsChar = int.Parse(ServiceConfiguration.GetFileLocation("LbUnitsChar"));
        //private int? GrossModeChar = int.Parse(ServiceConfiguration.GetFileLocation("GrossModeChar"));
        //private int? MaxCharToRead = int.Parse(ServiceConfiguration.GetFileLocation("MaxCharToRead"));
        //private int? NumberOfMatchingRead = int.Parse(ServiceConfiguration.GetFileLocation("NumberOfMatchingRead"));
        private string _errorMessage;
        private string _scaleOutput;
        private List<Camera> Cameras = new List<Camera>();
        private List<CameraGroup> CameraGroups = new List<CameraGroup>();
        private readonly string yardId = ServiceConfiguration.GetFileLocation("YardId");

        public HandleScaleReader(List<Camera> cameras, List<CameraGroup> cameraGroups)
        {
            postImage = new PostImageToJpegger();
            CameraGroups = cameraGroups;
            Cameras = cameras;
        }

        public void ProcessCommandHandle(Camera camera, TudCommand command, int workStationId)
        {
            try
            {
                ReadCom(camera);
                if (!string.IsNullOrEmpty(camera.scale_camera_name))
                {
                    Task.Run(() => TriggerCamera(command));
                }
                Task.Run(() => UpdateWorkStation());
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Exception at HandleScaleReader.ProcessCommandHandle : ", ex);
            }
        }

        private void ReadCom(Camera camera)
        {
            try
            {

                var charlist = new List<int>();
                var i = 0;
                var matchReads = 0;
                var lastRead = string.Empty;

                InitializeComPort(camera);

                if (_cPort == null) return;

                if (!_cPort.IsOpen)
                {
                    _cPort.Open();
                }

                _cPort.DiscardInBuffer();
                _cPort.DiscardOutBuffer();


                while (i < camera.MaxCharToRead)
                {
                    var sb = new StringBuilder();
                    var currentCharacter = _cPort.ReadChar();

                    if (currentCharacter == camera.StartOfText)
                    {
                        while (i < camera.MaxCharToRead)
                        {
                            charlist.Add(currentCharacter);
                            {

                            }

                            currentCharacter = _cPort.ReadChar();
                            if (currentCharacter == camera.StartOfText)
                            {
                                var nextRead = FormatWeight(charlist, camera);
                                if (nextRead == lastRead)
                                {
                                    matchReads++;
                                }
                                else
                                {
                                    matchReads = 0;
                                }

                                if (matchReads >= camera.NumberOfMatchingRead)
                                {
                                    i = camera.MaxCharToRead ?? 150;
                                }

                                lastRead = nextRead;
                            }
                            else
                                i++;
                        }
                    }
                    else
                        i++;
                }

                _scaleOutput = lastRead;
                CloseComPort();

            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock(" Exception at HandleScaleReader.ReadCom : ", ex);
            }
        }

        private string FormatWeight(List<int> charlist, Camera camera)
        {
            var unitsPos = (camera.UnitsPosition ?? 1) - 1;
            var modePos = (camera.ModePosition ?? 1) - 1;
            var motionPos = (camera.MotionPosition ?? 1) - 1; //Subtract 1 since charlist index starts at 0
            var sb = new StringBuilder();

            if (charlist.Count <= unitsPos)
            {
                return "Invalid unitsPos";
            }
            else if (charlist[unitsPos] != (camera.LbUnitsChar ?? 0))
            {
                return "Invalid LbUnitsChar";
            }

            if (charlist.Count <= modePos)
            {
                return "Invalid modePos";
            }
            else if (charlist[modePos] != (camera.GrossModeChar ?? 0))
            {
                return "Invalid GrossModeChar";
            }

            if (charlist.Count <= motionPos)
            {
                return "Invalid motionPos";
            }
            else if (charlist[motionPos] != (camera.NoMotionChar ?? 0))
            {
                return "Invalid NoMotionChar";
            }

            var parts = charlist.GetRange(((camera.WeightBeginPosition ?? 1) - 1), ((camera.WeightEndPosition ?? 1) + 1 - (camera.WeightBeginPosition ?? 1)));

            foreach (var item in parts)
            {
                sb.Append(Convert.ToChar(item));
            }

            // Clear Previous Errors
            _errorMessage = string.Empty;

            return sb.ToString().Trim();
        }

        private void CloseComPort()
        {
            try
            {
                if (_cPort != null
                               && _cPort.IsOpen)
                {
                    _cPort.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock(" Exception at HandleScaleReader.CloseComPort : ", ex);
            }
        }

        private void InitializeComPort(Camera camera)
        {
            try
            {
                _cPort = ComObject.InitializeCom(
                    new ComDefinition
                    {
                        BaudRate = camera.BaudRate,
                        DataBits = Convert.ToInt32(Regex.Split(camera.DataStop, "/")[0]),
                        StopBits = (StopBits)Convert.ToInt32(Regex.Split(camera.DataStop, "/")[1]),
                        Parity = (Parity)camera.ScaleParity,
                        PortName = "COM" + camera.ComPort,
                        ReadTimeout = 3000
                    });
            }
            catch (System.FormatException ex)
            {
                Logger.LogExceptionWithNoLock(" Exception at InitializeComPort.InitializeComPort : ", ex);
            }
        }

        private async Task TriggerCamera(TudCommand command)
        {
            try
            {
                var request = new JpeggerCameraCaptureRequest
                {
                    CaptureDataApi = new JpeggerCameraCaptureDataModel { YardId = Guid.Parse(yardId), SpecifyJpeggerTable = "Images", CommodityName = command.commodity, CameraName = command.camera_name, TicketNumber = command.ticket_nbr, EventCode = command.event_code },
                    YardId = string.IsNullOrWhiteSpace(command.yardid) ? yardId : command.yardid,
                    BranchCode = command.branch_code,
                    CameraName = command.camera_name,
                    EventCode = command.event_code,
                    TicketNumber = command.ticket_nbr,
                    CommodityName = command.commodity,
                    TransactionType = command.transaction_type,
                };

                _ = Task.Run(async () =>
                {
                    var handleCamera = new HandleCamera(Cameras, CameraGroups);
                    await handleCamera.TriggerCamera(request);


                });
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Exception at HandleCamera.ProcessCommandHandle ", ex);
            }
        }

        private async Task UpdateWorkStation()
        {
            try
            {
                var api = new API();
                var updateWorkStation = new UpdateWorkStation() { command = !string.IsNullOrEmpty(_errorMessage) ? $"Error:{_errorMessage}" : $"Scale={_scaleOutput}" };
                api.PutRequest<UpdateWorkStation>(updateWorkStation, $"workstations/{_workStationId}");
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock(" Exception at InitializeComPort.UpdateWorkStation : ", ex);
            }

        }
    }
}
