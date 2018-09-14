using System;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Postal.Tests")]
namespace Postal
{
    /// <summary>
    /// Sends email using the default <see cref="SmtpClient"/>.
    /// </summary>
    public class EmailService : IEmailService
    {
        /// <summary>Creates a new <see cref="EmailService"/>, using the given view engines.</summary>
        public static EmailService Create(IServiceProvider serviceProvider, Func<SmtpClient> createSmtpClient = null)
        {
            EmailViewRender emailViewRender = serviceProvider.GetRequiredService<EmailViewRender>();
            return new EmailService(emailViewRender, createSmtpClient: createSmtpClient);
        }

        /// <summary>
        /// Creates a new <see cref="EmailService"/>.
        /// </summary>
        public EmailService(IEmailViewRender emailViewRenderer, IEmailParser emailParser = null, Func<SmtpClient> createSmtpClient = null)
        {
            this.emailViewRenderer = emailViewRenderer;
            this.emailParser = emailParser;
            this.createSmtpClient = createSmtpClient ?? (() => new SmtpClient());
        }

        readonly IEmailViewRender emailViewRenderer;
        IEmailParser emailParser;
        readonly Func<SmtpClient> createSmtpClient;
        
        /// <summary>
        /// Send an email asynchronously, using an <see cref="SmtpClient"/>.
        /// </summary>
        /// <param name="email">The email to send.</param>
        /// <returns>A <see cref="Task"/> that completes once the email has been sent.</returns>
        public async Task SendAsync(Email email)
        {
            // Wrap the SmtpClient's awkward async API in the much nicer Task pattern.
            // However, we must be careful to dispose of the resources we create correctly.
            var mailMessage = await CreateMailMessageAsync(email);
            try
            {
                var smtp = createSmtpClient();
                try
                {
                    //var taskCompletionSource = new TaskCompletionSource<object>();

                    //smtp.SendCompleted += (o, e) =>
                    //{
                    //    smtp.Dispose();
                    //    mailMessage.Dispose();

                    //    if (e.Error != null)
                    //    {
                    //        taskCompletionSource.TrySetException(e.Error);
                    //    }
                    //    else if (e.Cancelled)
                    //    {
                    //        taskCompletionSource.TrySetCanceled();
                    //    }
                    //    else // Success
                    //    {
                    //        taskCompletionSource.TrySetResult(null);
                    //    }
                    //};

                    //smtp.SendAsync(mailMessage, null);
                    //await taskCompletionSource.Task;
                    await smtp.SendMailAsync(mailMessage);
                }
                catch
                {
                    smtp.Dispose();
                    throw;
                }
            }
            catch
            {
                mailMessage.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Renders the email view and builds a <see cref="MailMessage"/>. Does not send the email.
        /// </summary>
        /// <param name="email">The email to render.</param>
        /// <returns>A <see cref="MailMessage"/> containing the rendered email.</returns>
        public async Task<MailMessage> CreateMailMessageAsync(Email email)
        {
            var rawEmailString = await emailViewRenderer.RenderAsync(email);
            emailParser = new EmailParser(emailViewRenderer);
            var mailMessage = await emailParser.ParseAsync(rawEmailString, email);
            return mailMessage;
        }
    }
}