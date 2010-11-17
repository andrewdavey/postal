using System;
using System.Web.Mvc;
using Postal;

namespace MvcSample.Controllers
{
    public class HomeController : Controller
    {
        // In real code this should be injected by an IoC container.
        IEmailSender sender = new EmailSender(ViewEngines.Engines);

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Send(string to, string subject, string message)
        {
            dynamic email = new Email("Example");
            email.To = to;
            email.Subject = subject;
            email.Message = message;
            email.Date = DateTime.UtcNow;

            sender.Send(email);

            return RedirectToAction("Sent");
        }

        public ActionResult Sent()
        {
            return View();
        }
    }
}
