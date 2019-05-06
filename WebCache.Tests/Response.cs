using RichTea.Common;
using System;

namespace RichTea.WebCache.Test
{
    public class Response
    {
        public byte[] ResponseData { get; private set; }

        public int StatusCode { get; private set; }

        public DateTimeOffset CacheDate { get; private set; }

        public Response(byte[] responseData, int statusCode, DateTimeOffset cacheDate)
        {
            ResponseData = responseData ?? throw new ArgumentNullException(nameof(responseData));
            StatusCode = statusCode;
            CacheDate = cacheDate;
        }

        public override string ToString()
        {
            return new ToStringBuilder<Response>(this)
                .Append(ResponseData)
                .Append(StatusCode)
                .ToString();
        }

        public override bool Equals(object that)
        {
            var other = that as Response;
            return new EqualsBuilder<Response>(this, that)
                .Append(ResponseData, other?.ResponseData)
                .Append(StatusCode, other?.StatusCode)
                .Append(CacheDate, other?.CacheDate)
                .AreEqual;
        }

        public override int GetHashCode()
        {
            return new HashCodeBuilder<Response>(this)
                .Append(ResponseData)
                .Append(StatusCode)
                .Append(CacheDate)
                .HashCode;
        }
    }
}
