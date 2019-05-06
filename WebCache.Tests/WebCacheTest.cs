using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RichTea.WebCache.Test
{
    [TestClass]
    public class WebCacheTest
    {
        private Server server;

        private int port = 9000;

        private WebCache webCache;

        [TestInitialize]
        public void Setup()
        {
            server = new Server(port);
            webCache = WebCache.CreateWebCache("test");
            webCache.CleanCache();
        }

        [TestCleanup]
        public void Cleanup()
        {
            server?.Dispose();
            server = null;

            webCache.CleanCache();
        }

        [TestMethod] 
        public async Task BasicWebTest()
        {
            string webText = "hello-world";
            server.Responses.Add($"http://localhost:{port}/test", new TextResponse(webText, 200));

            var response = await webCache.GetWebPageAsync($"http://localhost:{port}/test");

            Assert.AreEqual(webText, response.GetContents());
        }

        [TestMethod]
        public async Task EncodingWebTest()
        {
            string webText1 = "hello-world1";
            server.Responses.Add($"http://localhost:{port}/test with space", new TextResponse(webText1, 200));

            string webText2 = "hello-world2";
            server.Responses.Add($"http://localhost:{port}/nested path/test with space", new TextResponse(webText2, 200));

            var response1 = await webCache.GetWebPageAsync($"http://localhost:{port}/test with space");
            var response2 = await webCache.GetWebPageAsync($"http://localhost:{port}/nested path/test with space");

            Assert.AreEqual(webText1, response1.GetContents());
            Assert.AreEqual(webText2, response2.GetContents());
        }

        [TestMethod]
        public async Task ParallelWebTest()
        {
            int requestsToMake = 100;

            foreach (var i in Enumerable.Range(0, requestsToMake))
            {
                string url = $"http://localhost:{port}/test/{i}";
                server.Responses.Add(url, new TextResponse(url, 200));
            }

            var tasks = Enumerable.Range(0, requestsToMake).Select(i =>
                webCache.GetWebPageAsync($"http://localhost:{port}/test/{i}"));

            var webDocuments = await Task.WhenAll(tasks);
            var urlList = webDocuments.Select(d => d.Url).ToArray();
            var contentList = webDocuments.Select(d => d.GetContents()).ToArray();
           
            Assert.AreEqual(requestsToMake, server.RequestUriList.Count);
            CollectionAssert.AreEqual(urlList, contentList);
            
        }

        [TestMethod]
        public async Task CacheDateTest()
        {
            string webText = "test";
            server.Responses.Add($"http://localhost:{port}/dateTest", new TextResponse(webText, 200));

            await webCache.GetWebPageAsync($"http://localhost:{port}/dateTest");
            var cachedDate = webCache.GetDateOfCachedResource($"http://localhost:{port}/dateTest");

            Assert.IsNotNull(cachedDate);
            Assert.IsTrue(cachedDate > DateTime.Now.AddSeconds(-1) && cachedDate < DateTime.Now.AddSeconds(1));
        }

        [TestMethod]
        public void NullCacheDateTest()
        {
            // this path does not exist
            var cachedDate = webCache.GetDateOfCachedResource($"http://localhost:{port}/dateTest2");

            Assert.IsNull(cachedDate);
        }

        [TestMethod]
        public async Task CacheExpiryTest()
        {
            // setup
            string remoteUrl = $"http://localhost:{port}/cacheExpiry";
            string webText = "hello-world";
            string updatedWebContext = "hello-world-updated";
            server.Responses.Add(remoteUrl, new TextResponse(webText, 200));

            await webCache.GetWebPageAsync(remoteUrl);
            var cachedFilepath = webCache.GetCachedFilePath(remoteUrl);
            File.SetLastWriteTime(cachedFilepath, DateTime.Now.AddYears(-1));

            server.Responses[remoteUrl] = new TextResponse(updatedWebContext, 200);

            // test
            var updatedResponse = await webCache.GetWebPageAsync(remoteUrl, DateTime.Now.AddDays(-1));

            Assert.AreEqual(updatedWebContext, updatedResponse.GetContents());
        }
    }
}
