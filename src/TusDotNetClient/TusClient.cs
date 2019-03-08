using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace TusDotNetClient
{
    public partial class TusClient
    {
        public event ProgressDelegate UploadProgress;

        public event ProgressDelegate DownloadProgress;

        private readonly CancellationTokenSource _cancellationSource = new CancellationTokenSource();

        private readonly double _chunkSize;

        public Dictionary<string, string> AdditionalHeaders { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public TusClient() : this(5.0)
        {
        }

        public TusClient(double chunkSize) =>
            _chunkSize = chunkSize;

        public IWebProxy Proxy { get; set; }

        public void Cancel()
        {
            _cancellationSource.Cancel();
        }

        public string Create(string url, FileInfo file, params (string key, string value)[] metadata)
        {
            var metadataDictionary = metadata.ToDictionary(md => md.key, md => md.value);
            if (!metadataDictionary.ContainsKey("filename"))
            {
                metadataDictionary["filename"] = file.Name;
            }

            return Create(url, file.Length, metadataDictionary);
        }

        public string Create(string url, long uploadLength, Dictionary<string, string> metadata)
        {
            var requestUri = new Uri(url);
            var client = new TusHttpClient {Proxy = Proxy};

            var request = new TusHttpRequest(url, RequestMethod.Post);
            foreach (var kvp in AdditionalHeaders)
                request.AddHeader(kvp.Key, kvp.Value);
            request.AddHeader(TusHeaderNames.UploadLength, uploadLength.ToString());
            request.AddHeader(TusHeaderNames.ContentLength, "0");

            request.AddHeader(TusHeaderNames.UploadMetadata, string.Join(",", metadata
                .Select(md =>
                    $"{md.Key.Replace(" ", "").Replace(",", "")} {Convert.ToBase64String(Encoding.UTF8.GetBytes(md.Value))}")));

            var response = client.PerformRequest(request);

            if (response.StatusCode != HttpStatusCode.Created)
                throw new Exception("CreateFileInServer failed. " + response.ResponseString);

            if (!response.Headers.ContainsKey("Location"))
                throw new Exception("Location Header Missing");

            if (!Uri.TryCreate(response.Headers["Location"], UriKind.RelativeOrAbsolute, out var locationUri))
                throw new Exception("Invalid Location Header");

            if (!locationUri.IsAbsoluteUri)
                locationUri = new Uri(requestUri, locationUri);

            return locationUri.ToString();
        }

        public void Upload(string url, FileInfo file)
        {
            using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            {
                Upload(url, fs);
            }
        }

        public void Upload(string url, Stream fs)
        {
            var offset = GetFileOffset(url);
            var client = new TusHttpClient();
            SHA1 sha = new SHA1Managed();

            var chunkSize = (int) Math.Ceiling(_chunkSize * 1024.0 * 1024.0); // to MB
            if (offset == fs.Length)
                OnUploadProgress(fs.Length, fs.Length);

            while (offset < fs.Length)
            {
                fs.Seek(offset, SeekOrigin.Begin);
                var buffer = new byte[chunkSize];
                var bytesRead = fs.Read(buffer, 0, chunkSize);
                Array.Resize(ref buffer, bytesRead);
                var sha1Hash = sha.ComputeHash(buffer);
                var request = new TusHttpRequest(url, RequestMethod.Patch, buffer, _cancellationSource.Token);
                foreach (var kvp in AdditionalHeaders)
                    request.AddHeader(kvp.Key, kvp.Value);
                request.AddHeader(TusHeaderNames.UploadOffset, offset.ToString());
                request.AddHeader(TusHeaderNames.UploadChecksum, $"sha1 {Convert.ToBase64String(sha1Hash)}");
                request.AddHeader(TusHeaderNames.ContentType, "application/offset+octet-stream");

                try
                {
                    var response = client.PerformRequest(request);
                    if (response.StatusCode != HttpStatusCode.NoContent)
                        throw new Exception("WriteFileInServer failed. " + response.ResponseString);

                    offset = long.Parse(response.Headers[TusHeaderNames.UploadOffset]);

                    OnUploadProgress(offset, fs.Length);
                }
                catch (IOException ex)
                {
                    if (ex.InnerException is SocketException socketException)
                    {
                        if (socketException.SocketErrorCode == SocketError.ConnectionReset)
                        {
                            // retry by continuing the while loop
                            // but get new offset from server to prevent Conflict error
                            offset = GetFileOffset(url);
                        }
                        else
                        {
                            throw socketException;
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        public TusHttpResponse Download(string url)
        {
            var client = new TusHttpClient();
            var request = new TusHttpRequest(url, RequestMethod.Get, null, _cancellationSource.Token);
            foreach (var kvp in AdditionalHeaders)
                request.AddHeader(kvp.Key, kvp.Value);
            request.DownloadProgressed += OnDownloadProgress;
            var response = client.PerformRequest(request);
            return response;
        }

        public TusHttpResponse Head(string url)
        {
            var client = new TusHttpClient();
            var request = new TusHttpRequest(url, RequestMethod.Head);
            foreach (var kvp in AdditionalHeaders)
                request.AddHeader(kvp.Key, kvp.Value);
            try
            {
                return client.PerformRequest(request);
            }
            catch (TusException ex)
            {
                return new TusHttpResponse(ex.StatusCode);
            }
        }

        public TusServerInfo GetServerInfo(string url)
        {
            var client = new TusHttpClient();
            var request = new TusHttpRequest(url, RequestMethod.Options);
            foreach (var kvp in AdditionalHeaders)
                request.AddHeader(kvp.Key, kvp.Value);
            var response = client.PerformRequest(request);
            if (response.StatusCode != HttpStatusCode.NoContent && response.StatusCode != HttpStatusCode.OK)
                throw new Exception("getServerInfo failed. " + response.ResponseString);

            // Spec says NoContent but tusd gives OK because of browser bugs
            response.Headers.TryGetValue(TusHeaderNames.TusResumable, out var version);
            response.Headers.TryGetValue(TusHeaderNames.TusVersion, out var supportedVersion);
            response.Headers.TryGetValue(TusHeaderNames.TusExtension, out var extensions);
            response.Headers.TryGetValue(TusHeaderNames.TusMaxSize, out var maxSizeString);
            long.TryParse(maxSizeString, out var maxSize);
            return new TusServerInfo(version, supportedVersion, extensions, maxSize);
        }

        public bool Delete(string url)
        {
            var client = new TusHttpClient();
            var request = new TusHttpRequest(url, RequestMethod.Delete);
            foreach (var kvp in AdditionalHeaders)
                request.AddHeader(kvp.Key, kvp.Value);
            var response = client.PerformRequest(request);

            return response.StatusCode == HttpStatusCode.NoContent ||
                   response.StatusCode == HttpStatusCode.NotFound ||
                   response.StatusCode == HttpStatusCode.Gone;
        }

        public void OnUploadProgress(long bytesTransferred, long bytesTotal) =>
            UploadProgress?.Invoke(bytesTransferred, bytesTotal);

        public void OnDownloadProgress(long bytesTransferred, long bytesTotal) =>
            DownloadProgress?.Invoke(bytesTransferred, bytesTotal);

        private long GetFileOffset(string url)
        {
            var client = new TusHttpClient();
            var request = new TusHttpRequest(url, RequestMethod.Head);
            foreach (var kvp in AdditionalHeaders)
                request.AddHeader(kvp.Key, kvp.Value);
            var response = client.PerformRequest(request);

            if (response.StatusCode != HttpStatusCode.NoContent && response.StatusCode != HttpStatusCode.OK)
                throw new Exception("getFileOffset failed. " + response.ResponseString);

            if (!response.Headers.ContainsKey("Upload-Offset"))
                throw new Exception("Offset Header Missing");

            return long.Parse(response.Headers["Upload-Offset"]);
        }
    }
}