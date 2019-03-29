namespace TusDotNetClient
{
    public partial class TusClient
    {
        public class TusServerInfo
        {
            public string Version { get; }
            public string SupportedVersions { get; }
            public string Extensions { get; }
            public ulong MaxSize { get; }

            public bool SupportsDelete => Extensions.Contains("termination");

            public TusServerInfo(string version, string supportedVersions, string extensions, ulong? maxSize)
            {
                Version = version ?? "";
                SupportedVersions = supportedVersions ?? "";
                Extensions = extensions ?? "";
                MaxSize = maxSize ?? 0;
            }
        }
    }
}