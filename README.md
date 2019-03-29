# TusDotNetClient
.Net client for [tus.io](http://tus.io/) Resumable File Upload protocol.

## Features
- Supports tus v1.0.0
- Protocol extensions supported: Creation, Termination
- Upload progress events

## Usage
```c#
var file = new FileInfo(@"path/to/file.ext");
var client = new TusClient();
var fileUrl = await client.CreateAsync(Address, file.Length, metadata);
await client.UploadAsync(fileUrl, file, chunkSize: 5D);
```

### Progress updates
`UploadAsync` returns an object of type `TusOperation`, which exposes an event which will report the progress of the upload.

Store the return object in a variable and subscribe to the `Progressed` event for updates. The upload operation will not start until `TusOperation` is `await`ed.

```c#
var file = new FileInfo(@"path/to/file.ext");
var client = new TusClient();
var fileUrl = await client.CreateAsync(Address, file.Length, metadata);
var uploadOperation = client.UploadAsync(fileUrl, file, chunkSize: 5D);

uploadOperation.Progressed += (transferred, total) => 
    System.Diagnostics.Debug.WriteLine($"Progress: {transferred}/{total}");
    
await uploadOperation; 
```

## License
MIT
