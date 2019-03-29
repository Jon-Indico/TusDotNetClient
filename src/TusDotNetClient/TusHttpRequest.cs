using System;
using System.Collections.Generic;
using System.Threading;

namespace TusDotNetClient
{
    public enum RequestMethod
    {
        Get,
        Post,
        Head,
        Patch,
        Options,
        Delete
    }

    public class TusHttpRequest
    {
        private readonly Dictionary<string, string> _headers;
        
        public event ProgressDelegate UploadProgressed;

        public event ProgressDelegate DownloadProgressed;

        public Uri Url { get; }
        public string Method { get; }
        public IReadOnlyDictionary<string, string> Headers => _headers;
        public ArraySegment<byte> BodyBytes { get; }

        public CancellationToken CancelToken { get; }

        public TusHttpRequest(
            string url,
            RequestMethod method,
            IDictionary<string, string> additionalHeaders = null,
            ArraySegment<byte> bodyBytes = default,
            CancellationToken? cancelToken = null)
        {
            Url = new Uri(url);
            Method = method.ToString().ToUpperInvariant();
            BodyBytes = bodyBytes;
            CancelToken = cancelToken ?? CancellationToken.None;

            _headers = additionalHeaders is null
                ? new Dictionary<string, string>(1)
                : new Dictionary<string, string>(additionalHeaders); 
            _headers.Add(TusHeaderNames.TusResumable, "1.0.0");
        }

        public void AddHeader(string key, string value) => _headers.Add(key, value);

        public void OnUploadProgressed(long bytesTransferred, long bytesTotal) =>
            UploadProgressed?.Invoke(bytesTransferred, bytesTotal);

        public void OnDownloadProgressed(long bytesTransferred, long bytesTotal) =>
            DownloadProgressed?.Invoke(bytesTransferred, bytesTotal);
    }
}