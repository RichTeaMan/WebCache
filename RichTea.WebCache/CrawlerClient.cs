using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RichTea.WebCache
{
    public class CrawlerClient : WebClient
    {
        public CrawlerClient() : base()
        {
            Encoding = Encoding.UTF8;
            Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36";
            
        }

        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest w = base.GetWebRequest(uri);
            if (Timeout != 0)
            {
                w.Timeout = Timeout;
            }
            return w;
        }

        public int Timeout { get; set; }
    }
}
