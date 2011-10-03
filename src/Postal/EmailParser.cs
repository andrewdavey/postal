using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

namespace Postal
{
    /// <summary>
    /// Converts the raw string output of a view into a <see cref="MailMessage"/>.
    /// </summary>
    public class EmailParser : IEmailParser
    {
        public EmailParser(IEmailViewRenderer alternativeViewRenderer)
        {
            this.alternativeViewRenderer = alternativeViewRenderer;
        }

        readonly IEmailViewRenderer alternativeViewRenderer;

        public MailMessage Parse(string emailViewOutput, Email email)
        {
            var message = new MailMessage();
            InitializeMailMessage(message, emailViewOutput, email);
            return message;
        }

        void InitializeMailMessage(MailMessage message, string emailViewOutput, Email email)
        {
            using (var reader = new StringReader(emailViewOutput))
            {
                ParserUtils.ParseHeaders(reader, (key, value) => ProcessHeader(key, value, message, email));
                AssignCommonHeaders(message, email);
                if (message.AlternateViews.Count == 0)
                {
                    message.Body = reader.ReadToEnd().Trim();
                    if (message.Body.StartsWith("<")) message.IsBodyHtml = true;
                }

                AddAttachments(message, email);
            }
        }

        void AssignCommonHeaders(MailMessage message, Email email)
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

        void AssignCommonHeader<T>(Email email, string header, Action<T> assign)
            where T : class
        {
            object value;
            if (email.ViewData.TryGetValue(header, out value))
            {
                var typedValue = value as T;
                if (typedValue != null) assign(typedValue);
            }
        }

        void ProcessHeader(string key, string value, MailMessage message, Email email)
        {
            if (IsAlternativeViewsHeader(key))
            {
                foreach (var view in CreateAlternativeViews(value, email))
                {
                    message.AlternateViews.Add(view);
                }
            }
            else
            {
                AssignEmailHeaderToMailMessage(key, value, message);
            }
        }

        IEnumerable<AlternateView> CreateAlternativeViews(string deliminatedViewNames, Email email)
        {
            var viewNames = deliminatedViewNames.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
            return from viewName in viewNames 
                   select CreateAlternativeView(email, viewName);
        }

        AlternateView CreateAlternativeView(Email email, string alternativeViewName)
        {
            var fullViewName = email.ViewName + "." + alternativeViewName;
            var imageEmbedder = new ImageEmbedder();
            email.ViewData["Postal.ImageEmbedder"] = imageEmbedder;
            var output = alternativeViewRenderer.Render(email, fullViewName);

            string contentType = null;
            string body = null;
            using (var reader = new StringReader(output))
            {
                contentType = ParseHeadersForContentType(reader);
                body = reader.ReadToEnd();
            }

            if (string.IsNullOrWhiteSpace(contentType))
                throw new Exception("The 'Content-Type' header is missing from the alternative view '" + fullViewName + "'.");

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
            imageEmbedder.PutImagesIntoView(alternativeView);
            email.ViewData.Remove("Postal.ImageEmbedder");
            return alternativeView;
        }

        MemoryStream CreateStreamOfBody(string body)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(body);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        string ParseHeadersForContentType(StringReader reader)
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

        bool IsAlternativeViewsHeader(string headerName)
        {
            return headerName.Equals("views", StringComparison.OrdinalIgnoreCase);
        }

        void AssignEmailHeaderToMailMessage(string key, string value, MailMessage message)
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
                case "content-type":
                    var charsetMatch = Regex.Match(value, @"\bcharset\s*=\s*(.*)$");
                    if (charsetMatch.Success)
                    {
                        message.BodyEncoding = Encoding.GetEncoding(charsetMatch.Groups[1].Value);
                    }
                    break;
                default:
                    message.Headers[key] = value;
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