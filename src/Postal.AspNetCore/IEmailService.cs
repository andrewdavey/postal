using System.Net.Mail;
using System.Threading.Tasks;

namespace Postal
{
    /// <summary>
    /// Creates and send email.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Creates and sends a <see cref="MailMessage"/> asynchronously using <see cref="SmtpClient"/>.
        /// This uses the default configuration for mail defined in web.config.
        /// </summary>
        /// <param name="email">The email to send.</param>
        /// <returns>A <see cref="Task"/> that can be used to await completion of sending the email.</returns>
        Task SendAsync(Email email);

        /// <summary>
        /// Creates a new <see cref="MailMessage"/> for the given email. You can
        /// modify the message, for example adding attachments, and then send this yourself.
        /// </summary>
        /// <param name="email">The email to generate.</param>
        /// <returns>A new <see cref="MailMessage"/>.</returns>
        Task<MailMessage> CreateMailMessageAsync(Email email);
    }
}
