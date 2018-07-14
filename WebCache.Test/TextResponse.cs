using RichTea.Common;
using System.Text;

namespace RichTea.WebCache.Test
{
    public class TextResponse : Response
    {
        public string ResponseText { get; private set; }


        public TextResponse(string responseText, int statusCode) : base(Encoding.UTF8.GetBytes(responseText), statusCode)
        {
            ResponseText = responseText;
        }

        public override string ToString()
        {
            return new ToStringBuilder<TextResponse>(this)
                .Append(ResponseText)
                .ToString();
        }

        public override bool Equals(object that)
        {
            var other = that as TextResponse;
            return new EqualsBuilder<TextResponse>(this, that)
                .Append(ResponseText, other?.ResponseData)
                .AreEqual;
        }

        public override int GetHashCode()
        {
            return new HashCodeBuilder<TextResponse>(this)
                .Append(ResponseText)
                .HashCode;
        }
    }
}
