# TusDotNetClient
.Net client for [tus.io](http://tus.io/) Resumable File Upload protocol.

## Features
- Supports tus v1.0.0
- Protocol extensions supported: Creation, Termination
- Upload progress events
- Targets .NET Standard 2.0

## Usage
```c#
var file = new FileInfo(@"path/to/file.ext");
var client = new TusClient(ChunkSize);
var fileUrl = await client.CreateAsync(Address, file, metadata);
await client.UploadAsync(fileUrl, file);
```

## License
MIT
