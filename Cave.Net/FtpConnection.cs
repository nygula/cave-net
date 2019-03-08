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
        /// <returns>Returns an array of byte.</returns>
        public static byte[] Get(ConnectionString connectionString)
        {
            var connection = new FtpConnection();
            return connection.Download(connectionString);
        }

        /// <summary>
        /// Directly obtains the data of the specified fileName by using the specified connectionstring as string
        /// </summary>
        /// <param name="connectionString">The full connectionstring for the download</param>
        /// <returns>Returns the downloaded data as string (utf8).</returns>
        public static string GetString(ConnectionString connectionString)
        {
            return Encoding.UTF8.GetString(Get(connectionString));
        }

        #region private functionality

        FtpWebRequest CreateRequest(string method, ConnectionString connectionString)
        {
            var l_Uri = connectionString.ToUri();
            var request = (FtpWebRequest)WebRequest.Create(l_Uri);
            request.Credentials = connectionString.GetCredentials();
            request.EnableSsl = EnableSSL;
            request.Method = method;
            return request;
        }
        #endregion

        /// <summary>
        /// Gets or sets a value indicating whether SSL is enabled for ftp access or not.
        /// </summary>
        public bool EnableSSL { get; set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpConnection"/> class.
        /// </summary>
        public FtpConnection()
        {
        }

        /// <summary>
        /// Obtains a list of files and directories at the current path
        /// </summary>
        /// <param name="connectionString">The full connectionstring for the path to list</param>
        /// <returns>Returns a new array of strings. This is may be empty but is never null.</returns>
        public string[] List(ConnectionString connectionString)
        {
            FtpWebResponse response = null;
            try
            {
                FtpWebRequest request = CreateRequest(WebRequestMethods.Ftp.ListDirectory, connectionString);
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
                var reader = new StreamReader(response.GetResponseStream());
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
        /// <param name="data">Byte array to upload.</param>
        public void Upload(ConnectionString connectionString, byte[] data)
        {
            FtpWebResponse response = null;
            try
            {
                FtpWebRequest request = CreateRequest(WebRequestMethods.Ftp.UploadFile, connectionString);
                var writer = new DataWriter(request.GetRequestStream());
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
        /// <param name="stream">The stream to upload.</param>
        /// <returns>Returns the number of bytes uploaded.</returns>
        public long Upload(ConnectionString connectionString, Stream stream)
        {
            FtpWebResponse response = null;
            try
            {
                FtpWebRequest request = CreateRequest(WebRequestMethods.Ftp.UploadFile, connectionString);
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
        /// <returns>Returns an array of bytes.</returns>
        public byte[] Download(ConnectionString connectionString)
        {
            FtpWebResponse response = null;
            try
            {
                FtpWebRequest request = CreateRequest(WebRequestMethods.Ftp.DownloadFile, connectionString);
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
        /// <param name="stream">Target stream to download to.</param>
        /// <returns>Returns the number of bytes downloaded.</returns>
        public long Download(ConnectionString connectionString, Stream stream)
        {
            FtpWebResponse response = null;
            try
            {
                FtpWebRequest request = CreateRequest(WebRequestMethods.Ftp.DownloadFile, connectionString);
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
