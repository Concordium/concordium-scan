/// https://github.com/philipmat/EnumerableStreamFileResult
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace EnumerableStreamFileResult
{
    class EnumerableFileResult<T> : FileResult
    {
        private readonly IEnumerable<T> _enumeration;
        private readonly IStreamWritingAdapter<T> _writer;

        public EnumerableFileResult(IEnumerable<T> enumeration, IStreamWritingAdapter<T> writer)
            : base(writer.ContentType)
        {
            _enumeration = enumeration ?? throw new ArgumentNullException(nameof(enumeration));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            SetContentType(context);
            SetContentDispositionHeader(context);

            await WriteContentAsync(context).ConfigureAwait(false);
        }

        private async Task WriteContentAsync(ActionContext context)
        {
            var body = context.HttpContext.Response.Body;
            await _writer.WriteHeaderAsync(body).ConfigureAwait(false);
            int recordCount = 0;
            foreach (var item in _enumeration)
            {
                await _writer.WriteAsync(item, body).ConfigureAwait(false);
                recordCount++;
            }

            await _writer.WriteFooterAsync(body, recordCount);

            await base.ExecuteResultAsync(context).ConfigureAwait(false);
        }

        private void SetContentDispositionHeader(ActionContext context)
        {
            var headers = context.HttpContext.Response.Headers;
            var cd = new System.Net.Mime.ContentDisposition
            {
                FileName = FileDownloadName,
                Inline = false, // false = attachement
            };

            headers.Add(
                "Content-Disposition",
                new Microsoft.Extensions.Primitives.StringValues(cd.ToString()));
        }

        private void SetContentType(ActionContext context)
        {
            var response = context.HttpContext.Response;
            response.ContentType = ContentType;
        }
    }

    internal interface IStreamWritingAdapter<T>
    {
       string ContentType { get; }

        Task WriteHeaderAsync(Stream stream);

        Task WriteAsync(T item, Stream stream);

        Task WriteFooterAsync(Stream stream, int recordCount); 
    }
}
