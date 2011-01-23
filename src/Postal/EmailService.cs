using System.Net.Mail;
using System.Web.Mvc;

namespace Postal
{
    /// <summary>
    /// Sends email using the default <see cref="SmtpClient"/>.
    /// </summary>
    public class EmailService : IEmailService
    {
        public EmailService() : this(ViewEngines.Engines, null)
        {
        }

        /// <param name="viewEngines">The view engines to use when creating email views.</param>
        /// <param name="urlHostName">The host name of the website. This is for the UrlHelper used when generating Urls in a view. When null, this is determined from the current HttpContext instead.</param>
        public EmailService(ViewEngineCollection viewEngines, string urlHostName = null)
        {
            emailViewRenderer = new EmailViewRenderer(viewEngines, urlHostName);
            emailParser = new EmailParser(emailViewRenderer);
        }

        public EmailService(IEmailViewRenderer emailViewRenderer, IEmailParser emailParser)
        {
            this.emailViewRenderer = emailViewRenderer;
            this.emailParser = emailParser;
        }

        readonly IEmailViewRenderer emailViewRenderer;
        readonly IEmailParser emailParser;

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
            var rawEmailString = emailViewRenderer.Render(email);
            var mailMessage = emailParser.Parse(rawEmailString, email);
            return mailMessage;
        }
    }
}