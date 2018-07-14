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

        public Response Default404Response { get; } = new TextResponse("Not Found", 404);

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
                HttpListenerContext httpListenerContext = httpListener.EndGetContext(result);
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
            finally
            {
                httpListener.BeginGetContext(new AsyncCallback(ListenerCallback), this);
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
                if (disposing)
                {
                    httpListener?.Close();
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
