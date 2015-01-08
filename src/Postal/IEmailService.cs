﻿using System.Net.Mail;
using System.Threading.Tasks;

namespace Postal
{
    /// <summary>
    /// Creates and send email.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Creates and sends a <see cref="MailMessage"/> using <see cref="SmtpClient"/>.
        /// This uses the default configuration for mail defined in web.config.
        /// </summary>
        /// <param name="email">The email to send.</param>
        void Send(Email email);

        /// <summary>
        /// Creates and sends a <see cref="MailMessage"/> asynchronously using <see cref="SmtpClient"/>.
        /// This uses the default configuration for mail defined in web.config.
        /// </summary>
        /// <param name="email">The email to send.</param>
        /// <returns>A <see cref="Task"/> that can be used to await completion of sending the email.</returns>
        Task SendAsync(Email email);

        /// <summary>
        /// Saves a copy of the email to a file.  Using this you can
        /// save a file and then also use the Send() function to send the email.
        /// </summary>
        /// <param name="path">The full path to the folder where you want to save the file.
        /// <param name="email">The email to send.</param>
        /// For example, "c:\emails" </param>
        void SaveToFile(Email email, string path);

        /// <summary>
        /// Creates a new <see cref="MailMessage"/> for the given email. You can
        /// modify the message, for example adding attachments, and then send this yourself.
        /// </summary>
        /// <param name="email">The email to generate.</param>
        /// <returns>A new <see cref="MailMessage"/>.</returns>
        MailMessage CreateMailMessage(Email email);
    }
}
