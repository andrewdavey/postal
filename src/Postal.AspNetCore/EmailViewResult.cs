using System;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
#if ASPNET5
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
#else
using System.Web.Mvc;
#endif

namespace Postal
{
    /// <summary>
    /// Renders a preview of an email to display in the browser.
    /// </summary>
    public class EmailViewResult : ViewResult
    {
        public Email Email { get; private set; }

        /// <summary>
        /// Creates a new <see cref="EmailViewResult"/>.
        /// </summary>
        public EmailViewResult(Email email)
        {
            Email = email;
        }

        /// <summary>
        /// When called by the action invoker, renders the view to the response.
        /// </summary>
        public override async Task ExecuteResultAsync(ActionContext context)
        {
            var executor = context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<EmailViewResult>>();
            await executor.ExecuteAsync(context, this);
        }
    }
}
