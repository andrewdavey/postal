using System;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Postal
{
    /// <summary>
    /// Sends email using the default <see cref="SmtpClient"/>.
    /// </summary>
    public class EmailService : IEmailService
    {
        /// <summary>
        /// Creates a new cref="EmailService"/, using the default view engines.
        /// </summary>
        public EmailService() : this(ViewEngines.Engines)
        {
        }

        /// <summary>Creates a new <see cref="EmailService"/>, using the given view engines.</summary>
        /// <param name="viewEngines">The view engines to use when creating email views.</param>
        /// <param name="createSmtpClient">A function that creates a <see cref="SmtpClient"/>. If null, a default creation function is used.</param>
        public EmailService(ViewEngineCollection viewEngines, Func<SmtpClient> createSmtpClient = null)
        {
            emailViewRenderer = new EmailViewRenderer(viewEngines);
            emailParser = new EmailParser(emailViewRenderer);
            this.createSmtpClient = createSmtpClient ?? (() => new SmtpClient());
        }

        /// <summary>
        /// Creates a new <see cref="EmailService"/>.
        /// </summary>
        public EmailService(IEmailViewRenderer emailViewRenderer, IEmailParser emailParser, Func<SmtpClient> createSmtpClient)
        {
            this.emailViewRenderer = emailViewRenderer;
            this.emailParser = emailParser;
            this.createSmtpClient = createSmtpClient;
        }

        readonly IEmailViewRenderer emailViewRenderer;
        readonly IEmailParser emailParser;
        readonly Func<SmtpClient> createSmtpClient;

        /// <summary>
        /// Sends an email using an <see cref="SmtpClient"/>.
        /// </summary>
        /// <param name="email">The email to send.</param>
        public void Send(Email email)
        {
            using (var mailMessage = CreateMailMessage(email))
            using (var smtp = createSmtpClient())
            {
                smtp.Send(mailMessage);
            }
        }

        /// <summary>
        /// Send an email asynchronously, using an <see cref="SmtpClient"/>.
        /// </summary>
        /// <param name="email">The email to send.</param>
        /// <returns>A <see cref="Task"/> that completes once the email has been sent.</returns>
        public async Task SendAsync(Email email)
        {
            // Wrap the SmtpClient's awkward async API in the much nicer Task pattern.
            using (var mailMessage = CreateMailMessage(email))
            { 
                using (var smtp = createSmtpClient())
                {
#if NET45
                    await smtp.SendMailAsync(mailMessage);
#else
                    await smtp.SendTaskAsync(mailMessage);
#endif
                }
            }
        }

        /// <summary>
        /// Renders the email view and builds a <see cref="MailMessage"/>. Does not send the email.
        /// </summary>
        /// <param name="email">The email to render.</param>
        /// <returns>A <see cref="MailMessage"/> containing the rendered email.</returns>
        public MailMessage CreateMailMessage(Email email)
        {
            var rawEmailString = emailViewRenderer.Render(email);
            var mailMessage = emailParser.Parse(rawEmailString, email);
            return mailMessage;
        }
    }
}