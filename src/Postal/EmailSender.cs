using System.Net.Mail;
using System.Web.Mvc;

namespace Postal
{
    public class EmailSender : IEmailSender
    {
        /// <param name="urlHostName">The host name of the website. This is for the UrlHelper used when generating Urls in a view.</param>
        public EmailSender(ViewEngineCollection viewEngines, string urlHostName = null)
        {
            emailViewRender = new EmailViewRender(viewEngines, urlHostName);
            parser = new EmailParser();
        }

        readonly EmailViewRender emailViewRender;
        readonly EmailParser parser;

        public void Send(Email email)
        {
            var emailString = emailViewRender.Render(email);
            using (var mailMessage = parser.Parse(emailString))
            using (var smtp = new SmtpClient())
            {
                smtp.Send(mailMessage);
            }
        }
    }
}