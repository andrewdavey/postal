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
                ParseHeaders(message, reader);
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

        /// <summary>
        /// Headers are of the form "(key): (value)" e.g. "Subject: Hello, world".
        /// The headers block is terminated by an empty line.
        /// </summary>
        void ParseHeaders(MailMessage message, TextReader reader)
        {
            string line;
            while (string.IsNullOrWhiteSpace(line = reader.ReadLine()))
            {
                // Skip over any empty lines before the headers.
            }

            do
            {
                var index = line.IndexOf(':');
                if (index <= 0) throw new Exception("Invalid email header. Headers must be of the form 'To: hello@world.com'. Also, there must be a blank line between headers and the email body.");

                var key = line.Substring(0, index).ToLowerInvariant().Trim();
                var value = line.Substring(index + 1).Trim();
                AssignEmailHeaderToMailMessage(key, value, message);
            } while (!string.IsNullOrWhiteSpace(line = reader.ReadLine()));
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