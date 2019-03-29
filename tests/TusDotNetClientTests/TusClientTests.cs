using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using TusDotNetClient;
using Xunit;
using static TusDotNetClientTests.Utils;

namespace TusDotNetClientTests
{
    public class TusClientTests : IClassFixture<Fixture>
    {
        private readonly string _dataDirectoryPath;

        public TusClientTests()
        {
            _dataDirectoryPath = Fixture.DataDirectory.FullName;
        }
        
        [Theory]
        [MemberData(nameof(Fixture.TestFiles), MemberType = typeof(Fixture))]
        public async Task AfterCallingCreate_DataShouldContainAFile(FileInfo file)
        {
            var sut = new TusClient();

            var url = await sut.CreateAsync(
                "http://localhost:1080/files/",
                file.Length);

            var upload = new FileInfo(Path.Combine(_dataDirectoryPath, $"{url.Split('/').Last()}.bin"));
            upload.Exists.ShouldBe(true);
            upload.Length.ShouldBe(0);
        }

        [Theory]
        [MemberData(nameof(Fixture.TestFiles), MemberType = typeof(Fixture))]
        public async Task AfterCallingCreateAndUpload_UploadedFileShouldBeTheSameAsTheOriginalFile(FileInfo file)
        {
            var sut = new TusClient();

            var url = await sut.CreateAsync("http://localhost:1080/files/", file.Length);
            await sut.UploadAsync(url, file);
            
            var upload = new FileInfo(Path.Combine(_dataDirectoryPath, $"{url.Split('/').Last()}.bin"));
            upload.Exists.ShouldBe(true);
            upload.Length.ShouldBe(file.Length);
            using (var fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            using (var uploadStream = new FileStream(upload.FullName, FileMode.Open, FileAccess.Read))
            {
                var fileBytes = new byte[fileStream.Length];
                fileStream.Read(fileBytes, 0, fileBytes.Length);
                var uploadBytes = new byte[uploadStream.Length];
                uploadStream.Read(uploadBytes, 0, uploadBytes.Length);
                SHA1(uploadBytes).ShouldBe(SHA1(fileBytes));
            }
        }

        [Theory]
        [MemberData(nameof(Fixture.TestFiles), MemberType = typeof(Fixture))]
        public async Task AfterCallingDownload_DownloadedFileShouldBeTheSameAsTheOriginalFile(FileInfo file)
        {
            var sut = new TusClient();

            var url = await sut.CreateAsync("http://localhost:1080/files/", file.Length);
            await sut.UploadAsync(url, file);
            var response = await sut.DownloadAsync(url);
            
            using (var fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            {
                var fileBytes = new byte[fileStream.Length];
                fileStream.Read(fileBytes, 0, fileBytes.Length);
                SHA1(response.ResponseBytes).ShouldBe(SHA1(fileBytes));
            }
        }

        [Theory]
        [MemberData(nameof(Fixture.TestFiles), MemberType = typeof(Fixture))]
        public async Task CallingHead_ShouldReturnProgressOfUploadedFile(FileInfo file)
        {
            var sut = new TusClient();

            var url = await sut.CreateAsync("http://localhost:1080/files/", file.Length);
            var headBeforeUpload = await sut.HeadAsync(url);
            await sut.UploadAsync(url, file);
            var headAfterUpload = await sut.HeadAsync(url);

            headBeforeUpload.Headers.Keys.ShouldContain("Upload-Offset");
            headBeforeUpload.Headers["Upload-Offset"].ShouldBe("0");
            headAfterUpload.Headers.Keys.ShouldContain("Upload-Offset");
            headAfterUpload.Headers["Upload-Offset"].ShouldBe(file.Length.ToString());
        }

        [Fact]
        public async Task CallingGetServerInfo_ShouldReturnServerInfo()
        {
            var sut = new TusClient();

            var response = await sut.GetServerInfo("http://localhost:1080/files/");

            response.Version.ShouldNotBeNullOrWhiteSpace();
            response.Extensions.ShouldNotBeEmpty();
            response.SupportedVersions.ShouldNotBeEmpty();
        }

        [Theory]
        [MemberData(nameof(Fixture.TestFiles), MemberType = typeof(Fixture))]
        public async Task CallingDelete_ShouldRemoveUploadedFile(FileInfo file)
        {
            var sut = new TusClient();


            var url = await sut.CreateAsync("http://localhost:1080/files/", file.Length);
            await sut.UploadAsync(url, file);
            var uploadHeadResponse = await sut.HeadAsync(url);
            var deleteResult = await sut.Delete(url);


            deleteResult.ShouldBe(true);
            uploadHeadResponse.Headers.Keys.ShouldContain("Upload-Offset");
            uploadHeadResponse.Headers["Upload-Offset"].ShouldBe(file.Length.ToString());
            File.Exists(Path.Combine(_dataDirectoryPath, $"url.Split('/').Last().bin")).ShouldBe(false);
        }
    }
}