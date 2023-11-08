using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TUD2._0.Classes;
using TUD2._0.Interface;
using System.IO;
using NLog.Fluent;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace TUD2._0.Cameras
{
    public class HandleCamera : IHandleTUDCommand
    {
        private List<Camera> Cameras = new List<Camera>();
        private List<CameraGroup> CameraGroups = new List<CameraGroup>();
        private readonly string jpeggerEndPoint = ServiceConfiguration.GetFileLocation("JPEGgerAPI");
        private readonly string jpeggerToken = ServiceConfiguration.GetFileLocation("JPEGgerToken");
        private readonly string yardId = ServiceConfiguration.GetFileLocation("YardId");
        private readonly int addToken = int.Parse(ServiceConfiguration.GetFileLocation("IncludeToken"));
        int _workStationId = 0;

        public HandleCamera(List<Camera> cameras, List<CameraGroup> cameraGroups)
        {
            CameraGroups = cameraGroups;
            Cameras = cameras;
        }
        public void ProcessCommandHandle(Camera camera, TudCommand command, int workStationId)
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
                _workStationId = workStationId;
                Task.Run(() => UpdateWorkStation());
                Task.Run(() => TriggerCamera(request));
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Exception at HandleCamera.ProcessCommandHandle ", ex);
            }

        }

        public async Task TriggerCamera(JpeggerCameraCaptureRequest request)
        {
            try
            {
                if (request == null) { return; }

                var cameraGroup = GetCameraGroupInfo(request.CaptureDataApi.CameraName);

                if (cameraGroup?.Count > 0)
                {
                    await HandleGroupCameras(cameraGroup, request.CaptureDataApi);
                }
                else
                {
                    var cameraInfo = GetCameraInfo(request.CaptureDataApi.CameraName);
                    if (cameraInfo != null && cameraInfo.IsNetCam == 1 && !string.IsNullOrEmpty(cameraInfo.URL))
                    {
                        await CaptureCameraImage(cameraInfo, request);

                    }
                    else if (cameraInfo != null)
                    {
                        Logger.LogWarningWithNoLock($" '{cameraInfo.camera_name}' No a valid camera to take picture or no url is provided ");
                    }
                    else
                    {
                        Logger.LogWarningWithNoLock($" No a valid camera in the list. ");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Exception at HandleCamera.TriggerCamera ", ex);
            }


        }

        private async Task<bool> CaptureCameraImage(Camera camera, JpeggerCameraCaptureRequest request)
        {
            try
            {
                if (camera != null && !string.IsNullOrEmpty(camera.URL))
                {
                    await TakePicture(camera, request.CaptureDataApi);

                    if (request.CaptureDataApi.CaptureCameraPictures != null && !request.CaptureDataApi.CaptureCameraPictures.Contains(camera.camera_name))
                    {
                        request.CaptureDataApi.CameraPostSuccess.Clear();

                        return false;
                    }
                    if (request.CaptureDataApi.CameraPostSuccess.Contains(camera.camera_name))
                    {
                        request.CaptureDataApi.CameraPostSuccess.Clear();

                        return true;
                    }
                    else
                    {
                        request.CaptureDataApi.CameraPostSuccess.Clear();

                        return false;
                    }
                }
                request.CaptureDataApi.CameraPostSuccess.Clear();

                return false;


            }
            catch (Exception ex)
            {

                Logger.LogExceptionWithNoLock($" Exception at HandleCamera.CaptureCameraImage ", ex);
                return false;
            }
        }

        private async Task HandleGroupCameras(List<Camera> cameras, JpeggerCameraCaptureDataModel request)
        {
            try
            {
                var cameraResponse = new List<bool>();
                var failedCamera = new StringBuilder();
                var failedJpeggerPost = new StringBuilder();

                var takePictureTasks = new List<Task>();
                request.CameraGroupName = request.CameraName;

                foreach (var camera in cameras)
                {
                    takePictureTasks.Add(TakePicture(camera, request));
                }

                await Task.WhenAll(takePictureTasks);


                var failedCamerasList = cameras.Where(x => !request.CaptureCameraPictures.Any(y => y == x.camera_name));

                foreach (var camera in failedCamerasList)
                {
                    failedCamera.Append($"Camera: {camera.camera_name}, Ip: {camera.ip_address} \n");
                }

                if (!string.IsNullOrEmpty(failedCamera.ToString()))
                    failedCamera.Insert(0, $"Jpegger Camera Capture failed. Check configuration \n");

                var failedPostsList = request.CaptureCameraPictures.Where(x => !request.CameraPostSuccess.Any(y => y == x));

                foreach (var camera in failedPostsList)
                {
                    failedJpeggerPost.Append($"Camera: {camera} \n");
                }

                if (!string.IsNullOrEmpty(failedJpeggerPost.ToString()))
                    failedJpeggerPost.Insert(0, $"Error in posting images into jpegger API \n");

                failedCamera.Append(failedJpeggerPost.ToString());

                request.CaptureCameraPictures.Clear();
                request.CameraPostSuccess.Clear();
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Exception at HandleCamera.HandleGroupCameras ", ex);
            }


        }

        private async Task TakePicture(Camera camera, JpeggerCameraCaptureDataModel request)
        {
            try
            {
                Logger.LogWithNoLock($" Firing Camera '{camera.camera_name}' with IP '{camera.ip_address}' and Port '{camera.port_nbr}'");
                var requestUri = new Uri(camera.URL);
                var credCache = new CredentialCache();

                if (camera.isBasic)
                {
                    credCache.Add(requestUri, "Basic", new NetworkCredential(camera.username, camera.pwd));
                }
                else
                {
                    credCache.Add(requestUri, "Digest", new NetworkCredential(camera.username, camera.pwd));
                }
                var clientHander = new HttpClientHandler { Credentials = credCache, PreAuthenticate = true };

                var httpClient = new HttpClient(clientHander)
                {
                    Timeout = TimeSpan.FromSeconds(60)
                };

                var httpResponse = await httpClient.GetAsync(requestUri);

                byte[] buffer = new byte[5000000];
                int read, total = 0;

                if (httpResponse.IsSuccessStatusCode)
                {
                    var stream = await httpResponse.Content.ReadAsStreamAsync();
                    while ((read = stream.Read(buffer, total, 1000)) != 0)
                    {
                        total += read;
                    }
                }
                var image = new MemoryStream(buffer, 0, total);
                if (image != null && image.Length > 0)
                {
                    Logger.LogWithNoLock($" Success in Capturing image from  Camera '{camera.camera_name}'");
                    await PostJpeggerImage(image, request, camera.camera_name);
                }
                else
                {
                    Logger.LogWithNoLock($" Failed in Capturing image from  Camera '{camera.camera_name}'");
                }
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Exception at HandleCamera.TakePicture : Camera '{camera.camera_name}' with IP '{camera.ip_address}' and Port '{camera.port_nbr}'", ex);
            }


        }

        private async Task PostJpeggerImage(Stream img, JpeggerCameraCaptureDataModel request, string cameraName)
        {
            try
            {
                var formData = GenerateMultipartFormData(img, request, cameraName);

                if (formData != null)
                {
                    await PostMultiForm(formData, request, cameraName);
                }
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Exception at HandleCamera.PostJpeggerImage ", ex);
            }
        }

        private MultipartFormDataContent GenerateMultipartFormData(Stream image, JpeggerCameraCaptureDataModel request, string cameraName)
        {
            try
            {
                var table = request.SpecifyJpeggerTable.TrimEnd('s').ToLowerInvariant();
                var multipartFormContent = new MultipartFormDataContent
                {
                    { new StreamContent(image), "\"" + $"{table}[file]" + "\"", "display.jpg" },
                    { new StringContent(yardId), "\"" + $"{table}[yardid]" + "\"" }
                };

                foreach (var prop in request.GetType().GetProperties())
                {
                    var propValue = prop.GetValue(request);
                    switch (prop.Name)
                    {
                        case "TicketNumber":
                            if (propValue != null && propValue.ToString() != "-1" && propValue.ToString() != "0")
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[ticket_nbr]" + "\"");
                            break;

                        case "CameraName":
                            if (cameraName != null)
                                multipartFormContent.Add(new StringContent(cameraName), "\"" + $"{table}[camera_name]" + "\"");
                            break;

                        case "CameraGroupName":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[camera_group]" + "\"");
                            break;

                        case "EventCode":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[event_code]" + "\"");
                            break;

                        case "ReceiptNumber":
                            if (propValue != null && propValue.ToString() != "-1" && propValue.ToString() != "0")
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[receipt_nbr]" + "\"");
                            break;

                        case "Location":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[location]" + "\"");
                            break;

                        case "TareSequenceNumber":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[tare_seq_nbr]" + "\"");
                            break;

                        case "Amount":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[amount]" + "\"");
                            break;

                        case "ContractNumber":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[contr_nbr]" + "\"");
                            break;

                        case "ContractName":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[contr_name]" + "\"");
                            break;

                        case "Weight":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[weight]" + "\"");
                            break;

                        case "CustomerName":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[cust_name]" + "\"");
                            break;

                        case "CustomerNumber":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[cust_nbr]" + "\"");
                            break;

                        case "CertificationNumber":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[cert_nbr]" + "\"");
                            break;

                        case "CertificateDescription":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[cert_desc]" + "\"");
                            break;

                        case "CommodityName":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[cmdy_name]" + "\"");
                            break;

                        case "ContainerNumber":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[container_nbr]" + "\"");
                            break;
                    }
                }

                return multipartFormContent;
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Exception at HandleCamera.GenerateMultipartFormData ", ex);
                return null;
            }
        }


        public async Task PostMultiForm(MultipartFormDataContent content, JpeggerCameraCaptureDataModel request, string cameraName)
        {
            try
            {
                var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(60);

                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("multipart/form-data"));

                if (addToken == 1 && !string.IsNullOrWhiteSpace(jpeggerToken))
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", jpeggerToken);


                var response = await httpClient.PostAsync(jpeggerEndPoint + request.SpecifyJpeggerTable.ToLowerInvariant(), content);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();

                    request.CameraPostSuccess.Add(cameraName);
                    Logger.LogWithNoLock($" Success in posting images for Ticket Number ='{request.TicketNumber}' ,Camera '{cameraName}'");
                }
                else
                {
                    Logger.LogWarningWithNoLock($" Warning at PostMultiForm() Failure Response : '{response.ReasonPhrase}' : Ticket Number ='{request.TicketNumber}' , Camera Name ='{cameraName}' , Camera Group Name ='{request.CameraGroupName}' ");
                }

            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Exception at HandleCamera.PostMultiForm : Ticket Number ='{request.TicketNumber}' , Camera Name ='{cameraName}'", ex);
            }
        }


        private List<Camera> GetCameraGroupInfo(string cameraGroup)
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


        private Camera GetCameraInfo(string cameraName)
        {
            if (Cameras != null && Cameras.Any())
            {
                return Cameras.Where(x => x.camera_name.ToLower() == cameraName.ToLower()).FirstOrDefault();
            }
            else
                return null;
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
                Logger.LogExceptionWithNoLock(" Exception at HandleCamera.UpdateWorkStation : ", ex);
            }

        }
    }
}
