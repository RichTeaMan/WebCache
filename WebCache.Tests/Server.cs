using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace RichTea.WebCache.Test
{
    public class Server : IDisposable
    {
        public int Port { get; private set; }

        private HttpListener httpListener;

        public Dictionary<string, Response> Responses { get; private set; } = new Dictionary<string, Response>();

        private ConcurrentBag<HttpListenerRequest> _requestUriList = new ConcurrentBag<HttpListenerRequest>();

        public IReadOnlyList<HttpListenerRequest> RequestUriList { get { return _requestUriList.ToArray(); } }

        public Response Default404Response { get; } = new TextResponse("Not Found", 404, DateTimeOffset.MinValue);

        public Server(int port)
        {
            Port = port;
            httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://localhost:{Port}/");
            httpListener.Start();
            httpListener.BeginGetContext(new AsyncCallback(ListenerCallback), this);
        }

        /// <summary>
        /// Callback for async server. Adds context to stack and readies server for another exchange.
        /// </summary>
        /// <param name="result"></param>
        private void ListenerCallback(IAsyncResult result)
        {
            if (disposedValue)
            {
                return;
            }
            try
            {
                // Call EndGetContext to complete the asynchronous operation.
                HttpListenerContext httpListenerContext = httpListener?.EndGetContext(result);
                if (httpListenerContext == null)
                {
                    return;
                }
                HttpListenerRequest request = httpListenerContext.Request;

                string body = null;
                using (StreamReader reader = new StreamReader(request.InputStream))
                {
                    body = reader.ReadToEnd();
                }

                _requestUriList.Add(request);

                Response testResponse;
                if (!Responses.TryGetValue(request.Url.ToString(), out testResponse))
                {
                    testResponse = Default404Response;
                }

                using (var response = httpListenerContext.Response)
                using (var outputStream = response.OutputStream)
                {
                    byte[] buffer = new byte[0];

                    if (testResponse?.ResponseData?.Length > 0)
                    {
                        buffer = testResponse.ResponseData;
                    }
                    response.ContentLength64 = buffer.Length;
                    response.StatusCode = testResponse.StatusCode;
                    outputStream.Write(buffer, 0, buffer.Length);
                    Console.WriteLine("sending response");
                }

            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 995)
            {
                // System.Net.HttpListenerException: The I/ O operation has been aborted because of either a thread exit or an application request
                // This appears to happen if a socket is closed, it seems safe to carry on and begin listening again.
            }
            finally
            {
                var callBack = new AsyncCallback(ListenerCallback);
                if (!disposedValue)
                {
                    httpListener?.BeginGetContext(callBack, this);
                }
            }

        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Disposes the server.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                var _httpListener = httpListener;
                httpListener = null;
                if (disposing)
                {
                    _httpListener?.Stop();
                    _httpListener?.Close();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        /// <summary>
        /// Disposes the server.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
