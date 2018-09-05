using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Postal.AspNetCore
{
    public class EmailViewResultExecutor : IActionResultExecutor<EmailViewResult>
    {
        private const string TextContentType = "text/plain";
        private const string HtmlContentType = "text/html";
        private static readonly Action<ILogger, string, Exception> _emailViewResultExecuting = LoggerMessage.Define<string>(
             LogLevel.Information,
                 1,
                 "Executing EmailViewResult with HTTP Response ContentType of {ContentType}");

        private const string DefaultContentType = "text/html; charset=utf-8";
        private readonly ILogger<EmailViewResultExecutor> _logger;
        private readonly IHttpResponseStreamWriterFactory _httpResponseStreamWriterFactory;

        IEmailViewRenderer Renderer { get; set; }
        IEmailParser Parser { get; set; }

        public EmailViewResultExecutor(ILoggerFactory loggerFactory,
            IHttpResponseStreamWriterFactory httpResponseStreamWriterFactory,
            IEmailViewRenderer renderer,
            IEmailParser parser = null)
        {
            _logger = loggerFactory.CreateLogger<EmailViewResultExecutor>();
            _httpResponseStreamWriterFactory = httpResponseStreamWriterFactory;
            Renderer = renderer;
            Parser = parser ?? new EmailParser(Renderer);
        }

        public async Task ExecuteAsync(ActionContext context, EmailViewResult result)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var response = context.HttpContext.Response;

            string resolvedContentType;
            Encoding resolvedContentTypeEncoding;
            ResponseContentTypeHelper.ResolveContentTypeAndEncoding(
                result.ContentType,
                response.ContentType,
                DefaultContentType,
                out resolvedContentType,
                out resolvedContentTypeEncoding);

            response.ContentType = resolvedContentType;

            if (result.StatusCode != null)
            {
                response.StatusCode = result.StatusCode.Value;
            }

            _emailViewResultExecuting(_logger, resolvedContentType, null);

            var httpContext = context.HttpContext;
            var requestFeature = httpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpRequestFeature>();
            var query = httpContext.Request.Query;
            var format = query["format"];
            using (var textWriter = _httpResponseStreamWriterFactory.CreateWriter(response.Body, resolvedContentTypeEncoding))
            {
                var contentType = await WriteEmailAsync(result.Email, textWriter, format);
                await textWriter.FlushAsync();
                httpContext.Response.ContentType = contentType;
            }
        }

        /// <summary>
        /// Writes the email preview in the given format.
        /// </summary>
        /// <returns>The content type for the HTTP response.</returns>
        internal async Task<string> WriteEmailAsync(Email email, TextWriter writer, string format = null)
        {
            var result = await Renderer.RenderAsync(email);
            var mailMessage = await Parser.ParseAsync(result, email);

            // no special requests; render what's in the template
            if (string.IsNullOrEmpty(format))
            {
                if (!mailMessage.IsBodyHtml)
                {
                    await writer.WriteAsync(result);
                    return TextContentType;
                }

                var template = Extract(result);
                template.Write(writer);
                return HtmlContentType;
            }

            // Check if alternative 
            var alternativeContentType = CheckAlternativeViews(writer, mailMessage, format);

            if (!string.IsNullOrEmpty(alternativeContentType))
                return alternativeContentType;

            if (format == "text")
            {
                if (mailMessage.IsBodyHtml)
                    throw new NotSupportedException("No text view available for this email");

                writer.Write(result);
                return TextContentType;
            }

            if (format == "html")
            {
                if (!mailMessage.IsBodyHtml)
                    throw new NotSupportedException("No html view available for this email");

                var template = Extract(result);
                template.Write(writer);
                return HtmlContentType;
            }

            throw new NotSupportedException(string.Format("Unsupported format {0}", format));
        }

        private static TemplateParts Extract(string template)
        {
            var headerBuilder = new StringBuilder();

            using (var reader = new StringReader(template))
            {
                // try to read until we passed headers
                var line = reader.ReadLine();

                while (line != null)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        return new TemplateParts(headerBuilder.ToString(), reader.ReadToEnd());
                    }

                    headerBuilder.AppendLine(line);
                    line = reader.ReadLine();
                }
            }

            return null;
        }

        class TemplateParts
        {
            readonly string header;
            readonly string body;

            public TemplateParts(string header, string body)
            {
                this.header = header;
                this.body = body;
            }

            public void Write(TextWriter writer)
            {
                writer.WriteLine("<!--");
                writer.WriteLine(header);
                writer.WriteLine("-->");
                writer.WriteLine(body);
            }
        }


        static string CheckAlternativeViews(TextWriter writer, MailMessage mailMessage, string format)
        {
            var contentType = format == "html"
                ? HtmlContentType
                : TextContentType;

            // check for alternative view
            var view = mailMessage.AlternateViews.FirstOrDefault(v => v.ContentType.MediaType == contentType);

            if (view == null)
                return null;

            string content;
            using (var reader = new StreamReader(view.ContentStream))
                content = reader.ReadToEnd();

            content = ReplaceLinkedImagesWithEmbeddedImages(view, content);

            writer.Write(content);
            return contentType;
        }

        internal static string ReplaceLinkedImagesWithEmbeddedImages(AlternateView view, string content)
        {
            var resources = view.LinkedResources;

            if (!resources.Any())
                return content;

            foreach (var resource in resources)
            {
                var find = "src=\"cid:" + resource.ContentId + "\"";
                var imageData = ComposeImageData(resource);
                content = content.Replace(find, "src=\"" + imageData + "\"");
            }

            return content;
        }

        static string ComposeImageData(LinkedResource resource)
        {
            var contentType = resource.ContentType.MediaType;
            var bytes = ReadFully(resource.ContentStream);
            return string.Format("data:{0};base64,{1}",
                contentType,
                Convert.ToBase64String(bytes));
        }

        static byte[] ReadFully(Stream input)
        {
            using (var ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
