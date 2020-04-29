using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Aoto.EMS.Infrastructure
{
    public class HttpHelper
    {
        private const string contentType = "application/octet-stream; charset=utf-8";//"application/json"
        private static string host = GetEthernetIP();
        public static string Post(string path, string body)
        {
            return Post(Config.App.WebServer.Host, Config.App.WebServer.Port, Config.App.WebServer.ContextPath, Config.App.WebServer.Timeout, path, body);
        }
        public static string Post(string host, int port, string contextPath, int timeout, string path, string body)
        {
            log.DebugFormat("begin, args: path = {0}, body = {1}", path, body);

            byte[] data = CommonUtils.Encrypt(Encoding.UTF8.GetBytes(body), Config.App.AesKey);
            data = HttpHelper.Post(host, port, contextPath, timeout, path, data);
            string rtn = String.Empty;

            if (null != data && data.Length > 0)
            {
                rtn = Encoding.UTF8.GetString(CommonUtils.Decrypt(data, Config.App.AesKey));
            }

            log.DebugFormat("end, return: {0}", rtn);
            return rtn;
        }
        public static byte[] Post(string host, int port, string contextPath, int timeout, string path, byte[] data)
        {
            HttpWebRequest request = null;
            Stream requestStream = null;
            WebResponse response = null;
            Stream responseStream = null;
            MemoryStream ms = null;

            try
            {
                UriBuilder builder = new UriBuilder("http", host, port);
                builder.Path = contextPath + path;

                request = (HttpWebRequest)HttpWebRequest.Create(builder.Uri.AbsoluteUri);
                request.Timeout = timeout;
                request.KeepAlive = false;
                request.Method = WebRequestMethods.Http.Post;
                request.ContentType = contentType;

                request.Headers.Add("tradeCode", "qmssign");
                request.Headers.Add("qmsIp","192.168.43");

                //request.Credentials = new NetworkCredential(Config.App.WebServer.Username, Config.App.WebServer.Password);          

                if (null != data && data.Length > 0)
                {
                    requestStream = request.GetRequestStream();
                    requestStream.Write(data, 0, data.Length);
                }
            
                response = request.GetResponse();
                responseStream = response.GetResponseStream();

                int bufferSize = 2048;
                int readCount = 0;
                byte[] buffer = new byte[bufferSize];
                ms = new MemoryStream();

                while ((readCount = responseStream.Read(buffer, 0, bufferSize)) > 0)
                {
                    ms.Write(buffer, 0, readCount);
                }

                AppState.Online = true;
                return ms.ToArray();
            }
            catch (WebException e)
            {
                if (WebExceptionStatus.ConnectFailure == e.Status || WebExceptionStatus.Timeout == e.Status)
                {
                    AppState.Online = false;
                }

                throw;
            }
            finally
            {
                if (ms != null)
                {
                    ms.Close();
                }

                if (null != requestStream)
                {
                    requestStream.Close();
                }

                if (null != response)
                {
                    response.Close();
                }
            }
        }
        public static byte[] Post(string path, byte[] data)
        {
            return Post(Config.App.WebServer.Host, Config.App.WebServer.Port, Config.App.WebServer.ContextPath, Config.App.WebServer.Timeout, path, data);
        }
        public static byte[] Get(string path, string query)
        {
            HttpWebRequest request = null;
            WebResponse response = null;
            Stream responseStream = null;
            MemoryStream ms = null;

            try
            {
                UriBuilder builder = new UriBuilder("http", Config.App.WebServer.Host, Config.App.WebServer.Port);
                builder.Path = Config.App.WebServer.ContextPath + path;
                builder.Query = query;

                request = (HttpWebRequest)HttpWebRequest.Create(builder.Uri.AbsoluteUri);
                request.Timeout = Config.App.WebServer.Timeout;
                request.KeepAlive = false;
                request.Method = WebRequestMethods.Http.Get;
                request.ContentType = contentType;
                request.Credentials = new NetworkCredential(Config.App.WebServer.Username, Config.App.WebServer.Password);

                response = request.GetResponse();
                responseStream = response.GetResponseStream();

                int bufferSize = 2048;
                int readCount = 0;
                byte[] buffer = new byte[bufferSize];
                ms = new MemoryStream();

                while ((readCount = responseStream.Read(buffer, 0, bufferSize)) > 0)
                {
                    ms.Write(buffer, 0, readCount);
                }

                AppState.Online = true;
                return ms.ToArray();
            }
            catch (WebException e)
            {
                if (WebExceptionStatus.ConnectFailure == e.Status || WebExceptionStatus.Timeout == e.Status)
                {
                    AppState.Online = false;
                }

                throw;
            }
            finally
            {
                if (ms != null)
                {
                    ms.Close();
                }

                if (null != response)
                {
                    response.Close();
                }
            }
        }

        #region 物联网专用

        public static string IOTPost(string url, string jsonRequest)
        {
            string responseResult = String.Empty;
            HttpWebRequest httpRequest = null;
            try
            {
                //UriBuilder builder = new UriBuilder("https", ServerIP);这样默认带端口
                UriBuilder builder = new UriBuilder("https://10.252.252.252");
                builder.Path = url;
                SetCertificatePolicy(); //匿名访问【1】
                httpRequest = (HttpWebRequest)HttpWebRequest.Create(builder.Uri.AbsoluteUri);
                httpRequest.Timeout = 1000;
                httpRequest.Method = WebRequestMethods.Http.Post;
                httpRequest.ContentType = "application/json";
                //httpRequest.Credentials = new NetworkCredential("admin", "admin");//访问验证【2】暂时不成功

                using (var streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
                {
                    streamWriter.Write(jsonRequest);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
                HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    responseResult = streamReader.ReadToEnd();
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return responseResult;
        }

        public static string IOTPost(string url, string access_token, string jsonRequest)
        {
            string responseResult = String.Empty;
            HttpWebRequest httpRequest = null;
            try
            {
                //UriBuilder builder = new UriBuilder("https", ServerIP);这样默认带端口
                UriBuilder builder = new UriBuilder("https://10.252.252.252");
                builder.Path = url;
                builder.Query = $"access_token ={access_token}";
                SetCertificatePolicy(); //匿名访问【1】
                httpRequest = (HttpWebRequest)HttpWebRequest.Create(builder.Uri.AbsoluteUri);
                httpRequest.Timeout = 10000;
                httpRequest.Method = WebRequestMethods.Http.Post;
                httpRequest.ContentType = "application/json";
                //httpRequest.Credentials = new NetworkCredential("admin", "admin");//访问验证【2】暂时不成功

                using (var streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
                {
                    streamWriter.Write(jsonRequest);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
                HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    responseResult = streamReader.ReadToEnd();
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return responseResult;
        }

        #endregion
        public static void UploadFile(string path, string filename)
        {
            HttpWebRequest request = null;
            WebResponse response = null;
            Stream responseStream = null;
            Stream requestStream = null;
            FileStream fs = null;

            try
            {
                UriBuilder builder = new UriBuilder("http", Config.App.WebServer.Host, Config.App.WebServer.Port);
                builder.Path = Config.App.WebServer.ContextPath + path;

                request = (HttpWebRequest)HttpWebRequest.Create(builder.Uri.AbsoluteUri);
                request.Timeout = Config.App.WebServer.Timeout;
                request.KeepAlive = false;
                request.Method = WebRequestMethods.Http.Post;
                request.ContentType = contentType;
                request.Credentials = new NetworkCredential(Config.App.WebServer.Username, Config.App.WebServer.Password);

                fs = File.OpenRead(filename);
                int readCount = 0;
                int bufferSize = 2048;
                byte[] buffer = new byte[bufferSize];
                requestStream = request.GetRequestStream();

                while ((readCount = fs.Read(buffer, 0, bufferSize)) > 0)
                {
                    requestStream.Write(buffer, 0, readCount);
                }

                response = request.GetResponse();
                responseStream = response.GetResponseStream();
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }

                if (responseStream != null)
                {
                    responseStream.Close();
                }

                if (null != response)
                {
                    response.Close();
                }
            }
        }
        public static void DownloadFile(string host, int port, string contextPath, int timeout, string path, string filename)
        {
            HttpWebRequest request = null;
            WebResponse response = null;
            Stream responseStream = null;
            FileStream fs = null;

            try
            {
                UriBuilder builder = new UriBuilder("http", host, port);
                builder.Path = contextPath + path;

                request = (HttpWebRequest)HttpWebRequest.Create(builder.Uri.AbsoluteUri);
                request.Timeout = timeout;
                request.KeepAlive = false;
                request.Method = WebRequestMethods.Http.Get;
                request.ContentType = contentType;
                request.Credentials = new NetworkCredential(Config.App.WebServer.Username, Config.App.WebServer.Password);

                response = request.GetResponse();
                responseStream = response.GetResponseStream();

                int bufferSize = 2048;
                int readCount = 0;
                byte[] buffer = new byte[bufferSize];

                FileInfo fileInfo = new FileInfo(filename);

                if (fileInfo.Exists)
                {
                    fileInfo.Delete();
                }

                if (!fileInfo.Directory.Exists)
                {
                    fileInfo.Directory.Create();
                }

                fs = fileInfo.Create();

                while ((readCount = responseStream.Read(buffer, 0, bufferSize)) > 0)
                {
                    fs.Write(buffer, 0, readCount);
                }
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }

                if (null != response)
                {
                    response.Close();
                }
            }
        }
        public static void DownloadFile(string path, string filename)
        {
            DownloadFile(Config.App.WebServer.Host, Config.App.WebServer.Port, Config.App.WebServer.ContextPath, Config.App.WebServer.Timeout, path, filename);
        }

        #region 远程证书无效
        /// <summary> 
        /// Sets the cert policy. 
        /// </summary> 
        public static void SetCertificatePolicy()
        {
            ServicePointManager.ServerCertificateValidationCallback
                       += RemoteCertificateValidate;
        }

        /// <summary> 
        /// Remotes the certificate validate. 
        /// </summary> 
        private static bool RemoteCertificateValidate(
           object sender, X509Certificate cert,
            X509Chain chain, SslPolicyErrors error)
        {
            // trust any certificate!!! 
            return true;
        }
        #endregion

        #region 获取以太网ip
        private IPAddress GetEthernetIPAddress()
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in nics)
            {
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    foreach (var item in adapter.GetIPProperties().UnicastAddresses)
                    {
                        //item.IPv4Mask获取掩码
                        if (item.Address.AddressFamily == AddressFamily.InterNetwork)
                            return item.Address;            
                    }
                }
                //adapter.GetIPProperties().GatewayAddresses获取网关
            }
            throw new Exception("Ethernet not connected");
        }
        public static string GetEthernetIP()
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in nics)
            {
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    foreach (var item in adapter.GetIPProperties().UnicastAddresses)
                    {
                        if (item.Address.AddressFamily == AddressFamily.InterNetwork)
                            return item.Address.ToString();            //item.IPv4Mask获取掩码
                    }
                }
                //adapter.GetIPProperties().GatewayAddresses获取网关
            }
            throw new Exception("Ethernet not connected");
        }
        #endregion
    }
}
