using System;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Postal.AspNetCore;
using Microsoft.Extensions.Logging;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Postal.Tests")]
namespace Postal
{
    /// <summary>
    /// Sends email using the default <see cref="SmtpClient"/>.
    /// </summary>
    public class EmailService : IEmailService
    {
        /// <summary>Creates a new <see cref="EmailService"/>, using the given view engines.</summary>
        [Obsolete]
        public static EmailService Create(IServiceProvider serviceProvider, Func<SmtpClient> createSmtpClient = null)
        {
            var emailViewRender = serviceProvider.GetRequiredService<IEmailViewRender>();
            var emailParser = serviceProvider.GetRequiredService<IEmailParser>();
            var options = Options.Create(new EmailServiceOptions() { CreateSmtpClient = createSmtpClient });
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<EmailService>();
            return new EmailService(emailViewRender, emailParser, options, logger);
        }

        /// <summary>
        /// Creates a new <see cref="EmailService"/>.
        /// </summary>
        public EmailService(IEmailViewRender emailViewRenderer, IEmailParser emailParser, IOptions<EmailServiceOptions> options, ILogger<EmailService> logger)
        {
            this.emailViewRenderer = emailViewRenderer;
            this.emailParser = emailParser;
            this.options = options.Value;
            this.logger = logger;

            logger.LogDebug($"EmailService options:");
            logger.LogDebug($"\tHost: {this.options.Host}");
            logger.LogDebug($"\tPort: {this.options.Port}");
            logger.LogDebug($"\tEnableSSL: {this.options.EnableSSL}");
            logger.LogDebug($"\tFromAddress: {this.options.FromAddress}");
            logger.LogDebug($"\tUserName: {this.options.UserName}");
        }

        protected readonly IEmailViewRender emailViewRenderer;
        protected IEmailParser emailParser;
        protected EmailServiceOptions options;
        protected ILogger<EmailService> logger;

        //for unit testing
        internal Func<SmtpClient> CreateSmtpClient
        {
            get
            {
                return options.CreateSmtpClient;
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
            // However, we must be careful to dispose of the resources we create correctly.
            using (var mailMessage = await CreateMailMessageAsync(email))
            {
                await SendAsync(mailMessage);
            }
        }

        /// <summary>
        /// Send an email asynchronously, using an <see cref="SmtpClient"/>.
        /// </summary>
        /// <param name="email">The email to send.</param>
        /// <returns>A <see cref="Task"/> that completes once the email has been sent.</returns>
        public async Task SendAsync(MailMessage mailMessage)
        {
            using (var smtp = options.CreateSmtpClient())
            {
                this.logger.LogDebug($"Smtp created: host: {smtp.Host}, port: {smtp.Port}, enableSsl: {smtp.EnableSsl}, defaultCredentials: {smtp.UseDefaultCredentials}");
                this.logger.LogInformation($"Smtp send email from {mailMessage.From} to {mailMessage.To}");
                await smtp.SendMailAsync(mailMessage);
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
            if (mailMessage.From == null || mailMessage.From.Address == null)
            {
                mailMessage.From = new MailAddress(this.options.FromAddress);
            }
            return mailMessage;
        }
    }
}