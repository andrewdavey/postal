using System.Linq;
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
    }
}