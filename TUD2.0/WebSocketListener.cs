﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TUD2._0.Classes;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.Timers;
using TUD2._0.FingerPrint;
using TUD2._0.Waycom;
using TUD2._0.Topaz;
using TUD2._0.ESeek;
using TUD2._0.Gemalto;
using TUD2._0.TwainKodak;
using TUD2._0.Cameras;
using TUD2._0.WeightScale;
using Newtonsoft.Json.Linq;

namespace TUD2._0
{
    public class WebSocketListener
    {

        #region Properties

        private readonly string workStationWebSocket = ServiceConfiguration.GetFileLocation("WorkStationWebSocket");//@"ws://devjpegger.tranact.com/cable";
        private readonly string jpeggerEndPoint = ServiceConfiguration.GetFileLocation("JPEGgerAPI"); //@"https://devjpegger.tranact.com/api/v1/";
        private readonly string jpeggerToken = ServiceConfiguration.GetFileLocation("JPEGgerToken");
        private readonly int addToken = int.Parse(ServiceConfiguration.GetFileLocation("IncludeToken"));

        private bool ValidtWebSockettry = true;
        private string WorkStationIp = ServiceConfiguration.GetFileLocation("WorkStationIp");
        private string WorkStationPort = ServiceConfiguration.GetFileLocation("WorkStationPort");
        private string WorkStationName = "Test Workstation";
        public int WorkStationId = 0;
        WebSocketCommandHandler webSocketCommandHandler;
        public bool webSocketCommandProcessed = true;


        private static List<Camera> Cameras = new List<Camera>();
        private static List<CameraGroup> CameraGroups = new List<CameraGroup>();
        private static List<CameraTypes> CameraTypes = new List<CameraTypes>();
        private static List<Contracts> CameraContracts = new List<Contracts>();
        public static WorkStation TUDWorkStation = new WorkStation();
        private System.Timers.Timer refreshcameras = new System.Timers.Timer(1000 * 60 * 2);
        private bool CallWorkStation = true;

        #endregion

        public WebSocketListener()
        {
            //if (TUDWorkStation != null)
            //{
            Task.Factory.StartNew(() => { LoadCameras(); });
            refreshcameras.Elapsed += new ElapsedEventHandler(RefreshCamerasEvent);
            refreshcameras.Start();
            //}
            //else
            //{
            //    Logger.LogWarningWithNoLock($" No matching Workstation is not available.");
            //}
        }

        private void RefreshCamerasEvent(object source, ElapsedEventArgs e)
        {
            try
            {
                LogEvents($" Entered RefreshCameras/Groups");
                LoadCameras();
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Exception at WebSocketListener.RefreshCamerasEvent.", ex);
            }
        }

        public void CloseWebSocket()
        {
            try
            {
                CleanObjects();
            }
            catch (Exception)
            {

            }

        }

        private void CleanObjects()
        {
            try
            {
                ValidtWebSockettry = false;
                refreshcameras.Stop();
                refreshcameras.Enabled = false;
                Cameras = null;
                CameraGroups = null;
                GC.Collect();
            }
            catch (Exception)
            {

            }
        }


        #region Web Socket
        public async Task ConnectWebSocket()
        {
            try
            {
                await Task.Delay(5000);

                if (TUDWorkStation == null)
                    return;

                if (string.IsNullOrEmpty(workStationWebSocket))
                {
                    Logger.LogWarningWithNoLock($" Work Station '{WorkStationName}' : Web Socket end point is not provided.");
                    return;
                }
                ClientWebSocket ws = new ClientWebSocket();

                if (addToken == 1 && !string.IsNullOrWhiteSpace(jpeggerToken))
                    ws.Options.SetRequestHeader("Token", jpeggerToken);
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                await ws.ConnectAsync(new Uri(workStationWebSocket), CancellationToken.None);
                LogEvents($" Work Station '{WorkStationName}' : Web Socket connected... ");

                var sending = Task.Run(async () =>
                {
                    try
                    {
                        var subscription = @"{""command"":""subscribe"", ""identifier"":""{\""channel\"":\""WorkstationChannel\"",\""ip\"":\""WorkStationIp\"",\""port\"":\""WorkStationPort\""}""}";
                        subscription = subscription.Replace("WorkStationIp", WorkStationIp);
                        subscription = subscription.Replace("WorkStationPort", WorkStationPort);


                        var bytes = Encoding.UTF8.GetBytes(subscription);
                        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, endOfMessage: true, cancellationToken: CancellationToken.None);

                        if (ws != null && ws.State != WebSocketState.Open)
                        {
                            LogEvents($" Work Station '{WorkStationName}' : Listening for Web Socket command... ");
                        }
                        else
                        {
                            LogEvents($" Work Station '{WorkStationName}' : Web Socket Closed");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogExceptionWithNoLock($" Work Station '{WorkStationName}' : Exception at Socket Subscription :", ex);
                    }

                });

                var receiving = Receiving(ws);

                await Task.WhenAll(sending, receiving);

            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Work Station '{WorkStationName}' : Exception at WebSocketListener.ConnectWebSocket.", ex);
                LogEvents($" Work Station '{WorkStationName}' : Retrying to connect... {workStationWebSocket}");

                await Task.Delay(30000);
                await ConnectWebSocket();
            }
        }

        private async Task Receiving(ClientWebSocket ws)
        {
            var buffer = new byte[2048];
            //var resu = "";


            try
            {
                while (ValidtWebSockettry)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var resu = Encoding.UTF8.GetString(buffer, 0, result.Count);

                        try
                        {
                            var socket = JsonConvert.DeserializeObject<SocketMessage>(resu);

                            if (socket == null) { }


                            else if (socket != null && socket.message == null)
                            { }

                            else if (socket != null && !string.IsNullOrEmpty(socket.message.command))
                            {

                                var command = JsonConvert.DeserializeObject<TudCommand>(socket.message.command);
                                if (command != null && !string.IsNullOrWhiteSpace(command.camera_name))
                                {
                                    var camera = Cameras.Where(x => x.camera_name.ToLower() == command.camera_name.ToLower().Trim()).FirstOrDefault();

                                    if (camera != null)
                                    {
                                        var cameraType = CameraTypes.Where(x => x.ID == camera.camera_type).FirstOrDefault();
                                        if (cameraType != null)
                                        {
                                            switch (cameraType.Description)
                                            {
                                                case "Hamster Finger Print Reader":

                                                    if (camera.workstation_ip != WorkStationIp && camera.workstation_port != WorkStationPort && camera.IsNetCam != 0)
                                                    {
                                                        LogEvents($" Work Station '{WorkStationName}' : Command received to trigger Hamster Fingerprint Reader for different workstation");
                                                        return;
                                                    }
                                                    LogEvents($" Work Station '{WorkStationName}' : Command received to trigger Hamster Fingerprint Reader ");
                                                    webSocketCommandProcessed = false;
                                                    webSocketCommandHandler = new WebSocketCommandHandler(new HandleFingerPrintReader());
                                                    webSocketCommandHandler.ProcessCommandHandle(camera, command, WorkStationId);
                                                    break;
                                                case "Waycom Signature Pad":
                                                case "Wacom Signature Pad":
                                                    if (camera.workstation_ip != WorkStationIp && camera.workstation_port != WorkStationPort && camera.IsNetCam != 0)
                                                    {
                                                        LogEvents($" Work Station '{WorkStationName}' : Command received to trigger Wacom Signature Pad for different workstation");
                                                        return;
                                                    }
                                                    LogEvents($" Work Station '{WorkStationName}' : Command received to trigger Wacom Signature Pad ");
                                                    webSocketCommandProcessed = false;

                                                    if (camera.contract_id != null && CameraContracts != null && CameraContracts.Any())
                                                    {
                                                        var contract = CameraContracts.Where(x => x.contract_id == camera.contract_id.ToString()).FirstOrDefault();

                                                        if (contract != null)
                                                            camera.contract_text = contract.text1;
                                                        else
                                                        {
                                                            Logger.LogWithNoLock($" No contract found for Camera '{camera.camera_name}' with Contract Id {camera.contract_id}.");
                                                        }
                                                    }
                                                    webSocketCommandHandler = new WebSocketCommandHandler(new HandleWaycomSignaturePad());
                                                    webSocketCommandHandler.ProcessCommandHandle(camera, command, WorkStationId);
                                                    break;
                                                case "Topaz Signature Pad":

                                                    if (camera.workstation_ip != WorkStationIp && camera.workstation_port != WorkStationPort && camera.IsNetCam != 0)
                                                    {
                                                        LogEvents($" Work Station '{WorkStationName}' : Command received to trigger Topaz Signature Pad for different workstation");
                                                        return;
                                                    }
                                                    LogEvents($" Work Station '{WorkStationName}' : Command received to trigger Topaz Signature Pad ");
                                                    webSocketCommandProcessed = false;

                                                    if (camera.contract_id != null && CameraContracts != null && CameraContracts.Any())
                                                    {
                                                        var contract = CameraContracts.Where(x => x.contract_id == camera.contract_id.ToString()).FirstOrDefault();

                                                        if (contract != null)
                                                            camera.contract_text = contract.text1;
                                                        else
                                                        {
                                                            Logger.LogWithNoLock($" No contract found for Camera '{camera.camera_name}' with Contract Id {camera.contract_id}.");
                                                        }
                                                    }

                                                    webSocketCommandHandler = new WebSocketCommandHandler(new HandleTopazSignaturePad());
                                                    webSocketCommandHandler.ProcessCommandHandle(camera, command, WorkStationId);
                                                    break;
                                                case "Twain Kodak Doc Scanner":
                                                    if (camera.workstation_ip != WorkStationIp && camera.workstation_port != WorkStationPort && camera.IsNetCam != 0)
                                                    {
                                                        LogEvents($" Work Station '{WorkStationName}' : Command received to trigger Twain Kodak Doc Scanner for different workstation");
                                                        return;
                                                    }
                                                    LogEvents($" Work Station '{WorkStationName}' : Command received to trigger Twain Kodak Doc Scanner ");
                                                    webSocketCommandProcessed = false;
                                                    webSocketCommandHandler = new WebSocketCommandHandler(new HandleTwainKodakScanner());
                                                    webSocketCommandHandler.ProcessCommandHandle(camera, command, WorkStationId);
                                                    break;
                                                case "ESeek License Scanner":

                                                    if (camera.workstation_ip != WorkStationIp && camera.workstation_port != WorkStationPort && camera.IsNetCam != 0)
                                                    {
                                                        LogEvents($" Work Station '{WorkStationName}' : Command received to trigger  ESeek License Scanner for different workstation");
                                                        return;
                                                    }
                                                    LogEvents($" Work Station '{WorkStationName}' : Command received to trigger ESeek License Scanner ");
                                                    webSocketCommandProcessed = false;
                                                    webSocketCommandHandler = new WebSocketCommandHandler(new HandleESeekLicenseScanner());
                                                    webSocketCommandHandler.ProcessCommandHandle(camera, command, WorkStationId);

                                                    break;
                                                case "Gemalto License Scanner":
                                                    if (camera.workstation_ip != WorkStationIp && camera.workstation_port != WorkStationPort && camera.IsNetCam != 0)
                                                    {
                                                        LogEvents($" Work Station '{WorkStationName}' : Command received to trigger Gemalto License Scanner for different workstation");
                                                        return;
                                                    }
                                                    LogEvents($" Work Station '{WorkStationName}' : Command received to trigger Gemalto License Scanner ");
                                                    webSocketCommandProcessed = false;
                                                    webSocketCommandHandler = new WebSocketCommandHandler(new HandleGemaltoLicenseScanner());
                                                    webSocketCommandHandler.ProcessCommandHandle(camera, command, WorkStationId);
                                                    break;
                                                case "Scale Reader":
                                                    if (camera.workstation_ip != WorkStationIp && camera.workstation_port != WorkStationPort && camera.IsNetCam != 0)
                                                    {
                                                        LogEvents($" Work Station '{WorkStationName}' : Command received to trigger Scale Reader for different workstation");
                                                        return;
                                                    }
                                                    LogEvents($" Work Station '{WorkStationName}' : Command received to trigger Scale Reader ");
                                                    webSocketCommandProcessed = false;
                                                    webSocketCommandHandler = new WebSocketCommandHandler(new HandleScaleReader(Cameras, CameraGroups));
                                                    webSocketCommandHandler.ProcessCommandHandle(camera, command, WorkStationId);
                                                    break;
                                                default:

                                                    if (camera.IsNetCam == 1)
                                                    {
                                                        LogEvents($" Work Station '{WorkStationName}' : Command received to trigger Cameras ");
                                                        webSocketCommandProcessed = false;
                                                        webSocketCommandHandler = new WebSocketCommandHandler(new HandleCamera(Cameras, CameraGroups));
                                                        webSocketCommandHandler.ProcessCommandHandle(camera, command, WorkStationId);

                                                    }
                                                    else
                                                    {
                                                        LogEvents($" Work Station '{WorkStationName}' : Ping received {JsonConvert.SerializeObject(socket)}");
                                                        webSocketCommandProcessed = false;
                                                    }
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            LogEvents($" Work Station '{WorkStationName}' : Invalid Camera Type to trigger.");
                                        }

                                    }
                                    else
                                    {
                                        var cameraGroup = GetCameraGroupInfo(command.camera_name.Trim());

                                        if (cameraGroup?.Count > 0)
                                        {
                                            LogEvents($" Work Station '{WorkStationName}' : Command received to trigger Camera group ");
                                            webSocketCommandProcessed = false;
                                            webSocketCommandHandler = new WebSocketCommandHandler(new HandleCamera(Cameras, CameraGroups));
                                            webSocketCommandHandler.ProcessCommandHandle(camera, command, WorkStationId);
                                        }
                                        else
                                        {
                                            LogEvents($" Work Station '{WorkStationName}' : No valid camera available.");
                                        }
                                    }
                                }
                                else
                                {
                                    LogEvents($" Work Station '{WorkStationName}' : No camera parameter provided in command request.");
                                }

                            }
                        }
                        catch (Exception)
                        {

                        }

                    }
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                        break;
                    }

                    if (!ValidtWebSockettry)
                        break;
                }

            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Work Station '{WorkStationName}' : Exception at WebSocketListener.ConnectWebSocket.", ex);

                if (ws != null && ws.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }
                ws.Dispose();
                ws = null;
                if (ValidtWebSockettry)
                    await ConnectWebSocket();
            }
            finally
            {
                if (ws != null)
                {
                    await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    ws.Dispose();
                    ws = null;
                    await Task.Delay(5000);
                }
            }

        }

        #endregion

        #region Cameras

        private async Task LoadCameras()
        {
            if (CallWorkStation)
            {
                CallWorkStation = false;
                await GetWorkstations();

                Task.Delay(5000);

                if (TUDWorkStation == null)
                {
                    Logger.LogWarningWithNoLock($" No matching Workstation is not available for IP={WorkStationIp}, Port={WorkStationPort}.");
                    CallWorkStation = true;
                    return;
                }
                else
                {
                    Logger.LogWithNoLock($" WorkStation IP={WorkStationIp}, Port={WorkStationPort}  matched with available list .");
                }
            }

            Cameras = await GetCameraList();
            var camCount = Cameras == null ? 0 : Cameras.Count;
            LogEvents($" Loaded {camCount} Cameras ");

            CameraGroups = await GetCameraGroupList();
            var camgroupCount = CameraGroups == null ? 0 : CameraGroups.Count;
            LogEvents($" Loaded {camgroupCount} Camera Groups.");

            CameraTypes = await GetCameraTypes();

            var camTypesCount = CameraTypes == null ? 0 : CameraTypes.Count;
            LogEvents($" Loaded {camTypesCount} Camera Types.");

            CameraContracts = await GetContracts();
            var cameraContractsCount = CameraContracts == null ? 0 : CameraContracts.Count;
            LogEvents($" Loaded {cameraContractsCount} Contracts.");
        }

        private async Task<List<Contracts>> GetContracts()
        {
            try
            {
                return await Get<List<Contracts>>($"contracts", jpeggerToken, jpeggerEndPoint);
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Work Station '{WorkStationName}' : Exception at WebSocketListener.GetContracts.", ex);
                return new List<Contracts>();
            }
        }

        private async Task GetWorkstations()
        {
            try
            {
                var workStation = await Get<List<WorkStation>>($"workstations", jpeggerToken, jpeggerEndPoint);
                if (workStation != null)
                {
                    TUDWorkStation = workStation.Where(x => x.ip == WorkStationIp && x.port == WorkStationPort).FirstOrDefault();
                    if (TUDWorkStation != null)
                    {
                        WorkStationName = TUDWorkStation.name;
                        WorkStationIp = TUDWorkStation.ip;
                        WorkStationPort = TUDWorkStation.port;
                        WorkStationId = TUDWorkStation.id;

                    }
                    {
                        Logger.LogWarningWithNoLock($" No matching Workstation is not available.");
                    }
                }
                else
                {
                    Logger.LogWarningWithNoLock($" Workstation details are not available.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Exception at WebSocketListener.GetWorkstations.", ex);
            }
        }

        private async Task<List<CameraTypes>> GetCameraTypes()
        {
            try
            {
                return await Get<List<CameraTypes>>($"camera_types", jpeggerToken, jpeggerEndPoint);
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Work Station '{WorkStationName}' : Exception at WebSocketListener.GetCameraTypes.", ex);
                return new List<CameraTypes>();
            }
        }
        private async Task<List<Camera>> GetCameraList()
        {
            try
            {
                var camCollection = await Get<List<Camera>>($"cameras", jpeggerToken, jpeggerEndPoint);
                if (camCollection == null)
                    return new List<Camera>();

                //var filteredCameras = camCollection.Where(x => x.workstation_ip == WorkStationIp && x.workstation_port == WorkStationPort).ToList();
                if (camCollection != null)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var camera in camCollection)
                    {
                        if (!string.IsNullOrEmpty(camera.URL) && !string.IsNullOrEmpty(camera.ip_address))
                        {
                            sb.Clear();
                            sb.Append("http://");
                            sb.Append(camera.ip_address);
                            sb.Append(camera.URL);
                            camera.URL = sb.ToString();
                        }
                    }
                    sb = null;
                }
                return camCollection;
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Work Station '{WorkStationName}' : Exception at WebSocketListener.GetCameraList.", ex);
                return new List<Camera>();
            }

        }

        private async Task<List<CameraGroup>> GetCameraGroupList()
        {
            try
            {
                return await Get<List<CameraGroup>>($"camera_groups", jpeggerToken, jpeggerEndPoint);

            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Work Station '{WorkStationName}' : Exception at WebSocketListener.GetCameraGroupList.", ex);
                return new List<CameraGroup>();
            }
        }


        public async Task<T> Get<T>(string path, string token, string endpoint)
        {
            var httpResponseString = string.Empty;

            try
            {
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(30)
                };

                if (addToken == 1 && !string.IsNullOrWhiteSpace(token))
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", jpeggerToken);

                var httpResponse = await httpClient.GetAsync(endpoint + path);
                if (httpResponse.IsSuccessStatusCode)
                {
                    httpResponseString = await httpResponse.Content.ReadAsStringAsync();
                }
                else
                    Logger.LogWithNoLock($" Failure code : {httpResponse.ReasonPhrase}\r\n{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff}|INFO| Method url : {endpoint + path}");
                return JsonConvert.DeserializeObject<T>(httpResponseString);

            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Work Station '{WorkStationName}' : Exception at WebSocketListener.Get.", ex);

                return JsonConvert.DeserializeObject<T>(httpResponseString);
            }
        }

        private static List<Camera> GetCameraGroupInfo(string cameraGroup)
        {
            if (CameraGroups?.Count > 0)
            {
                var camGroup = CameraGroups.Where(x => x.cam_group.ToLower() == cameraGroup.ToLower() && !string.IsNullOrEmpty(x.cam_name));
                if (camGroup.Any())
                {
                    return Cameras.Where(x => camGroup.Any(z => x.camera_name.ToLower() == z.cam_name.ToLower())).ToList();
                }
            }
            return default;
        }
        #endregion

        private void LogEvents(string input)
        {
            Logger.LogWithNoLock(input);
        }
    }
}
