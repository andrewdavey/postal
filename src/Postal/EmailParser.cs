using System;
using System.Linq;
using System.IO;
using System.Net.Mail;
using System.Collections.Generic;

namespace Postal
{
    /// <summary>
    /// Converts the raw string output of a view into a <see cref="MailMessage"/>.
    /// </summary>
    class EmailParser
    {
        public EmailParser(EmailViewRenderer alternativeViewRenderer)
        {
            this.alternativeViewRenderer = alternativeViewRenderer;
        }

        readonly EmailViewRenderer alternativeViewRenderer;

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
                if (message.AlternateViews.Count == 0)
                {
                    message.Body = reader.ReadToEnd();
                    if (message.Body.StartsWith("<")) message.IsBodyHtml = true;
                }
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
            return new AlternateView(stream, contentType);
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
                default:
                    message.Headers[key] = value;
                    break;
            }
        }
    }
}