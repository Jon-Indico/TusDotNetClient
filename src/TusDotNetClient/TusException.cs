using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace TusDotNetClient
{
    public class TusException : WebException
    {
        public string ResponseContent { get; }
        public HttpStatusCode StatusCode { get; }
        public string StatusDescription { get; }

        public WebException OriginalException { get; }

        public TusException(OperationCanceledException ex)
            : base(ex.Message, ex, WebExceptionStatus.RequestCanceled, null)
        {
            OriginalException = null;
        }

        public TusException(TusException ex, string message)
            : base($"{message}{ex.Message}", ex, ex.Status, ex.Response)
        {
            OriginalException = ex;

            StatusCode = ex.StatusCode;
            StatusDescription = ex.StatusDescription;
            ResponseContent = ex.ResponseContent;
        }

        public TusException(WebException ex, string message = "")
            : base($"{message}{ex.Message}", ex, ex.Status, ex.Response)
        {
            OriginalException = ex;

            if (ex.Response is HttpWebResponse webResponse &&
                webResponse.GetResponseStream() is Stream responseStream)
            {
                using (var reader = new StreamReader(responseStream))
                {
                    StatusCode = webResponse.StatusCode;
                    StatusDescription = webResponse.StatusDescription;
                    ResponseContent = reader.ReadToEnd();
                }
            }
        }

        public string FullMessage
        {
            get
            {
                var bits = new List<string>
                {
                    Message
                };

                if (Response is WebResponse response)
                {
                    bits.Add($"URL:{response.ResponseUri}");
                }

                if (StatusCode != HttpStatusCode.OK)
                {
                    bits.Add($"{StatusCode}:{StatusDescription}");
                }

                if (!string.IsNullOrEmpty(ResponseContent))
                {
                    bits.Add(ResponseContent);
                }

                return string.Join(Environment.NewLine, bits);
            }
        }

    }
}