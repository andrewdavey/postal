using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Postal.AspNetCore
{
    public class EmailServiceOptions
    {
        public EmailServiceOptions()
        {
            CreateSmtpClient = () => new SmtpClient(Host, Port)
            {
                Credentials = new NetworkCredential(UserName, Password),
                EnableSsl = EnableSSL
            };
        }

        public string Host { get; set; }
        public int Port { get; set; }
        public bool EnableSSL { get; set; }
        public string FromAddress { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public Func<SmtpClient> CreateSmtpClient { get; set; }
    }
}
