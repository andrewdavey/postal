using System;
using System.Net.Mail;

namespace Postal
{
    public interface IEmailParser
    {
        MailMessage Parse(string emailViewOutput, Email email);
    }
}
