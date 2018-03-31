
using System.Text;

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

    }
}
