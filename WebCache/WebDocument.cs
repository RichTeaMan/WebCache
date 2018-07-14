using System.Text;
using System.Text.RegularExpressions;

namespace RichTea.WebCache
{
    /// <summary>
    /// Web document.
    /// </summary>
    public sealed class WebDocument
    {
        /// <summary>
        /// Gets URL.
        /// </summary>
        public string Url { get; private set; }

        /// <summary>
        /// Gets underlying byte array.
        /// </summary>
        public byte[] Binary { get; private set; }

        /// <summary>
        /// Initialises a new instance of the <see cref="WebDocument"/> class.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <param name="binaryDocument">Binary document.</param>
        public WebDocument(string url, byte[] binaryDocument)
        {
            Url = url;
            Binary = binaryDocument;
        }

        /// <summary>
        /// Gets the contents of the document as a UTF-8 string.
        /// </summary>
        /// <returns></returns>
        public string GetContents()
        {
            var contents = GetContents(Encoding.UTF8);
            return contents;
        }

        /// <summary>
        /// Gets the contents of the document as a string with the given encoding.
        /// </summary>
        /// <param name="encoding">Encoding.</param>
        /// <returns></returns>
        public string GetContents(Encoding encoding)
        {
            string contents = encoding.GetString(Binary);
            return contents;
        }

        /// <summary>
        /// Gets contents with HTML unescaped.
        /// </summary>
        /// <returns></returns>
        public string GetContentsWithUnescapedHtml()
        {

            var contents = GetContents();
            return contents.Replace("&nbsp;", string.Empty).Replace("&#160;", string.Empty).Replace("&#8212;", "-").Trim();
        }

        /// <summary>
        /// Gets contents with HTML removed.
        /// </summary>
        /// <returns></returns>
        public string GetContentsWithRemovedHtml()
        {
            var contents = GetContents();
            var htmlRegex = new Regex("<[^>]+>");
            var cleaned = htmlRegex.Replace(contents, string.Empty);
            return cleaned;
        }

    }
}
