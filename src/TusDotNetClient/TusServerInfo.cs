using System.Linq;

namespace TusDotNetClient
{
    public partial class TusClient
    {
        public class TusServerInfo
        {
            public string Version { get; }
            public string SupportedVersions { get; }
            public string Extensions { get; }
            public long MaxSize { get; }
            public string[] SupportedChecksumAlgorithms { get; }

            public bool SupportsDelete => Extensions.Contains("termination");

            public TusServerInfo(
                string version,
                string supportedVersions,
                string extensions,
                long? maxSize,
                string checksumAlgorithms)
            {
                Version = version ?? "";
                SupportedVersions = supportedVersions ?? "";
                Extensions = extensions ?? "";
                MaxSize = maxSize ?? 0;
                SupportedChecksumAlgorithms = (checksumAlgorithms ?? "").Trim().Split(',').ToArray();
            }
        }
    }
}