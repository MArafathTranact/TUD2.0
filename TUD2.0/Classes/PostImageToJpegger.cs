using Newtonsoft.Json;
using NLog.Fluent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TUD2._0.Classes
{
    public class PostImageToJpegger
    {
        private static readonly Encoding encoding = Encoding.UTF8;

        public async Task<bool> LoadImageToJpegger(string commandStringLog, string commandStringImage, string message, TudCommand command)
        {
            try
            {
                if (!File.Exists(commandStringLog))
                {
                    Logger.LogWarningWithNoLock($" No Log file available in {commandStringLog}");
                }
                else
                {
                    using (var stream = new StreamReader(commandStringLog))
                    {
                        var logString = stream.ReadLine();

                        if (logString != null)
                        {
                            if (logString.Contains("SUCCESS"))
                            {
                                Logger.LogWithNoLock($" {message} is Successfully captured for camera '{command.camera_name}'");

                                if (!File.Exists(commandStringImage))
                                {
                                    Logger.LogWarningWithNoLock($" No file to upload in Jpegger under {commandStringImage}");
                                }
                                else
                                {
                                    using (MemoryStream mstream = new MemoryStream(File.ReadAllBytes(commandStringImage)))
                                    {
                                        var request = new JpeggerCameraCaptureRequest() { EventCode = command.event_code, CameraName = command.camera_name, SpecifyJpeggerTable = "Images" };
                                        var result = await PostJpeggerImage(mstream, command, request);

                                        if (result)
                                        {
                                            if (!string.IsNullOrEmpty(commandStringImage) && File.Exists(commandStringImage))
                                            {
                                                try
                                                {
                                                    File.Delete(commandStringImage);
                                                }
                                                catch (Exception ex)
                                                {
                                                    Logger.LogExceptionWithNoLock($" Exception at deleting the image file", ex);
                                                }
                                            }
                                            if (!string.IsNullOrEmpty(commandStringLog) && File.Exists(commandStringLog))
                                            {
                                                try
                                                {
                                                    File.Delete(commandStringLog);
                                                }
                                                catch (Exception ex)
                                                {
                                                    Logger.LogExceptionWithNoLock($" Exception at deleting the log file", ex);
                                                }
                                            }

                                            return true;
                                        }
                                        else
                                        {
                                            return false;
                                        }
                                    }
                                }


                            }
                            else if (logString.Contains("CANCEL"))
                            {
                                Logger.LogWarningWithNoLock($" {message} is Cancelled for camera '{command.camera_name}'");
                            }
                            else if (logString.Contains("SKIP"))
                            {
                                Logger.LogWarningWithNoLock($" {message} is Skipped for camera '{command.camera_name}'");
                            }
                        }

                    }
                }


            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($"Exception at PostImageToJpegger.LoadImageToJpegger", ex);
                return false;
            }

            return true;
        }


        private async Task<bool> PostJpeggerImage(MemoryStream img, TudCommand command, JpeggerCameraCaptureRequest request, string cameraGroupName = null)
        {
            try
            {
                var formData = GenerateMultipartFormData(img, command, cameraGroupName);

                if (formData != null)
                    return await PostMultiForm(formData, command.camera_name, request);
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Exception for Camera '{command.camera_name}' at PostImageToJpegger.PostJpeggerImage.", ex);
                return false;
            }
        }

        private MultipartFormDataContent GenerateMultipartFormData(MemoryStream image, TudCommand command, string cameraGroupName = null)
        {
            try
            {
                var multipartFormContent = new MultipartFormDataContent
                {
                    { new StreamContent(image), "\"" + $"image[file]" + "\"", "display.jpg" }
                };

                if (!string.IsNullOrWhiteSpace(cameraGroupName))
                    multipartFormContent.Add(new StringContent(cameraGroupName), "\"" + $"image[camera_group]" + "\"");

                if (!string.IsNullOrWhiteSpace(command.camera_name))
                    multipartFormContent.Add(new StringContent(command.camera_name), "\"" + $"image[camera_name]" + "\"");
                //multipartFormContent.Add(new StringContent(ndcClient.authenticateBarcode.receipt_nbr), "\"" + $"image[receipt_nbr]" + "\"");
                if (!string.IsNullOrWhiteSpace(command.event_code))
                    multipartFormContent.Add(new StringContent(command.event_code), "\"" + $"image[event_code]" + "\"");

                if (!string.IsNullOrWhiteSpace(command.location))
                    multipartFormContent.Add(new StringContent(command.location), "\"" + $"image[location]" + "\"");

                if (!string.IsNullOrWhiteSpace(command.branch_code))
                    multipartFormContent.Add(new StringContent(command.branch_code), "\"" + $"image[branch_code]" + "\"");

                if (!string.IsNullOrWhiteSpace(command.ticket_nbr))
                    multipartFormContent.Add(new StringContent(command.ticket_nbr), "\"" + $"image[ticket_nbr]" + "\"");

                if (!string.IsNullOrWhiteSpace(command.receipt_nbr))
                    multipartFormContent.Add(new StringContent(command.receipt_nbr), "\"" + $"image[receipt_nbr]" + "\"");

                if (!string.IsNullOrWhiteSpace(command.tare_seq_nbr.ToString()))
                    multipartFormContent.Add(new StringContent(command.tare_seq_nbr.ToString()), "\"" + $"image[tare_seq_nbr]" + "\"");

                if (!string.IsNullOrWhiteSpace(command.amount.ToString()))
                    multipartFormContent.Add(new StringContent(command.amount.ToString()), "\"" + $"image[amount]" + "\"");

                if (!string.IsNullOrWhiteSpace(command.transaction_type))
                    multipartFormContent.Add(new StringContent(command.transaction_type), "\"" + $"image[transaction_type]" + "\"");

                if (!string.IsNullOrWhiteSpace(command.amount.ToString()))
                    multipartFormContent.Add(new StringContent(command.amount.ToString()), "\"" + $"image[amount]" + "\"");

                if (!string.IsNullOrWhiteSpace(command.yardid))
                    multipartFormContent.Add(new StringContent(command.yardid), "\"" + $"image[yardid]" + "\"");


                return multipartFormContent;
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Exception for camera '{command.camera_name}' at PostImageToJpegger.GenerateMultipartFormData.", ex);
                return null;
            }
        }

        public async Task<bool> PostMultiForm(MultipartFormDataContent content, string cameraName, JpeggerCameraCaptureRequest request)
        {
            var status = false;
            try
            {
                var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(60)
                };

                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("multipart/form-data"));

                //if (addToken == 1 && !string.IsNullOrWhiteSpace(jpeggerToken))
                //    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", jpeggerToken);

                var response = await httpClient.PostAsync(ServiceConfiguration.GetFileLocation("JPEGgerAPI") + "images", content);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();

                    var Capture_seq_nbr = JsonConvert.DeserializeObject<CapturedImageResponse>(result).capture_seq_nbr;
                    Logger.LogWithNoLock($" Successfully posted the image to jpegger for camera '{cameraName}' with Capture Seq number ={Capture_seq_nbr}");

                    status = true;

                    //request.SuccessPicturePost.Add(cameraName);
                }
                else
                {
                    Logger.LogWarningWithNoLock($"Failure in posting the image to jpegger for camera '{cameraName}' : {response.ReasonPhrase}");
                    status = false;

                    // Logger.LogWithNoLock($" Dev {ndcClient.Deviceid} : Failed Response from API at NDCProcessBarcode.PostMultiForm :{result} for Camera :{cameraName}");
                }
                return status;
            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Exception for camera '{cameraName}' PostImageToJpegger.PostMultiForm.", ex);
                return status;
            }
        }

        //private async Task<bool> PostJpeggerImage(MemoryStream img, JpeggerCameraCaptureRequest request)
        //{
        //    try
        //    {
        //        // LogEvents(" Posting Image starts");
        //        var formDataBoundary = String.Format("----------{0:N}", Guid.NewGuid());
        //        var contentType = "multipart/form-data; boundary=" + formDataBoundary;
        //        var tableName = "images";
        //        var formData = await GenerateMultipartFormData(formDataBoundary, img, request, tableName.TrimEnd('s').ToLowerInvariant());
        //        if (formData != null)
        //        {
        //            return await PostMultiForm(contentType, formData, request);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.LogExceptionWithNoLock($"Exception at PostImageToJpegger.PostJpeggerImage : ", ex);
        //    }

        //    return false;
        //}

        //public async Task<bool> PostMultiForm(string contentType, byte[] formData, JpeggerCameraCaptureRequest captureRequest)
        //{
        //    var status = false;
        //    try
        //    {
        //        Logger.LogWithNoLock($" Posting Multipart form data for camera '{captureRequest.CameraName}'");
        //        ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

        //        var JpeggerAPI = ServiceConfiguration.GetFileLocation("JPEGgerAPI");//@"https://jpegger.eastus.azurecontainer.io/api/v1/";//GetAppSettingValue("JPEGgerAPI");
        //                                                                            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
        //        var request = (HttpWebRequest)WebRequest.Create(JpeggerAPI + captureRequest.SpecifyJpeggerTable);
        //        // LogEvents($" Web request method : {request.RequestUri}");
        //        request.Method = "POST";
        //        request.ContentType = contentType;
        //        request.ContentLength = formData.Length;

        //        using (Stream requestStream = request.GetRequestStream())
        //        {
        //            await requestStream.WriteAsync(formData, 0, formData.Length);
        //        }
        //        using (var response = await request.GetResponseAsync())
        //        {
        //            using (var responseStream = response.GetResponseStream())
        //            {
        //                StreamReader reader = new StreamReader(responseStream);
        //                var result = reader.ReadToEnd();
        //                try
        //                {
        //                    var capture_nbr = JsonConvert.DeserializeObject<CapturedImageResponse>(result);
        //                    if (capture_nbr != null && !string.IsNullOrEmpty(capture_nbr.Capture_Seq_Nbr))
        //                    {
        //                        status = true;
        //                        Logger.LogWithNoLock($" Image Posted successfully in to jpegger for camera '{captureRequest.CameraName}'. capture seq nbr = {capture_nbr.Capture_Seq_Nbr}");
        //                    }
        //                    else
        //                    {
        //                        Logger.LogWarningWithNoLock($" Image Posting failed for camera '{captureRequest.CameraName}'");
        //                        status = false;
        //                    }
        //                }
        //                catch (Exception)
        //                {
        //                    Logger.LogWarningWithNoLock($" Image Posting failed for camera '{captureRequest.CameraName}'");
        //                    status = false;
        //                }

        //            }


        //        }
        //        return status;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.LogExceptionWithNoLock($"Exception at PostImageToJpegger.PostMultiForm : ", ex);
        //        return status;
        //    }
        //}

        //private async Task<byte[]> GenerateMultipartFormData(string boundary, MemoryStream image, JpeggerCameraCaptureRequest request, string table)
        //{
        //    try
        //    {
        //        // LogEvents(" Generating Multipart form data...");
        //        using (var formDataStream = new MemoryStream())
        //        {
        //            var needNewLine = false;
        //            var param = string.Empty;
        //            foreach (var prop in request.GetType().GetProperties())
        //            {
        //                if (needNewLine)
        //                {
        //                    await formDataStream.WriteAsync(encoding.GetBytes("\r\n"), 0, encoding.GetByteCount("\r\n"));
        //                }

        //                switch (prop.Name)
        //                {
        //                    case "TicketNumber":
        //                        param = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}", boundary, $"{table}[ticket_nbr]", prop.GetValue(request).ToString());
        //                        break;

        //                    case "CameraName":
        //                        param = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}", boundary, $"{table}[camera_name]", prop.GetValue(request).ToString());
        //                        break;

        //                    case "EventCode":
        //                        param = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}", boundary, $"{table}[event_code]", prop.GetValue(request).ToString());
        //                        break;

        //                    case "ReceiptNumber":
        //                        param = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}", boundary, $"{table}[receipt_nbr]", prop.GetValue(request).ToString());
        //                        break;

        //                    case "Location":
        //                        param = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}", boundary, $"{table}[location]", prop.GetValue(request).ToString());
        //                        break;

        //                    case "TareSequenceNumber":
        //                        param = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}", boundary, $"{table}[tare_seq_nbr]", prop.GetValue(request).ToString());
        //                        break;

        //                    case "Amount":
        //                        param = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}", boundary, $"{table}[amount]", prop.GetValue(request).ToString());
        //                        break;

        //                    case "ContractNumber":
        //                        param = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}", boundary, $"{table}[contr_nbr]", prop.GetValue(request).ToString());
        //                        break;

        //                    case "ContractName":
        //                        param = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}", boundary, $"{table}[contr_name]", prop.GetValue(request).ToString());
        //                        break;

        //                    case "Weight":
        //                        param = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}", boundary, $"{table}[weight]", prop.GetValue(request).ToString());
        //                        break;

        //                    case "CustomerName":
        //                        param = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}", boundary, $"{table}[cust_name]", prop.GetValue(request).ToString());
        //                        break;

        //                    case "CustomerNumber":
        //                        param = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}", boundary, $"{table}[cust_nbr]", prop.GetValue(request).ToString());
        //                        break;

        //                    default:
        //                        needNewLine = false;
        //                        param = string.Empty;
        //                        break;
        //                }
        //                if (!string.IsNullOrEmpty(param))
        //                {
        //                    await formDataStream.WriteAsync(encoding.GetBytes(param), 0, encoding.GetByteCount(param));
        //                    needNewLine = true;
        //                }
        //            }

        //            if (formDataStream.Length != 0)
        //            {
        //                await formDataStream.WriteAsync(encoding.GetBytes("\r\n"), 0, encoding.GetByteCount("\r\n"));
        //            }

        //            param = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}", boundary, $"{table}[yardid]", "1612C2EA-4891-4F5A-84F6-B8C5F73CEB7C");
        //            if (!string.IsNullOrEmpty(param))
        //            {
        //                await formDataStream.WriteAsync(encoding.GetBytes(param), 0, encoding.GetByteCount(param));
        //            }

        //            param = string.Empty;

        //            if (formDataStream.Length != 0)
        //            {
        //                await formDataStream.WriteAsync(encoding.GetBytes("\r\n"), 0, encoding.GetByteCount("\r\n"));
        //            }

        //            byte[] img = image != null ? image.ToArray() : new byte[0];
        //            string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n", boundary, $"{table}[file]", "sigcap.jpg", "application/octet-stream");
        //            await formDataStream.WriteAsync(encoding.GetBytes(header), 0, encoding.GetByteCount(header));
        //            await formDataStream.WriteAsync(img, 0, img.Length);

        //            string footer = "\r\n--" + boundary + "--\r\n";
        //            await formDataStream.WriteAsync(encoding.GetBytes(footer), 0, encoding.GetByteCount(footer));
        //            formDataStream.Position = 0;

        //            byte[] formData = new byte[formDataStream.Length];
        //            await formDataStream.ReadAsync(formData, 0, formData.Length);
        //            return formData;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.LogExceptionWithNoLock($"Exception at PostImageToJpegger.GenerateMultipartFormData : ", ex);
        //        return null;
        //    }
        //}




        //public async Task<bool> PostJpeggerImage(MemoryStream img, JpeggerCameraCaptureRequest request)
        //{
        //    try
        //    {
        //        //LogEvents(" Posting Image starts");
        //        var tableName = "images";
        //        var formData = GenerateMultipartFormData(img, request, request.CameraName);
        //        if (formData != null)
        //        {
        //            PostMultiForm(formData, request, tableName);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.LogExceptionWithNoLock($"Exception at PostImageToJpegger.PostJpeggerImage", ex);
        //    }

        //    return false;
        //}

        //private static async Task PostMultiForm(MultipartFormDataContent content, JpeggerCameraCaptureRequest request, string cameraName)
        //{
        //    try
        //    {



        //        var httpClient = new HttpClient(); // _httpClientFactory.CreateClient(HttpClientNames.Jpegger);
        //        httpClient.Timeout = TimeSpan.FromSeconds(60);

        //        httpClient.DefaultRequestHeaders.Accept.Clear();
        //        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("multipart/form-data"));

        //        //if (!string.IsNullOrWhiteSpace(_yardOptionsService.GeneralOption.JpeggerToken))
        //        //    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", _yardOptionsService.GeneralOption.JpeggerToken);

        //        var response = await httpClient.PostAsync(@"https://devjpegger.tranact.com/api/v1/" + request.SpecifyJpeggerTable.ToLowerInvariant(), content);

        //        if (response.IsSuccessStatusCode)
        //        {
        //            var result = await response.Content.ReadAsStringAsync();
        //            //Capture_seq_nbr = JsonSerializer.Deserialize<CapturedImageResponse>(result).capture_seq_nbr;
        //            //request.CameraPostSuccess.Add(cameraName);
        //            Logger.LogWithNoLock($"Successfully posted image for '{cameraName}'");
        //        }
        //        else
        //        {
        //            Logger.LogWithNoLock($"Failed to posted image for '{cameraName}'");
        //            //Log.Logger.GetContext(typeof(Jpegger)).Error(response.ReasonPhrase, $"Jpegger.cs :: PostMultiForm() Failure Response :: Ticket Number ='{request.TicketNumber}' , Camera Name ='{cameraName}' , Camera Group Name ='{request.CameraGroupName}'");
        //        }

        //        // return response;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.LogExceptionWithNoLock($"Exception at PostImageToJpegger.PostMultiForm", ex);
        //        //Log.Logger.GetContext(typeof(Jpegger)).Error(ex, "Jpegger.cs :: PostMultiForm() Error");
        //    }
        //}

        //private static MultipartFormDataContent GenerateMultipartFormData(MemoryStream image, JpeggerCameraCaptureRequest request, string cameraName)
        //{
        //    try
        //    {
        //        var table = request.SpecifyJpeggerTable.TrimEnd('s').ToLowerInvariant();
        //        var multipartFormContent = new MultipartFormDataContent
        //        {
        //            { new StreamContent(image), "\"" + $"{table}[file]" + "\"", "display.jpg" }
        //           // { new StringContent(_yardOptionsService.Yard.Id.ToString()), "\"" + $"{table}[yardid]" + "\"" }
        //        };

        //        foreach (var prop in request.GetType().GetProperties())
        //        {
        //            var propValue = prop.GetValue(request);
        //            switch (prop.Name)
        //            {
        //                case "TicketNumber":
        //                    if (propValue != null && propValue.ToString() != "-1" && propValue.ToString() != "0")
        //                        multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[ticket_nbr]" + "\"");
        //                    break;

        //                case "CameraName":
        //                    if (cameraName != null)
        //                        multipartFormContent.Add(new StringContent(cameraName), "\"" + $"{table}[camera_name]" + "\"");
        //                    break;

        //                case "CameraGroupName":
        //                    if (propValue != null)
        //                        multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[camera_group]" + "\"");
        //                    break;

        //                case "EventCode":
        //                    if (propValue != null)
        //                        multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[event_code]" + "\"");
        //                    break;

        //                case "ReceiptNumber":
        //                    if (propValue != null && propValue.ToString() != "-1" && propValue.ToString() != "0")
        //                        multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[receipt_nbr]" + "\"");
        //                    break;

        //                case "Location":
        //                    if (propValue != null)
        //                        multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[location]" + "\"");
        //                    break;

        //                case "TareSequenceNumber":
        //                    if (propValue != null)
        //                        multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[tare_seq_nbr]" + "\"");
        //                    break;

        //                case "Amount":
        //                    if (propValue != null)
        //                        multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[amount]" + "\"");
        //                    break;

        //                case "ContractNumber":
        //                    if (propValue != null)
        //                        multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[contr_nbr]" + "\"");
        //                    break;

        //                case "ContractName":
        //                    if (propValue != null)
        //                        multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[contr_name]" + "\"");
        //                    break;

        //                case "Weight":
        //                    if (propValue != null)
        //                        multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[weight]" + "\"");
        //                    break;

        //                case "CustomerName":
        //                    if (propValue != null)
        //                        multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[cust_name]" + "\"");
        //                    break;

        //                case "CustomerNumber":
        //                    if (propValue != null)
        //                        multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[cust_nbr]" + "\"");
        //                    break;

        //                case "CertificationNumber":
        //                    if (propValue != null)
        //                        multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[cert_nbr]" + "\"");
        //                    break;

        //                case "CertificateDescription":
        //                    if (propValue != null)
        //                        multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[cert_desc]" + "\"");
        //                    break;

        //                case "CommodityName":
        //                    if (propValue != null)
        //                        multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[cmdy_name]" + "\"");
        //                    break;

        //                case "ContainerNumber":
        //                    if (propValue != null)
        //                        multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[container_nbr]" + "\"");
        //                    break;
        //            }
        //        }

        //        return multipartFormContent;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.LogExceptionWithNoLock($"Exception at PostImageToJpegger.GenerateMultipartFormData", ex);
        //        // Log.Logger.GetContext(typeof(Jpegger)).Error(ex, "Jpegger.cs :: GenerateMultipartFormData() Error");
        //        return null;
        //    }
        //}

    }
}
