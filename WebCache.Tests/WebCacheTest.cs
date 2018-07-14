using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace RichTea.WebCache.Test
{
    [TestClass]
    public class WebCacheTest
    {
        private Server server;

        private int port = 5000;

        private WebCache webCache;

        [TestInitialize]
        public void Setup()
        {
            server = new Server(port);
            webCache = new WebCache("test");
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
            server.Responses.Add("http://localhost:5000/test", new TextResponse(webText, 200));

            var response = await webCache.GetWebPageAsync("http://localhost:5000/test");

            Assert.AreEqual(webText, response.GetContents());
        }

        [TestMethod]
        public async Task ParallelWebTest()
        {
            int requestsToMake = 100;

            foreach (var i in Enumerable.Range(0, requestsToMake))
            {
                string url = $"http://localhost:5000/test/{i}";
                server.Responses.Add(url, new TextResponse(url, 200));
            }

            var tasks = Enumerable.Range(0, requestsToMake).Select(i =>
                webCache.GetWebPageAsync($"http://localhost:5000/test/{i}"));

            var webDocuments = await Task.WhenAll(tasks);
            var urlList = webDocuments.Select(d => d.Url).ToArray();
            var contentList = webDocuments.Select(d => d.GetContents()).ToArray();
           
            Assert.AreEqual(requestsToMake, server.RequestUriList.Count);
            CollectionAssert.AreEqual(urlList, contentList);
            
        }
    }
}
