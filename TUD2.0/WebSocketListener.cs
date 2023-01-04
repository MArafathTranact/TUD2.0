using System;
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

namespace TUD2._0
{
    public class WebSocketListener
    {

        #region Properties

        private readonly string workStationWebSocket = @"ws://devjpegger.tranact.com/cable";
        private readonly string jpeggerEndPoint = @"https://devjpegger.tranact.com/api/v1/";
        private readonly string jpeggerToken = "";
        private readonly int addToken = 0;

        private bool ValidtWebSockettry = true;
        private string WorkStationIp = "192.168.111.2";
        private string WorkStationPort = "4444";
        private string WorkStationName = "Arafath's Desktop";
        WebSocketCommandHandler webSocketCommandHandler;
        public bool webSocketCommandProcessed = true;


        private static List<Camera> Cameras = new List<Camera>();
        private static List<CameraGroup> CameraGroups = new List<CameraGroup>();
        private System.Timers.Timer refreshcameras = new System.Timers.Timer(1000 * 60 * 2);

        #endregion

        public WebSocketListener()
        {
            Task.Factory.StartNew(() => { LoadCameras(); });
            refreshcameras.Elapsed += new ElapsedEventHandler(RefreshCamerasEvent);
            refreshcameras.Start();
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
                await Task.Delay(10000);
                if (string.IsNullOrEmpty(workStationWebSocket))
                {
                    Logger.LogWarningWithNoLock($" Work Station '{WorkStationName}' : Web Socket end point is not provided.");
                    return;
                }
                ClientWebSocket ws = new ClientWebSocket();

                //ws.Options.SetRequestHeader("Token", jpeggerToken);
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
                        LogEvents($" Work Station '{WorkStationName}' : Listening for Web Socket command... ");
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

                await Task.Delay(5000);
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

                            if (socket == null)
                                return;
                            else if (socket != null && socket.message == null)
                                return;
                            else if (socket != null && !string.IsNullOrEmpty(socket.message.command))
                            {

                                var camera = Cameras.Where(x => x.camera_name == socket.message.command.Trim()).FirstOrDefault();

                                if (camera != null)
                                {
                                    switch (camera.camera_type)
                                    {
                                        case 59:
                                            LogEvents($" Work Station '{WorkStationName}' : Hamster Fingerprint Reader Command received");
                                            webSocketCommandProcessed = false;
                                            webSocketCommandHandler = new WebSocketCommandHandler(new HandleFingerPrintReader());
                                            webSocketCommandHandler.ProcessCommandHandle(camera);
                                            break;
                                        case 60:
                                            LogEvents($" Work Station '{WorkStationName}' : Waycam Signature Pad Command received");
                                            webSocketCommandProcessed = false;
                                            webSocketCommandHandler = new WebSocketCommandHandler(new HandleWaycomSignaturePad());
                                            webSocketCommandHandler.ProcessCommandHandle(camera);
                                            break;
                                        case 61:
                                            LogEvents($" Work Station '{WorkStationName}' : Command received");
                                            webSocketCommandProcessed = false;
                                            webSocketCommandHandler = new WebSocketCommandHandler(new HandleTopazSignaturePad());
                                            webSocketCommandHandler.ProcessCommandHandle(camera);
                                            break;
                                        case 62:
                                            LogEvents($" Work Station '{WorkStationName}' : Command received");
                                            webSocketCommandProcessed = false;
                                            webSocketCommandHandler = new WebSocketCommandHandler(new HandleESeekLicenseScanner());
                                            webSocketCommandHandler.ProcessCommandHandle(camera);
                                            break;
                                        case 63:
                                            LogEvents($" Work Station '{WorkStationName}' : Command received");
                                            webSocketCommandProcessed = false;
                                            webSocketCommandHandler = new WebSocketCommandHandler(new HandleGemaltoLicenseScanner());
                                            webSocketCommandHandler.ProcessCommandHandle(camera);
                                            break;
                                        case 64:
                                            LogEvents($" Work Station '{WorkStationName}' : Command received");
                                            webSocketCommandProcessed = false;
                                            webSocketCommandHandler = new WebSocketCommandHandler(new HandleTwainKodakScanner());
                                            webSocketCommandHandler.ProcessCommandHandle(camera);
                                            break;
                                        default:
                                            LogEvents($" Work Station '{WorkStationName}' : Ping received {JsonConvert.SerializeObject(socket)}");
                                            webSocketCommandProcessed = false;
                                            await webSocketCommandHandler.HandleCommand("tots").ConfigureAwait(false);
                                            break;
                                    }
                                }
                                else
                                {
                                    LogEvents($" Work Station '{WorkStationName}' : No valid camera available.");
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
                    //await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
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
                    //await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
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
            Cameras = await GetCameraList();
            var camCount = Cameras == null ? 0 : Cameras.Count;
            LogEvents($" Loaded {camCount} cameras ");

            CameraGroups = await GetCameraGroupList();
            var camgroupCount = CameraGroups == null ? 0 : CameraGroups.Count;
            LogEvents($" Loaded {camgroupCount} camera groups.");
        }


        private async Task<List<Camera>> GetCameraList()
        {
            try
            {
                var camCollection = await Get<List<Camera>>($"cameras", jpeggerToken, jpeggerEndPoint);
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
                    Timeout = TimeSpan.FromSeconds(20)
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

        #endregion

        private void LogEvents(string input)
        {
            Logger.LogWithNoLock(input);
        }
    }
}
