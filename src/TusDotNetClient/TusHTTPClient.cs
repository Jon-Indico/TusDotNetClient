using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace TusDotNetClient
{
    public class TusHttpClient
    {
        public IWebProxy Proxy { get; set; }

        public async Task<TusHttpResponse> PerformRequestAsync(TusHttpRequest request)
        {
            var segment = request.BodyBytes;

            try
            {
                var webRequest = WebRequest.CreateHttp(request.Url);
                webRequest.AutomaticDecompression = DecompressionMethods.GZip;

                webRequest.Timeout = Timeout.Infinite;
                webRequest.ReadWriteTimeout = Timeout.Infinite;
                webRequest.Method = request.Method;
                webRequest.KeepAlive = false;

                webRequest.Proxy = Proxy;

                try
                {
                    var currentServicePoint = webRequest.ServicePoint;
                    currentServicePoint.Expect100Continue = false;
                }
                catch (PlatformNotSupportedException)
                {
                    //expected on .net core 2.0 with systemproxy
                    //fixed by https://github.com/dotnet/corefx/commit/a9e01da6f1b3a0dfbc36d17823a2264e2ec47050
                    //should work in .net core 2.2
                }

                //SEND
                var buffer = new byte[4096];

                var totalBytesWritten = 0L;

                webRequest.AllowWriteStreamBuffering = false;
                webRequest.ContentLength = segment.Count;

                foreach (var header in request.Headers)
                    switch (header.Key)
                    {
                        case TusHeaderNames.ContentLength:
                            webRequest.ContentLength = long.Parse(header.Value);
                            break;
                        case TusHeaderNames.ContentType:
                            webRequest.ContentType = header.Value;
                            break;
                        default:
                            webRequest.Headers.Add(header.Key, header.Value);
                            break;
                    }

                if (request.BodyBytes.Count > 0)
                {
                    var inputStream = new MemoryStream(request.BodyBytes.Array, request.BodyBytes.Offset, request.BodyBytes.Count);

                    using (var requestStream = webRequest.GetRequestStream())
                    {
                        inputStream.Seek(0, SeekOrigin.Begin);

                        var bytesWritten = await inputStream.ReadAsync(buffer, 0, buffer.Length, request.CancelToken)
                            .ConfigureAwait(false);

                        request.OnUploadProgressed(0, segment.Count);

                        while (bytesWritten > 0)
                        {
                            totalBytesWritten += bytesWritten;

                            request.OnUploadProgressed(totalBytesWritten, segment.Count);

                            await requestStream.WriteAsync(buffer, 0, bytesWritten, request.CancelToken)
                                .ConfigureAwait(false);

                            bytesWritten = await inputStream.ReadAsync(buffer, 0, buffer.Length, request.CancelToken)
                                .ConfigureAwait(false);
                        }
                    }
                }


                var response = (HttpWebResponse)await webRequest.GetResponseAsync()
                    .ConfigureAwait(false);

                //contentLength=0 for gzipped responses due to .net bug
                long contentLength = Math.Max(response.ContentLength, 0);

                buffer = new byte[16 * 1024];
                var outputStream = new MemoryStream();

                using (var responseStream = response.GetResponseStream())
                {
                    if (responseStream != null)
                    {
                        var bytesRead = 0;
                        var totalBytesRead = 0L;

                        bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length, request.CancelToken)
                            .ConfigureAwait(false);

                        request.OnDownloadProgressed(0, contentLength);

                        while (bytesRead > 0)
                        {
                            totalBytesRead += bytesRead;

                            request.OnDownloadProgressed(totalBytesRead, contentLength);

                            await outputStream.WriteAsync(buffer, 0, bytesRead, request.CancelToken)
                                .ConfigureAwait(false);

                            bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length, request.CancelToken)
                                .ConfigureAwait(false);
                        }
                    }
                }

                var resp = new TusHttpResponse(response.StatusCode, outputStream.ToArray());

                foreach (string headerName in response.Headers.Keys)
                {
                    resp.AddHeader(headerName, response.Headers[headerName]);
                }

                return resp;
            }
            catch (OperationCanceledException cancelEx)
            {
                throw new TusException(cancelEx);
            }
            catch (WebException ex)
            {
                throw new TusException(ex);
            }
        }
    }
}