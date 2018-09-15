using System.Linq;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Postal.AspNetCore;

namespace Postal.AspNetCore
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPostal(this IServiceCollection services)
        {
            if (services.Any(sd => sd.ServiceType == typeof(IEmailService)))
                return services;

            services.AddScoped<IActionResultExecutor<EmailViewResult>, EmailViewResultExecutor>();
            services.AddScoped<ITemplateService, TemplateService>();
            services.AddScoped<IEmailParser, EmailParser>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IEmailViewRender, EmailViewRender>();
            return services;
        }
    }
}