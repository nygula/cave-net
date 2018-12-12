using System;
using System.IO;
using System.Net;
using System.Text;
using Cave.IO;

namespace Cave.Net
{
    /// <summary>
    /// Provides a simple asynchronous http fetch
    /// </summary>
    public sealed class FtpConnection
    {
        /// <summary>
        /// Directly obtains the data of the file represented by the specified connectionstring
        /// </summary>
        /// <param name="connectionString">The full connectionstring for the download</param>
        /// <returns></returns>
        public static byte[] Get(ConnectionString connectionString)
        {
            FtpConnection connection = new FtpConnection();
            return connection.Download(connectionString);
        }

        /// <summary>
        /// Directly obtains the data of the specified fileName by using the specified connectionstring as string
        /// </summary>
        /// <param name="connectionString">The full connectionstring for the download</param>
        /// <returns></returns>
        public static string GetString(ConnectionString connectionString)
        {
            return Encoding.UTF8.GetString(Get(connectionString));
        }

        #region private functionality
        bool m_EnableSSL = false;

        FtpWebRequest m_CreateRequest(string method, ConnectionString connectionString)
        {
            Uri l_Uri = connectionString.ToUri();
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(l_Uri);
            request.Credentials = connectionString.GetCredentials();
            request.EnableSsl = EnableSSL;
            request.Method = method;
            return request;
        }
        #endregion

        /// <summary>
        /// Enable SSL for ftp access
        /// </summary>
        public bool EnableSSL { get => m_EnableSSL; set => m_EnableSSL = value; }

        /// <summary>
        /// Creates a new ftp connection.
        /// </summary>
        public FtpConnection() { }

        /// <summary>
        /// Obtains a list of files and directories at the current path
        /// </summary>
        /// <param name="connectionString">The full connectionstring for the path to list</param>
        /// <returns></returns>
        public string[] List(ConnectionString connectionString)
        {
            FtpWebResponse response = null;
            try
            {
                FtpWebRequest request = m_CreateRequest(WebRequestMethods.Ftp.ListDirectory, connectionString);
                response = (FtpWebResponse)request.GetResponse();
                switch (response.StatusCode)
                {
                    case FtpStatusCode.RestartMarker:
                    case FtpStatusCode.DataAlreadyOpen:
                    case FtpStatusCode.OpeningData:
                        break;

                    default:
                        throw new NetworkException(string.Format("Ftp error status: {0} message: '{1}'", response.StatusCode, response.StatusDescription));
                }
                StreamReader reader = new StreamReader(response.GetResponseStream());
                string[] result = reader.ReadToEnd().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                reader.Close();
                if (response.StatusCode == FtpStatusCode.ClosingData)
                {
                    return result;
                }
                throw new NetworkException(string.Format("Ftp error status: {0} message: '{1}'", response.StatusCode, response.StatusDescription));
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }
        }

        /// <summary>
        /// Uploads a file
        /// </summary>
        /// <param name="connectionString">The full connectionstring for the upload</param>
        /// <param name="data"></param>
        public void Upload(ConnectionString connectionString, byte[] data)
        {
            FtpWebResponse response = null;
            try
            {
                FtpWebRequest request = m_CreateRequest(WebRequestMethods.Ftp.UploadFile, connectionString);
                DataWriter writer = new DataWriter(request.GetRequestStream());
                writer.Write(data);
                response = (FtpWebResponse)request.GetResponse();
                if (response.StatusCode != FtpStatusCode.ClosingData)
                {
                    throw new NetworkException(string.Format("Ftp error status: {0} message: '{1}'", response.StatusCode, response.StatusDescription));
                }
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }
        }

        /// <summary>
        /// Uploads a file
        /// </summary>
        /// <param name="connectionString">The full connectionstring for the upload</param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public long Upload(ConnectionString connectionString, Stream stream)
        {
            FtpWebResponse response = null;
            try
            {
                FtpWebRequest request = m_CreateRequest(WebRequestMethods.Ftp.UploadFile, connectionString);
                Stream requestStream = request.GetRequestStream();
                long result = stream.CopyBlocksTo(requestStream);
                requestStream.Close();
                response = (FtpWebResponse)request.GetResponse();
                if (response.StatusCode != FtpStatusCode.ClosingData)
                {
                    throw new NetworkException(string.Format("Ftp error status: {0} message: '{1}'", response.StatusCode, response.StatusDescription));
                }
                return result;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }
        }

        /// <summary>
        /// Downloads a file
        /// </summary>
        /// <param name="connectionString">The full connectionstring for the download</param>
        /// <returns></returns>
        public byte[] Download(ConnectionString connectionString)
        {
            FtpWebResponse response = null;
            try
            {
                FtpWebRequest request = m_CreateRequest(WebRequestMethods.Ftp.DownloadFile, connectionString);
                response = (FtpWebResponse)request.GetResponse();
                switch (response.StatusCode)
                {
                    case FtpStatusCode.DataAlreadyOpen:
                    case FtpStatusCode.OpeningData:
                        break;

                    default:
                        throw new NetworkException(string.Format("Ftp error status: {0} message: '{1}'", response.StatusCode, response.StatusDescription));
                }
                Stream responseStream = response.GetResponseStream();
                byte[] result = responseStream.ReadAllBytes();
                responseStream.Close();
                if (response.StatusCode == FtpStatusCode.ClosingData)
                {
                    return result;
                }
                throw new NetworkException(string.Format("Ftp error status: {0} message: '{1}'", response.StatusCode, response.StatusDescription));
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }
        }

        /// <summary>
        /// Downloads a file
        /// </summary>
        /// <param name="connectionString">The full connectionstring for the download</param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public long Download(ConnectionString connectionString, Stream stream)
        {
            FtpWebResponse response = null;
            try
            {
                FtpWebRequest request = m_CreateRequest(WebRequestMethods.Ftp.DownloadFile, connectionString);
                response = (FtpWebResponse)request.GetResponse();
                switch (response.StatusCode)
                {
                    case FtpStatusCode.DataAlreadyOpen:
                    case FtpStatusCode.OpeningData:
                        break;

                    default:
                        throw new NetworkException(string.Format("Ftp error status: {0} message: '{1}'", response.StatusCode, response.StatusDescription));
                }
                Stream responseStream = response.GetResponseStream();
                long size = responseStream.CopyBlocksTo(stream);
                responseStream.Close();
                if (response.StatusCode == FtpStatusCode.ClosingData)
                {
                    return size;
                }
                throw new NetworkException(string.Format("Ftp error status: {0} message: '{1}'", response.StatusCode, response.StatusDescription));
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }
        }
    }
}