using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Postal;

namespace MvcSample.Controllers
{
    public class HtmlOnlyController : Controller
    {
        // In real code this should be injected by an IoC container.
        readonly IEmailService emailService = new EmailService();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Send(string to, string subject, string message)
        {
            dynamic email = new Email("HtmlOnly");
            email.Subject = "An HTML email";
            email.Date = DateTime.UtcNow;
            // Send the email - this uses a default System.Net.Mail.SmtpClient
            // and web.config settings to send the email.
            emailService.Send(email);

            return RedirectToAction("Sent");
        }

        public ActionResult Sent()
        {
            return View();
        }

    }
}
