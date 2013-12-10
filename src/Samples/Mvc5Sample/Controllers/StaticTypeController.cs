using System.Web.Mvc;
using Postal;

namespace Mvc5Sample.Controllers
{
    // Example of using statically typed email data instead of dynamic.

    public class StaticTypeController : Controller
    {
        // In real code this should be injected by an IoC container.
        readonly IEmailService emailService = new EmailService();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Send()
        {
            var email = new ImportantEmail
            {
                To = "test@test.com", 
                Message = "This is important!"
            };
            emailService.Send(email);

            return RedirectToAction("sent");
        }

        public ActionResult Sent()
        {
            return View();
        }
    }

    public class ImportantEmail : Email
    {
        public string To { get; set; }
        public string Message { get; set; }
    }
}
