using System;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Postal
{
    public class EmailViewResult : ViewResult
    {
        const string TextContentType = "text/plain";
        const string HtmlContentType = "text/html";

        IEmailViewRenderer Renderer { get; set; }
        IEmailParser Parser { get; set; }

        Email Email { get; set; }

        public EmailViewResult(Email email, IEmailViewRenderer renderer, IEmailParser parser)
        {
            Email = email;
            Renderer = renderer ?? new EmailViewRenderer(ViewEngineCollection);
            Parser = parser ?? new EmailParser(Renderer);
        }

        public EmailViewResult(Email email)
            : this(email, null, null)
        {
        }

        public override void ExecuteResult(ControllerContext context)
        {
            HttpContextBase httpContext = context.RequestContext.HttpContext;

            var query = httpContext.Request.QueryString;
            string format = query["format"];

            string contentType = ExecuteResult(context.HttpContext.Response.Output, format);
            httpContext.Response.ContentType = contentType;
        }

        public string ExecuteResult(TextWriter writer, string format = null)
        {
            string result = Renderer.Render(Email);
            MailMessage mailMessage = Parser.Parse(result, Email);

            // no special requests; render what's in the template
            if (string.IsNullOrEmpty(format))
            {
                if (!mailMessage.IsBodyHtml)
                {
                    writer.Write(result);
                    return TextContentType;
                }

                TemplateParts template = Extract(result);
                template.Write(writer);
                return HtmlContentType;
            }

            // Check if alternative 
            string alternativeContentType = CheckAlternativeViews(writer, mailMessage, format);

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

                TemplateParts template = Extract(result);
                template.Write(writer);
                return HtmlContentType;
            }

            throw new NotSupportedException(string.Format("Unsupported format {0}", format));
        }

        static string CheckAlternativeViews(TextWriter writer, MailMessage mailMessage, string format)
        {
            string contentType = format == "html"
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

        private class TemplateParts
        {
            public string Header { get; set; }

            public string Body { get; set; }

            public void Write(TextWriter writer)
            {
                writer.WriteLine("<!--");
                writer.WriteLine(Header);
                writer.WriteLine("-->");
                writer.WriteLine(Body);
            }
        }

        static TemplateParts Extract(string template)
        {
            var headerBuilder = new StringBuilder();

            using (var reader = new StringReader(template))
            {
                // try to read until we passed headers
                string line = reader.ReadLine();

                while (line != null)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        return new TemplateParts
                        {
                            Header = headerBuilder.ToString(),
                            Body = reader.ReadToEnd(),
                        };
                    }

                    headerBuilder.AppendLine(line);
                    line = reader.ReadLine();
                }
            }

            return null;
        }
    }
}
