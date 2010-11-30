using System.IO;
using System.Net.Mail;

namespace Postal
{
    /// <summary>
    /// Converts the raw string output of a view into a <see cref="MailMessage"/>.
    /// </summary>
    class EmailParser
    {
        public MailMessage Parse(string emailViewOutput)
        {
            var message = new MailMessage();
            InitializeMailMessage(message, emailViewOutput);
            return message;
        }

        void InitializeMailMessage(MailMessage message, string emailViewOutput)
        {
            using (var reader = new StringReader(emailViewOutput))
            {
                ParseHeaders(message, reader);
                message.Body = reader.ReadToEnd();
                if (message.Body.StartsWith("<")) message.IsBodyHtml = true;
            }
        }

        /// <summary>
        /// Headers are of the form "(key): (value)" e.g. "Subject: Hello, world".
        /// The headers block is terminated by an empty line.
        /// </summary>
        void ParseHeaders(MailMessage message, TextReader reader)
        {
            string line;
            while (!string.IsNullOrWhiteSpace(line = reader.ReadLine()))
            {
                var index = line.IndexOf(':');
                if (index <= 0) continue;

                var key = line.Substring(0, index).ToLowerInvariant().Trim();
                var value = line.Substring(index + 1).Trim();
                AssignEmailHeaderToMailMessage(key, value, message);
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
                default:
                    message.Headers[key] = value;
                    break;
            }
        }
    }
}