using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace TusDotNetClient
{
    public class TusHttpResponse
    {
        public HttpStatusCode StatusCode { get; }
        public IReadOnlyDictionary<string, string> Headers { get; }
        public byte[] ResponseBytes { get; }
        public string ResponseString => Encoding.UTF8.GetString(ResponseBytes);

        public TusHttpResponse(
            HttpStatusCode statusCode,
            IDictionary<string, string> headers = null,
            byte[] responseBytes = null)
        {
            StatusCode = statusCode;
            Headers = headers is null
                ? new Dictionary<string, string>(0)
                : new Dictionary<string, string>(headers);
            ResponseBytes = responseBytes;
        }
    }
}