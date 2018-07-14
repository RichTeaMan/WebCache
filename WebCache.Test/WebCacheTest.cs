using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        }

        [TestCleanup]
        public void Cleanup()
        {
            server?.Dispose();
            server = null;
        }

        [TestMethod]
        public async Task BasicWebTest()
        {
            string webText = "hello-world";
            server.Responses.Add("http://localhost:5000/test", new TextResponse(webText, 200));

            var response = await webCache.GetWebPageAsync("http://localhost:5000/test");

            Assert.AreEqual(webText, response.GetContents());
        }
    }
}
