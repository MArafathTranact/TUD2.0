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

namespace TUD2._0.Cameras
{
    public class HandleCamera : IHandleTUDCommand
    {
        private List<Camera> Cameras = new List<Camera>();
        private List<CameraGroup> CameraGroups = new List<CameraGroup>();

        public HandleCamera(List<Camera> cameras, List<CameraGroup> cameraGroups)
        {
            CameraGroups = cameraGroups;
            Cameras = cameras;
        }
        public void ProcessCommandHandle(Camera camera, TudCommand command)
        {
            Task.Run(() => TriggerCamera(null));
        }

        private async Task TriggerCamera(JpeggerCameraCaptureRequest request)
        {
            try
            {
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
                }
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Exception at HandleCamera.TriggerCamera ", ex);
            }


        }

        private static async Task<bool> CaptureCameraImage(Camera camera, JpeggerCameraCaptureRequest request)
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

        private static async Task HandleGroupCameras(List<Camera> cameras, JpeggerCameraCaptureDataModel request)
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

        private static async Task TakePicture(Camera camera, JpeggerCameraCaptureDataModel request)
        {
            try
            {
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
                }
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Exception at HandleCamera.TakePicture ", ex);
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
    }
}
