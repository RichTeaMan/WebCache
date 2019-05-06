using RichTea.Common;
using System;
using System.Text;

namespace RichTea.WebCache.Test
{
    public class TextResponse : Response
    {
        public string ResponseText { get; private set; }


        public TextResponse(string responseText, int statusCode, DateTimeOffset cacheDate) : base(Encoding.UTF8.GetBytes(responseText), statusCode, cacheDate)
        {
            ResponseText = responseText;
        }

        public TextResponse(string responseText, int statusCode) : this(responseText, statusCode, DateTimeOffset.Now)
        {
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
