using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace TusDotNetClient
{
    public class TusHttpResponse
    {
        private readonly Dictionary<string, string> _headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public HttpStatusCode StatusCode { get; }
        public IReadOnlyDictionary<string, string> Headers => _headers;
        public byte[] ResponseBytes { get; }
        public string ResponseString => Encoding.UTF8.GetString(ResponseBytes);

        public TusHttpResponse(HttpStatusCode statusCode, byte[] responseBytes = null)
        {
            StatusCode = statusCode;
            ResponseBytes = responseBytes;
        }

        public void AddHeader(string key, string value) =>
            _headers.Add(key, value);
    }
}