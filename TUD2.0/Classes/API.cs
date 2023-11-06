using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace TUD2._0.Classes
{
    public class API
    {
        private static readonly string jpeggerEndPoint = ServiceConfiguration.GetFileLocation("JPEGgerAPI");
        private static readonly string jpeggerToken = ServiceConfiguration.GetFileLocation("JPEGgerToken");

        public T PutRequest<T>(T updateItem, string param)
        {
            string responseBody = string.Empty;
            var method = "";
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", jpeggerToken);
                    //client.Timeout = TimeSpan.FromSeconds(APITimeOut);
                    method = jpeggerEndPoint + param;
                    using (HttpResponseMessage response = client.PutAsync(method, updateItem, new JsonMediaTypeFormatter()).Result)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            responseBody = response.Content.ReadAsStringAsync().Result;
                        }
                        else
                        {
                            Logger.LogWarningWithNoLock($" Failure code : {response.ReasonPhrase}, Method url : {method}, Parameters : {JsonConvert.SerializeObject(updateItem)}");
                        }
                    }

                }
                return JsonConvert.DeserializeObject<T>(responseBody);
            }
            catch (HttpRequestException ex)
            {
                Logger.LogExceptionWithNoLock($" Method url : {method}, Parameters : {JsonConvert.SerializeObject(updateItem)}", ex);
                return default;

            }
            catch (TaskCanceledException ex)
            {
                Logger.LogExceptionWithNoLock($" Method url : {method}, Parameters : {JsonConvert.SerializeObject(updateItem)}", ex);
                return default;

            }
            catch (Exception ex)
            {
                Logger.LogExceptionWithNoLock($" Method url : {method}, Parameters : {JsonConvert.SerializeObject(updateItem)}", ex);
                return default;

            }
        }
    }
}
