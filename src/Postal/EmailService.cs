using System.Net.Mail;
using System.Web.Mvc;

namespace Postal
{
    /// <summary>
    /// Sends email using the default <see cref="SmtpClient"/>.
    /// </summary>
    public class EmailService : IEmailService
    {
        /// <param name="viewEngines">The view engines to use when creating email views.</param>
        /// <param name="urlHostName">The host name of the website. This is for the UrlHelper used when generating Urls in a view. When null, this is determined from the current HttpContext instead.</param>
        public EmailService(ViewEngineCollection viewEngines, string urlHostName = null)
        {
            emailViewRender = new EmailViewRender(viewEngines, urlHostName);
            parser = new EmailParser();
        }

        readonly EmailViewRender emailViewRender;
        readonly EmailParser parser;

        public void Send(Email email)
        {
            using (var mailMessage = CreateMailMessage(email))
            using (var smtp = new SmtpClient())
            {
                smtp.Send(mailMessage);
            }
        }

        public MailMessage CreateMailMessage(Email email)
        {
            var emailString = emailViewRender.Render(email);
            var mailMessage = parser.Parse(emailString);
            return mailMessage;
        }
    }
}