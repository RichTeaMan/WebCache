using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RichTea.WebCache
{
    public class WebCache
    {
        public delegate void MessageEventHandler(WebCache sendor, Message message);

        public event MessageEventHandler Message;

        public string CachePath { get; private set; }

        public string CacheName { get; private set; }

        public string UserAgent { get; set; } = $"RichTea.WebCache/{Constants.VersionToken}";

        private int _cacheMisses = 0;

        public int CacheMisses { get { return _cacheMisses; } }

        private int _cacheHits = 0;

        public int CacheHits { get { return _cacheHits; } }

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

            CachePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                CacheName);

            var version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            UserAgent = UserAgent.Replace(Constants.VersionToken, version);
        }

        protected virtual GeneratorException GetGeneratorException(string value, Exception innerException = null)
        {
            var message = string.Format("Error creating object. Could not get {0}.", value);
            var ex = new GeneratorException(message, innerException);
            return ex;
        }

        public string GetCachedFilePath(string url)
        {
            var pathName = Uri.EscapeDataString(url);

            var cachePath = Path.Combine(CachePath,
                pathName);

            return cachePath;
        }

        public WebDocument GetWebPage(string url)
        {
            var binary = GetBinaryFromCache(url);
            if (binary == null)
            {
                binary = GetWebResource(url);
                SaveBinaryToCache(url, binary);
            }
            var webDocument = new WebDocument(url, binary);
            return webDocument;
        }

        protected byte[] GetBinaryFromCache(string url)
        {
            byte[] binary = null;
            var cachePath = GetCachedFilePath(url);
            if (File.Exists(cachePath))
            {
                try
                {
                    binary = File.ReadAllBytes(cachePath);
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Cannot read '{cachePath}' from cache.");
                    Console.WriteLine(ex);
                }
            }
            return binary;
        }

        protected void SaveBinaryToCache(string url, byte[] binary)
        {
            var cachePath = GetCachedFilePath(url);
            var dirPath = Path.GetDirectoryName(cachePath);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            File.WriteAllBytes(cachePath, binary);
        }

        public byte[] GetWebResource(string url)
        {
            var result = GetResourceFromCache(url);
            if (result == null)
            {
                try
                {
                    using (var client = new CrawlerClient() { Timeout = 10 * 60 * 1000 })
                    {
                        client.Headers.Add("User-Agent", UserAgent);
                        result = client.DownloadData(url);
                    }
                    SaveResourceToCache(url, result);
                    Interlocked.Increment(ref _cacheMisses);
                }
                catch (WebException ex)
                {
                    Console.WriteLine("Exception downloading web page from url '{0}'.\n{1}", url, ex);
                }
            } else
            {
                Interlocked.Increment(ref _cacheHits);
            }
            return result;
        }

        protected byte[] GetResourceFromCache(string url)
        {
            var cachePath = GetCachedFilePath(url);
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
            var cachePath = GetCachedFilePath(url);
            var dirPath = Path.GetDirectoryName(cachePath);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            File.WriteAllBytes(cachePath, binary);
        }

        public int CountCachedFiles()
        {
            var files = Directory.GetFiles(CachePath);
            return files.Count();
        }

    }
}
