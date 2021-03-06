﻿using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace RichTea.WebCache
{
    /// <summary>
    /// Web cache.
    /// </summary>
    public class WebCache : IDisposable
    {
        /// <summary>
        /// Gets or sets the default cache name for all WebCaches.
        /// </summary>
        public static string DefaultCacheName { get; set; } = "RichTea.WebCache";

        /// <summary>
        /// Gets or sets the default cache pat for all WebCaches.
        /// </summary>
        public static string DefaultCachePath { get; set; } = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), DefaultCacheName);

        /// <summary>
        /// HTTP client.
        /// </summary>
        private readonly HttpClient httpClient = new HttpClient();

        /// <summary>
        /// Logger. Might be null.
        /// </summary>
        private readonly ILogger<WebCache> logger;

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
        /// Gets or sets how many download attempts occur before failing.
        /// </summary>
        public int DownloadAttempts { get; set; } = 10;

        private long _bytesDownloaded;

        /// <summary>
        /// Gets total number of bytes downloaded by this cache.
        /// </summary>
        public long BytesDownloaded { get { return _bytesDownloaded; } }

        private long _downloadTimeSpan;

        /// <summary>
        /// Gets the total time in milliseconds that downloads have been happening for.
        /// </summary>
        public long DownloadTimeSpan { get { return _downloadTimeSpan; } }

        /// <summary>
        /// Gets or sets rate limit. Null rate limit is regarded as unlimited rates.
        /// </summary>
        public RateLimit RateLimit { get; set; }

        /// <summary>
        /// Gets download speed in kB/s.
        /// </summary>
        public int DownloadSpeed
        {
            get
            {
                int result = 0;
                if (BytesDownloaded != 0 && DownloadTimeSpan != 0)
                {
                    result = (int)(BytesDownloaded / DownloadTimeSpan);
                }
                return result;
            }
        }

        /// <summary>
        /// Gets or sets the default cache expiry if one isn't specified. Defaults to 30 days.
        /// </summary>
        public TimeSpan DefaultCacheExpiry { get; set; } = TimeSpan.FromDays(30);

        /// <summary>
        /// Creates a cache with the given name and a null logger.
        /// </summary>
        /// <param name="cacheName"></param>
        /// <returns></returns>
        public static WebCache CreateWebCache(string cacheName)
        {
            return new WebCache(null, cacheName, Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), cacheName));
        }

        /// <summary>
        /// Constructs a webcache.
        /// </summary>
        /// <param name="logger">Logger.</param>
        public WebCache(ILogger<WebCache> logger) : this(logger, DefaultCacheName, DefaultCachePath) { }

        /// <summary>
        /// Constructs a webcache.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="cacheName">Cache name.</param>
        /// <param name="cachePath">Cache path.</param>
        protected WebCache(ILogger<WebCache> logger, string cacheName, string cachePath)
        {
            CacheName = cacheName;
            CachePath = cachePath;

            var version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            UserAgent = UserAgent.Replace(Constants.VersionToken, version);

            this.logger = logger;
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
            return await GetWebPageAsync(url, DateTimeOffset.Now - DefaultCacheExpiry);
        }

        /// <summary>
        /// Gets web document from a URL.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <param name="expiryDate">Expiry date. A resource older than this will be refetched.</param>
        /// <returns>Web document</returns>
        public async Task<WebDocument> GetWebPageAsync(string url, DateTimeOffset expiryDate)
        {
            var binary = await GetWebResourceAsync(url);
            var webDocument = new WebDocument(url, binary);
            return webDocument;
        }

        /// <summary>
        /// Gets byte array from the url.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <returns>Byte array.</returns>
        public async Task<byte[]> GetWebResourceAsync(string url)
        {
            var bytes = await GetWebResourceAsync(url, DateTimeOffset.Now - DefaultCacheExpiry);
            return bytes;
        }

        /// <summary>
        /// Gets byte array from the url.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <param name="expiryDate">Expiry date. A resource older than this will be refetched.</param>
        /// <returns>Byte array.</returns>
        public async Task<byte[]> GetWebResourceAsync(string url, DateTimeOffset expiryDate)
        {
            logger?.LogInformation($"Getting resource for {url}");
            var result = GetResourceFromCache(url);
            var cacheDate = GetDateOfCachedResource(url);
            if (cacheDate != null && cacheDate < expiryDate)
            {
                logger?.LogDebug($"{url} has an expired cache.");
                DeleteResourceFromCache(url);
                result = null;
            }

            if (result == null)
            {
                int attempts = 0;
                while (attempts < DownloadAttempts)
                {
                    logger?.LogDebug($"Download attempt {attempts} for {url}.");
                    try
                    {
                        while (_concurrentDownloads > MaxConcurrentDownloads)
                        {
                            await Task.Delay(500);
                        }

                        if (RateLimit != null)
                        {
                            while (RateLimit.IsThrottled())
                            {
                                // purposefully tie up the thread so others don't continue to hammer the endpoint.
                                Thread.Sleep(1000);
                            }
                        }

                        long startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                        Interlocked.Increment(ref _concurrentDownloads);
                        using (var client = new CrawlerClient() { Timeout = 10 * 60 * 1000 })
                        {
                            client.Headers.Add("User-Agent", UserAgent);
                            var encodedUrl = new Uri(url).AbsoluteUri;
                            result = await client.DownloadDataTaskAsync(encodedUrl);
                            RateLimit?.AddRequest();
                        }
                        long endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        long interval = endTime - startTime;

                        Interlocked.Add(ref _downloadTimeSpan, interval);

                        Interlocked.Add(ref _bytesDownloaded, result.LongLength);

                        logger?.LogDebug($"Downloaded resource for {url}.");
                        SaveResourceToCache(url, result);
                        Interlocked.Increment(ref _cacheMisses);
                        break;
                    }
                    catch (WebException ex)
                    {
                        if (ex.Status == WebExceptionStatus.NameResolutionFailure || ex.Status == WebExceptionStatus.Timeout)
                        {
                            attempts++;
                            if (attempts < DownloadAttempts)
                            {
                                await Task.Delay(1000);
                            }
                            else
                            {
                                logger?.LogWarning(ex, $"Exception downloading web page from url '{url}'");
                            }
                        }
                        else
                        {
                            throw ex;
                        }
                    }
                    finally
                    {
                        Interlocked.Decrement(ref _concurrentDownloads);
                    }
                }
            }
            else
            {
                Interlocked.Increment(ref _cacheHits);
            }
            return result;
        }

        /// <summary>
        /// Gets resource from the cache.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <returns>Byte array.</returns>
        protected byte[] GetResourceFromCache(string url)
        {
            byte[] binary = null;
            var cachePath = GetCachedFilePath(url);
            if (File.Exists(cachePath))
            {
                try
                {
                    binary = File.ReadAllBytes(cachePath);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, $"Cannot read '{cachePath}' from cache.");
                }
            }
            return binary;
        }

        /// <summary>
        /// Gets the last modified date of a cached resource. Null if the resource is not cached.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <returns>DateTimeOffset</returns>
        public DateTimeOffset? GetDateOfCachedResource(string url)
        {
            var cachePath = GetCachedFilePath(url);
            if (File.Exists(cachePath))
            {
                return new DateTimeOffset(File.GetLastWriteTime(cachePath));
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Deletes resource from the cache.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <returns>Byte array.</returns>
        protected void DeleteResourceFromCache(string url)
        {
            logger?.LogInformation($"Deleting cache for {url}.");
            var cachePath = GetCachedFilePath(url);
            if (File.Exists(cachePath))
            {
                try
                {
                    File.Delete(cachePath);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, $"Could not delete file: {cachePath}");
                }
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

        /// <summary>
        /// Deletes all files in the cache.
        /// </summary>
        public void CleanCache()
        {
            logger?.LogInformation("Deleting entire cache.");
            if (Directory.Exists(CachePath))
            {
                Directory.Delete(CachePath, true);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Disposes.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    httpClient.Dispose();
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Disposes.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion

    }
}
