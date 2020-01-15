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
            CreateSmtpClient = () =>
            {
                if (Port.HasValue)
                {
                    return new SmtpClient(Host, Port.Value)
                    {
                        UseDefaultCredentials = string.IsNullOrWhiteSpace(UserName),
                        Credentials = string.IsNullOrWhiteSpace(UserName) ? null : new NetworkCredential(UserName, Password),
                        EnableSsl = EnableSSL
                    };
                }

                return new SmtpClient(Host)
                {
                    UseDefaultCredentials = string.IsNullOrWhiteSpace(UserName),
                    Credentials = string.IsNullOrWhiteSpace(UserName) ? null : new NetworkCredential(UserName, Password),
                    EnableSsl = EnableSSL
                };
            };
        }

        public string Host { get; set; }
        public int? Port { get; set; }
        public bool EnableSSL { get; set; }
        public string FromAddress { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public Func<SmtpClient> CreateSmtpClient { get; set; }
    }
}
