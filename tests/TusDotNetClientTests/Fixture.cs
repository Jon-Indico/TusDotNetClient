using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace TusDotNetClientTests
{
    public class Fixture : IDisposable
    {
        private readonly Process _tusProcess;

        public static readonly DirectoryInfo DataDirectory;

        static Fixture()
        {
            DataDirectory = Directory.CreateDirectory("data");

            var smallTextFile = new FileInfo(Path.Combine(DataDirectory.FullName, "small_text_file.txt"));
            File.WriteAllText(smallTextFile.FullName, Guid.NewGuid().ToString());

            var largeSampleFile = new FileInfo(Path.Combine(DataDirectory.FullName, "large_sample_file.bin"));
            using (var fileStream = new FileStream(largeSampleFile.FullName, FileMode.Create, FileAccess.Write))
            {
                var bytes = new byte[1024 * 1024];
                foreach (var _ in Enumerable.Range(0, 50))
                {
                    new Random().NextBytes(bytes);
                    fileStream.Write(bytes, 0, bytes.Length);
                }
            }

            TestFiles = new[]
            {
                new[] {smallTextFile},
                new[] {largeSampleFile},
            };
        }

        public Fixture()
        {
            _tusProcess = Process.Start(new DirectoryInfo(Directory.GetCurrentDirectory())
                                            .Parent?
                                            .Parent?
                                            .Parent?
                                            .EnumerateFiles("tusd*")
                                            .First()
                                            .FullName ??
                                        throw new ArgumentException(
                                            "tusd executable must be present in test project directory"));
        }

        public static IEnumerable<object[]> TestFiles { get; }

        public void Dispose()
        {
            _tusProcess.Kill();
            DataDirectory.Delete(true);
        }
    }
}