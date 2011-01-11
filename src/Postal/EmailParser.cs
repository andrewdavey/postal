using System;
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
        public MailMessage Parse(Tuple<string, Dictionary<string, string>> emailViewOutput)
        {
            var message = new MailMessage();
            InitializeMailMessage(message, emailViewOutput);
            return message;
        }

        void InitializeMailMessage(MailMessage message, Tuple<string, Dictionary<string, string>> emailViewOutput)
        {
            using (var reader = new StringReader(emailViewOutput.Item1))
            {
                ParserUtils.ParseHeaders(reader, (key, value) => AssignEmailHeaderToMailMessage(key, value, message));
                if (emailViewOutput.Item2 == null)
                {
                    message.Body = reader.ReadToEnd();
                    if (message.Body.StartsWith("<")) message.IsBodyHtml = true;
                }
                else
                {
                    InitializeMailMessageWithAlternativeViews(message, emailViewOutput.Item2);
                }
            }
        }

        void InitializeMailMessageWithAlternativeViews(MailMessage message, Dictionary<string, string> parts)
        {
            foreach (var part in parts)
            {
                var stream = new MemoryStream();
                var writer = new StreamWriter(stream);
                
                writer.Write(part.Value);
                writer.Flush();
                stream.Position = 0;
                message.AlternateViews.Add(new AlternateView(stream, part.Key));
                
                // I assume AlternativeView will Dispose the stream for us!
            }
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