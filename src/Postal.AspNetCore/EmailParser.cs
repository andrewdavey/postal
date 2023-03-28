using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Postal
{
    /// <summary>
    /// Converts the raw string output of a view into a <see cref="MailMessage"/>.
    /// </summary>
    public class EmailParser : IEmailParser
    {
        /// <summary>
        /// Creates a new <see cref="EmailParser"/>.
        /// </summary>
        /// 
        public EmailParser(IEmailViewRender alternativeViewRenderer)
        {
            this.alternativeViewRenderer = alternativeViewRenderer;
        }

        readonly IEmailViewRender alternativeViewRenderer;

        /// <summary>
        /// Parses the email view output into a <see cref="MailMessage"/>.
        /// </summary>
        /// <param name="emailViewOutput">The email view output.</param>
        /// <param name="email">The <see cref="Email"/> used to generate the output.</param>
        /// <returns>A <see cref="MailMessage"/> containing the email headers and content.</returns>
        public async Task<MailMessage> ParseAsync(string emailViewOutput, Email email)
        {
            var message = new MailMessage();
            await InitializeMailMessageAsync(message, emailViewOutput, email);
            return message;
        }

        private async Task InitializeMailMessageAsync(MailMessage message, string emailViewOutput, Email email)
        {
            if (string.IsNullOrWhiteSpace(emailViewOutput))
            {
                throw new ArgumentNullException(nameof(emailViewOutput));
            }
            using (var reader = new StringReader(emailViewOutput))
            {
                await ParserUtils.ParseHeadersAsync(reader, (key, value) => ProcessHeaderAsync(key, value, message, email));
                AssignCommonHeaders(message, email);
                if (message.AlternateViews.Count == 0)
                {
                    var messageBody = reader.ReadToEnd().Trim();
                    if (email.ImageEmbedder.HasImages)
                    {
                        var view = AlternateView.CreateAlternateViewFromString(messageBody, new ContentType("text/html"));
                        email.ImageEmbedder.AddImagesToView(view);
                        message.AlternateViews.Add(view);
                        message.Body = "Plain text not available.";
                        message.IsBodyHtml = false;
                    }
                    else
                    {
                        message.Body = messageBody;
                        if (message.Body.StartsWith("<")) message.IsBodyHtml = true;
                    }
                }

                AddAttachments(message, email);
            }
        }

        private void AssignCommonHeaders(MailMessage message, Email email)
        {
            if (message.To.Count == 0)
            {
                AssignCommonHeader<string>(email, "to", to => message.To.Add(to));
                AssignCommonHeader<MailAddress>(email, "to", to => message.To.Add(to));
            }
            if (message.From == null)
            {
                AssignCommonHeader<string>(email, "from", from => message.From = new MailAddress(from));
                AssignCommonHeader<MailAddress>(email, "from", from => message.From = from);
            }
            if (message.CC.Count == 0)
            {
                AssignCommonHeader<string>(email, "cc", cc => message.CC.Add(cc));
                AssignCommonHeader<MailAddress>(email, "cc", cc => message.CC.Add(cc));
            }
            if (message.Bcc.Count == 0)
            {
                AssignCommonHeader<string>(email, "bcc", bcc => message.Bcc.Add(bcc));
                AssignCommonHeader<MailAddress>(email, "bcc", bcc => message.Bcc.Add(bcc));
            }
            if (message.ReplyToList.Count == 0)
            {
                AssignCommonHeader<string>(email, "replyto", replyTo => message.ReplyToList.Add(replyTo));
                AssignCommonHeader<MailAddress>(email, "replyto", replyTo => message.ReplyToList.Add(replyTo));
            }
            if (message.Sender == null)
            {
                AssignCommonHeader<string>(email, "sender", sender => message.Sender = new MailAddress(sender));
                AssignCommonHeader<MailAddress>(email, "sender", sender => message.Sender = sender);
            }
            if (string.IsNullOrEmpty(message.Subject))
            {
                AssignCommonHeader<string>(email, "subject", subject => message.Subject = subject);
            }
        }

        private void AssignCommonHeader<T>(Email email, string header, Action<T> assign)
            where T : class
        {
            object value;
            if (email.ViewData.TryGetValue(header, out value))
            {
                var typedValue = value as T;
                if (typedValue != null) assign(typedValue);
            }
        }

        private async Task ProcessHeaderAsync(string key, string value, MailMessage message, Email email)
        {
            if (IsAlternativeViewsHeader(key))
            {
                foreach (var view in CreateAlternativeViews(value, email))
                {
                    message.AlternateViews.Add(await view);
                }
            }
            else
            {
                AssignEmailHeaderToMailMessage(key, value, message);
            }
        }

        private IEnumerable<Task<AlternateView>> CreateAlternativeViews(string deliminatedViewNames, Email email)
        {
            var viewNames = deliminatedViewNames.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
            return viewNames.Select(v => CreateAlternativeView(email, v)).ToList();
        }

        private async Task<AlternateView> CreateAlternativeView(Email email, string alternativeViewName)
        {
            var fullViewName = GetAlternativeViewName(email, alternativeViewName);
            var output = await alternativeViewRenderer.RenderAsync(email, fullViewName);
            string contentType;
            string body;
            using (var reader = new StringReader(output))
            {
                contentType = ParseHeadersForContentType(reader);
                body = reader.ReadToEnd();
            }

            if (string.IsNullOrWhiteSpace(contentType))
            {
                if (alternativeViewName.Equals("text", StringComparison.OrdinalIgnoreCase))
                {
                    contentType = "text/plain";
                }
                else if (alternativeViewName.Equals("html", StringComparison.OrdinalIgnoreCase))
                {
                    contentType = "text/html";
                }
                else
                {
                    throw new Exception("The 'Content-Type' header is missing from the alternative view '" + fullViewName + "'.");
                }
            }

            var stream = CreateStreamOfBody(body);
            var alternativeView = new AlternateView(stream, contentType);
            if (alternativeView.ContentType.CharSet == null)
            {
                // Must set a charset otherwise mail readers seem to guess the wrong one!
                // Strings are unicode by default in .net.
                alternativeView.ContentType.CharSet = Encoding.Unicode.WebName;
                // A different charset can be specified in the Content-Type header.
                // e.g. Content-Type: text/html; charset=utf-8
            }
            email.ImageEmbedder.AddImagesToView(alternativeView);
            return alternativeView;
        }

        private static string GetAlternativeViewName(Email email, string alternativeViewName)
        {
            if (email.ViewName.StartsWith("~"))
            {
                var index = email.ViewName.LastIndexOf('.');
                return email.ViewName.Insert(index + 1, alternativeViewName + ".");
            }
            else
            {
                return email.ViewName + "." + alternativeViewName;
            }
        }

        private MemoryStream CreateStreamOfBody(string body)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(body);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private string ParseHeadersForContentType(StringReader reader)
        {
            string contentType = null;
            ParserUtils.ParseHeaders(reader, (key, value) =>
            {
                if (key.Equals("content-type", StringComparison.OrdinalIgnoreCase))
                {
                    contentType = value;
                }
            });
            return contentType;
        }

        private bool IsAlternativeViewsHeader(string headerName)
        {
            return headerName.Equals("views", StringComparison.OrdinalIgnoreCase);
        }

        private void AssignEmailHeaderToMailMessage(string key, string value, MailMessage message)
        {
            switch (key)
            {
                case "to":
                    message.To.Add(value);
                    break;
                case "from":
                    message.From = new MailAddress(value);
                    break;
                case "subject":
                    message.Subject = value;
                    break;
                case "cc":
                    message.CC.Add(value);
                    break;
                case "bcc":
                    message.Bcc.Add(value);
                    break;
                case "reply-to":
                    message.ReplyToList.Add(value);
                    break;
                case "sender":
                    message.Sender = new MailAddress(value);
                    break;
                case "priority":
                    MailPriority priority;
                    if (Enum.TryParse(value, true, out priority))
                    {
                        message.Priority = priority;
                    }
                    else
                    {
                        throw new ArgumentException(string.Format("Invalid email priority: {0}. It must be High, Medium or Low.", value));
                    }
                    break;
                case "content-type":
                    var charsetMatch = Regex.Match(value, @"\bcharset\s*=\s*(.*)$");
                    if (charsetMatch.Success)
                    {
                        message.BodyEncoding = Encoding.GetEncoding(charsetMatch.Groups[1].Value);
                    }
                    break;
                default:
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        message.Headers[key] = "   (empty)";
                    }
                    else
                    {
                        message.Headers[key] = value;
                    }
                    break;
            }
        }

        void AddAttachments(MailMessage message, Email email)
        {
            foreach (var attachment in email.Attachments)
            {
                message.Attachments.Add(attachment);
            }
        }
    }
}