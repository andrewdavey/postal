using System;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Postal
{
    /// <summary>
    /// Renders a preview of an email to display in the browser.
    /// </summary>
    public class EmailViewResult : ViewResult
    {
        const string TextContentType = "text/plain";
        const string HtmlContentType = "text/html";

        IEmailViewRenderer Renderer { get; set; }
        IEmailParser Parser { get; set; }
        Email Email { get; set; }

        /// <summary>
        /// Creates a new <see cref="EmailViewResult"/>.
        /// </summary>
        public EmailViewResult(Email email, IEmailViewRenderer renderer, IEmailParser parser)
        {
            Email = email;
            Renderer = renderer ?? new EmailViewRenderer(ViewEngineCollection);
            Parser = parser ?? new EmailParser(Renderer);
        }

        /// <summary>
        /// Creates a new <see cref="EmailViewResult"/>.
        /// </summary>
        public EmailViewResult(Email email)
            : this(email, null, null)
        {
        }

        public override void ExecuteResult(ControllerContext context)
        {
            var httpContext = context.RequestContext.HttpContext;
            var query = httpContext.Request.QueryString;
            var format = query["format"];
            var contentType = ExecuteResult(context.HttpContext.Response.Output, format);
            httpContext.Response.ContentType = contentType;
        }

        /// <summary>
        /// Writes the email preview in the given format.
        /// </summary>
        /// <returns>The content type for the HTTP response.</returns>
        public string ExecuteResult(TextWriter writer, string format = null)
        {
            var result = Renderer.Render(Email);
            var mailMessage = Parser.Parse(result, Email);

            // no special requests; render what's in the template
            if (string.IsNullOrEmpty(format))
            {
                if (!mailMessage.IsBodyHtml)
                {
                    writer.Write(result);
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
                if(mailMessage.IsBodyHtml)
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

            // Replace image embeds through linked resources
            var embedder = new ImageEmbedder();
            content = embedder.ReplaceImageData(view, content);

            writer.Write(content);
            return contentType;
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

        static TemplateParts Extract(string template)
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
    }
}
