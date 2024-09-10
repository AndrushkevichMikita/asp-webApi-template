using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System.IO.Compression;

namespace ApiTemplate.SharedKernel.PipelineExtensions
{
    public class CompressionExtension : IFileProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFileProvider _fileProvider;

        public CompressionExtension(IWebHostEnvironment hostingEnvironment, IHttpContextAccessor httpContextAccessor)
        {
            _fileProvider = hostingEnvironment.WebRootFileProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
            => _fileProvider.GetDirectoryContents(subpath);

        public IFileInfo GetFileInfo(string subpath)
        {
            if (_httpContextAccessor.HttpContext.Request.Headers.TryGetValue("Accept-Encoding", out var encodings))
            {
                if (encodings.Any(encoding => encoding.Contains("br")))
                {
                    var file = _fileProvider.GetFileInfo(subpath.EndsWith(".br") ? subpath : subpath + ".br");
                    if (file.Exists) return file;
                    else
                    {
                        var originalFile = _fileProvider.GetFileInfo(subpath);
                        if (originalFile.Exists)
                        {
                            using var stream = originalFile.CreateReadStream();
                            using FileStream compressedStream = File.Create(originalFile.PhysicalPath + ".br");
                            using BrotliStream brStream = new(compressedStream, CompressionMode.Compress);
                            stream.CopyTo(brStream);
                            brStream.Close(); // ensure that i/o operation with file is done => without it file info length can be 0
                            var brFile = _fileProvider.GetFileInfo(subpath + ".br");
                            if (brFile.Exists) return brFile;
                        }
                    }
                }
                if (encodings.Any(encoding => encoding.Contains("gzip")))
                {
                    var file = _fileProvider.GetFileInfo(subpath.EndsWith(".gz") ? subpath : subpath + ".gz"); ;
                    if (file.Exists) return file;
                    else
                    {
                        var originalFile = _fileProvider.GetFileInfo(subpath);
                        if (originalFile.Exists)
                        {
                            using var stream = originalFile.CreateReadStream();
                            using FileStream compressedStream = File.Create(originalFile.PhysicalPath + ".gz");
                            using GZipStream gzStream = new(compressedStream, CompressionMode.Compress);
                            stream.CopyTo(gzStream);
                            gzStream.Close(); // ensure that i/o operation with file is done => without it file info length can be 0
                            var gzFile = _fileProvider.GetFileInfo(subpath + ".gz");
                            if (gzFile.Exists) return gzFile;
                        }
                    }
                }
            }
            return _fileProvider.GetFileInfo(subpath);
        }

        public IChangeToken Watch(string filter)
            => _fileProvider.Watch(filter);
    }

    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseCompressedStaticFiles(this IApplicationBuilder applicationBuilder, IWebHostEnvironment hostingEnvironment, IHttpContextAccessor httpContextAccessor)
        {
            return applicationBuilder.UseStaticFiles(new StaticFileOptions
            {
                ServeUnknownFileTypes = true,
                FileProvider = new CompressionExtension(hostingEnvironment, httpContextAccessor),
                OnPrepareResponse = ctx =>
                {
                    var headers = ctx.Context.Response.Headers;

                    if (ctx.File.Name.EndsWith(".br"))
                        headers.Add("Content-Encoding", "br");
                    else if (ctx.File.Name.EndsWith(".gz"))
                        headers.Add("Content-Encoding", "gzip");
                }
            });
        }
    }
}
