using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RichTea.WebCache
{
    /// <summary>
    /// Crawler client.
    /// </summary>
    public class CrawlerClient : WebClient
    {

        /// <summary>
        /// Gets or sets timeout.
        /// </summary>
        public int Timeout { get; set; } = 60;

        /// <summary>
        /// Initialises a new instance of the <see cref="CrawlerClient" /> class.
        /// </summary>
        public CrawlerClient() : base()
        {
            Encoding = Encoding.UTF8;
        }

        /// <summary>
        /// Returns a <see cref="WebRequest"/> object for the specified resource.
        /// </summary>
        /// <param name="uri">URI.</param>
        /// <returns></returns>
        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest w = base.GetWebRequest(uri);
            if (Timeout != 0)
            {
                w.Timeout = Timeout;
            }
            return w;
        }

    }
}
