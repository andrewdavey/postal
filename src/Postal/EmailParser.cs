using System.IO;
using System.Net.Mail;

namespace Postal
{
    class EmailParser
    {
        public MailMessage Parse(string email)
        {
            var message = new MailMessage();
            InitializeMailMessage(message, email);
            return message;
        }

        void InitializeMailMessage(MailMessage message, string viewOutput)
        {
            using (var reader = new StringReader(viewOutput))
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
        void ParseHeaders(MailMessage message, StringReader reader)
        {
            string line;
            while (!string.IsNullOrWhiteSpace(line = reader.ReadLine()))
            {
                var index = line.IndexOf(':');
                if (index > 0)
                {
                    var key = line.Substring(0, index).ToLowerInvariant().Trim();
                    var value = line.Substring(index + 1).Trim();

                    if (key == "to") message.To.Add(value);
                    else if (key == "from") message.From = new MailAddress(value);
                    else if (key == "subject") message.Subject = value;
                    else if (key == "cc") message.CC.Add(value);
                    else if (key == "bcc") message.Bcc.Add(value);
                    else message.Headers[key] = value;
                }
            }
        }
    }
}