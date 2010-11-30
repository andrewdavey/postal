using System.Net.Mail;

namespace Postal
{
    public interface IEmailService
    {
        /// <summary>
        /// Creates and sends a <see cref="MailMessage"/> using <see cref="SmtpClient"/>.
        /// This uses the default configuration for mail defined in web.config.
        /// </summary>
        /// <param name="email">The email to send.</param>
        void Send(Email email);

        /// <summary>
        /// Creates a new <see cref="MailMessage"/> for the given email. You can
        /// modify the message, for example adding attachments, and then send this yourself.
        /// </summary>
        /// <param name="email">The email to generate.</param>
        /// <returns>A new <see cref="MailMessage"/>.</returns>
        MailMessage CreateMailMessage(Email email);
    }
}
