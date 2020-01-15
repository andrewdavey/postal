using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Postal.AspNetCore;

namespace Postal.AspNetCore
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPostal(this IServiceCollection services)
        {
            services.TryAddScoped<IActionResultExecutor<EmailViewResult>, EmailViewResultExecutor>();
            services.TryAddScoped<ITemplateService, TemplateService>();
            services.TryAddScoped<IEmailParser, EmailParser>();
            services.TryAddScoped<IEmailService, EmailService>();
            services.TryAddScoped<IEmailViewRender, EmailViewRender>();
            return services;
        }

        public static IServiceCollection ConfigurePostal(this IServiceCollection services, string smtpConnectionString)
        {
            var match = Regex.Match(smtpConnectionString, @"^smtp:\/\/((?<username>[^:@]+):(?<password>[^:@]+)@)?(?<host>[^:@,\/?#]+)(:(?<port>\d+))?(\?(?<option>[^&]+=[^&]+)(&(?<option>[^&]+=[^&]+))*)?$");

            if (!match.Success)
            {
                throw new Exception(string.Format("The connection string '{0}' is not valid.", smtpConnectionString));
            }

            var host = match.Groups["host"].Value;
            var userName = match.Groups["username"].Value;
            var password = match.Groups["password"].Value;

            int? port = default;
            bool enableSsl = default;
            string fromAddress = default;

            var portGroup = match.Groups["port"];

            if (string.IsNullOrWhiteSpace(portGroup.Value) == false)
            {
                port = int.Parse(portGroup.Value);
            }

            var optionCaptures = match.Groups["option"].Captures;

            foreach (Capture optionCapture in optionCaptures)
            {
                var parts = optionCapture.Value.Split(new[] { '=' }, 2);

                switch (parts[0])
                {
                    case "enableSsl":
                        bool.TryParse(parts[1], out enableSsl);
                        break;

                    case "fromAddress":
                        fromAddress = parts[1];
                        break;
                }
            }

            services.Configure<EmailServiceOptions>(options =>
            {
                options.Host = host;
                options.Port = port;
                options.UserName = userName;
                options.Password = password;
                options.EnableSSL = enableSsl;
                options.FromAddress = fromAddress;
            });

            return services;
        }
    }
}