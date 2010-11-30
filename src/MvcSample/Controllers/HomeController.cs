using System;
using System.Web.Mvc;
using Postal;

namespace MvcSample.Controllers
{
    public class HomeController : Controller
    {
        // In real code this should be injected by an IoC container.
        readonly IEmailService emailService = new EmailService(ViewEngines.Engines);

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Send(string to, string subject, string message)
        {
            // This will look for a view in "~/Views/Emails/Example.cshtml".
            dynamic email = new Email("Example");
            // Assign any view data to pass to the view.
            // It's dynamic, so you can put whatever you want here.
            email.To = to;
            email.Subject = subject;
            email.Message = message;
            email.Date = DateTime.UtcNow;

            // Send the email - this uses a default System.Net.Mail.SmtpClient
            // and web.config settings to send the email.
            emailService.Send(email);

            // Alternatively, you can just ask for the MailMessage to be created.
            // It contains the rendered email body and headers (To, From, etc).
            // You can then send this yourself using any method you like.
            // using (var mailMessage = emailService.CreateMailMessage(email))
            // {
            //     MyEmailGateway.Send(mailMessage);
            // }

            return RedirectToAction("Sent");
        }

        public ActionResult Sent()
        {
            return View();
        }
    }
}
