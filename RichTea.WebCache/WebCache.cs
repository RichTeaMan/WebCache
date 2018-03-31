using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RichTea.WebCache
{
    public class WebCache
    {
        public delegate void MessageEventHandler(WebCache sendor, Message message);

        public event MessageEventHandler Message;

        public string CacheName { get; private set; }

        #region Messages

        protected void FireMessage(Message message)
        {
            var _m = Message;
            if (_m != null)
            {
                _m(this, message);
            }
        }

        protected void FireMessage(Severity severity, string text, params object[] args)
        {
            var message = new Message(severity, text, args);
            FireMessage(message);
        }

        protected void FireTrace(string text, params object[] args)
        {
            FireMessage(Severity.Trace, text, args);
        }

        protected void FireInfo(string text, params object[] args)
        {
            FireMessage(Severity.Info, text, args);
        }

        protected void FireError(string text, params object[] args)
        {
            FireMessage(Severity.Error, text, args);
        }

        protected void FireFatal(string text, params object[] args)
        {
            FireMessage(Severity.Fatal, text, args);
        }

        protected void FireExceptionMessage(Severity severity, Exception exception, string text, params object[] args)
        {
            var message = new ExceptionMessage(severity, exception, text, args);
            FireMessage(message);
        }

        protected void FireErrorExceptionMessage(Exception exception, string text, params object[] args)
        {
            FireExceptionMessage(Severity.Error, exception, text, args);
        }

        protected void FireFatalExceptionMessage(Exception exception, string text, params object[] args)
        {
            FireExceptionMessage(Severity.Fatal, exception, text, args);
        }

        #endregion

        public WebCache(string cacheName)
        {
            CacheName = cacheName;
        }

        protected virtual GeneratorException GetGeneratorException(string value, Exception innerException = null)
        {
            var message = string.Format("Error creating object. Could not get {0}.", value);
            var ex = new GeneratorException(message, innerException);
            return ex;
        }

        public string GetCachePath(string url)
        {
            var pathName = Uri.EscapeDataString(url);

            var cachePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                CacheName,
                pathName);

            return cachePath;
        }

        public WebDocument GetWebPage(string url)
        {
            var binary = GetBinaryFromCache(url);
            if (binary == null)
            {
                binary = DownloadWebPage(url);
                SaveBinaryToCache(url, binary);
            }
            var webDocument = new WebDocument(url, binary);
            return webDocument;
        }

        private byte[] DownloadWebPage(string url)
        {
            try
            {
                byte[] result;
                using (var client = new CrawlerClient() { Timeout = 10 * 60 * 1000 })
                {
                    result = client.DownloadData(url);
                }
                return result;
            }
            catch (WebException ex)
            {
                Console.WriteLine("Exception downloading web page from url '{0}'.\n{1}", url, ex);
                throw ex;
            }
        }

        protected byte[] GetBinaryFromCache(string url)
        {
            byte[] binary = null;
            var cachePath = GetCachePath(url);
            if (File.Exists(cachePath))
            {
                binary = File.ReadAllBytes(url);
            }
            return binary;
        }

        protected void SaveBinaryToCache(string url, byte[] binary)
        {
            var cachePath = GetCachePath(url);
            var dirPath = Path.GetDirectoryName(cachePath);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            File.WriteAllBytes(cachePath, binary);
        }

        protected byte[] GetWebResource(string url)
        {
            var result = GetResourceFromCache(url);
            if (result == null)
            {
                using (var client = new CrawlerClient() { Timeout = 10 * 60 * 1000 })
                {
                    result = client.DownloadData(url);
                }
                SaveResourceToCache(url, result);
            }
            return result;
        }

        protected byte[] GetResourceFromCache(string url)
        {
            var cachePath = GetCachePath(url);
            if (File.Exists(cachePath))
            {
                return File.ReadAllBytes(cachePath);
            }
            else
            {
                return null;
            }
        }

        protected void SaveResourceToCache(string url, byte[] binary)
        {
            var cachePath = GetCachePath(url);
            var dirPath = Path.GetDirectoryName(cachePath);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            File.WriteAllBytes(cachePath, binary);
        }

        protected string Unescape(string input)
        {
            return input.Replace("&nbsp;", "").Replace("&#160;", "").Replace("&#8212;", "-").Trim();
        }

        protected string RemoveHtml(string input)
        {
            var htmlRegex = new Regex("<[^>]+>");
            var cleaned = htmlRegex.Replace(input, "");
            return cleaned;
        }

    }
}
