
using System.Text;
using System.Text.RegularExpressions;

namespace RichTea.WebCache
{
    public sealed class WebDocument
    {

        public string Url { get; private set; }

        public byte[] Binary { get; private set; }

        public WebDocument(string url, byte[] binaryDocument)
        {
            Url = url;
            Binary = binaryDocument;
        }

        public string GetContents()
        {
            var contents = GetContents(Encoding.UTF8);
            return contents;
        }

        public string GetContents(Encoding encoding)
        {
            string contents = encoding.GetString(Binary);
            return contents;
        }

        public string GetContentsWithUnescapedHtml()
        {
            var contents = GetContents();
            return contents.Replace("&nbsp;", string.Empty).Replace("&#160;", string.Empty).Replace("&#8212;", "-").Trim();
        }

        public string GetContentsWithRemovedHtml()
        {
            var contents = GetContents();
            var htmlRegex = new Regex("<[^>]+>");
            var cleaned = htmlRegex.Replace(contents, string.Empty);
            return cleaned;
        }

    }
}
