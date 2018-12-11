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
        /// Send an email asynchronously, using an <see cref="SmtpClient"/>.
        /// </summary>
        /// <param name="email">The email to send.</param>
        /// <returns>A <see cref="Task"/> that completes once the email has been sent.</returns>
        Task SendAsync(MailMessage email);

        /// <summary>
        /// Send an email asynchronously, using an <see cref="SmtpClient"/>.
        /// </summary>
        /// <param name="email">The email to send.</param>
        /// <returns>A <see cref="Task"/> that completes once the email has been sent.</returns>
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
