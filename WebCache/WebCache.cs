using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace RichTea.WebCache
{
    /// <summary>
    /// Web cache.
    /// </summary>
    public class WebCache
    {

        /// <summary>
        /// Gets the cache directory path.
        /// </summary>
        public string CachePath { get; private set; }

        /// <summary>
        /// Gets the name of the cache.
        /// </summary>
        public string CacheName { get; private set; }

        /// <summary>
        /// Gets or sets the user agent string.
        /// </summary>
        public string UserAgent { get; set; } = $"RichTea.WebCache/{Constants.VersionToken}";

        private int _cacheMisses = 0;

        /// <summary>
        /// Gets cache misses that have result in a web request.
        /// </summary>
        public int CacheMisses { get { return _cacheMisses; } }

        private int _cacheHits = 0;

        /// <summary>
        /// Gets cache hits that did not result in a web request.
        /// </summary>
        public int CacheHits { get { return _cacheHits; } }

        private int _concurrentDownloads = 0;

        /// <summary>
        /// Gets the number of downloads currently occuring.
        /// </summary>
        public int ConcurrentDownloads { get { return _concurrentDownloads; } }

        /// <summary>
        /// Gets or sets the maximum number of concurrent allowed to occur.
        /// </summary>
        public int MaxConcurrentDownloads { get; set; } = 5;
        
        /// <summary>
        /// Constructs a webcache.
        /// </summary>
        /// <param name="cacheName">Cache name.</param>
        public WebCache(string cacheName)
        {
            CacheName = cacheName;

            CachePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                CacheName);

            var version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            UserAgent = UserAgent.Replace(Constants.VersionToken, version);
        }

        /// <summary>
        /// Gets the path of the url in the cache. The file may or may not exist.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <returns>File path.</returns>
        public string GetCachedFilePath(string url)
        {
            var pathName = Uri.EscapeDataString(url);

            var cachePath = Path.Combine(CachePath,
                pathName);

            return cachePath;
        }

        /// <summary>
        /// Gets web document from a URL.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <returns>Web document</returns>
        public async Task<WebDocument> GetWebPageAsync(string url)
        {
            var binary = GetBinaryFromCache(url);
            if (binary == null)
            {
                binary = await GetWebResourceAsync(url);
                SaveBinaryToCache(url, binary);
            }
            var webDocument = new WebDocument(url, binary);
            return webDocument;
        }
        
        /// <summary>
        /// Gets byte array of cached object from the given URL.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <returns>Byte array.</returns>
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

        /// <summary>
        /// Save byte array to cache.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="binary"></param>
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

        /// <summary>
        /// Gets byte array from the url.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <returns>Byte array.</returns>
        public async Task<byte[]> GetWebResourceAsync(string url)
        {
            var result = GetResourceFromCache(url);
            if (result == null)
            {
                try
                {
                    while (_concurrentDownloads > MaxConcurrentDownloads)
                    {
                        await Task.Delay(500);
                    }
                    Interlocked.Increment(ref _concurrentDownloads);
                    using (var client = new CrawlerClient() { Timeout = 10 * 60 * 1000 })
                    {
                        client.Headers.Add("User-Agent", UserAgent);
                        result = await client.DownloadDataTaskAsync(url);
                    }
                    SaveResourceToCache(url, result);
                    Interlocked.Increment(ref _cacheMisses);
                }
                catch (WebException ex)
                {
                    Console.WriteLine("Exception downloading web page from url '{0}'.\n{1}", url, ex);
                }
                finally
                {
                    Interlocked.Decrement(ref _concurrentDownloads);
                }
            } else
            {
                Interlocked.Increment(ref _cacheHits);
            }
            return result;
        }

        /// <summary>
        /// Gets resource ffrom the cache.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <returns>Byte array.</returns>
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

        /// <summary>
        /// Saves resource to cache.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <param name="binary">Byte array.</param>
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

        /// <summary>
        /// Counts the files in the cache directory.
        /// </summary>
        /// <returns></returns>
        public int CountCachedFiles()
        {
            var files = Directory.GetFiles(CachePath);
            return files.Count();
        }

    }
}
