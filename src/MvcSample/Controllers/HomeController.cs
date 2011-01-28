using System;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using Postal;

namespace MvcSample.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Send(string to, string subject, string message, HttpPostedFileBase file)
        {
            // This will look for a view in "~/Views/Emails/Example.cshtml".
            dynamic email = new Email("Example");
            // Assign any view data to pass to the view.
            // It's dynamic, so you can put whatever you want here.
            email.To = to;
            email.Subject = subject;
            email.Message = message;
            email.Date = DateTime.UtcNow;

            if (file != null)
            {
                email.Attach(new Attachment(file.InputStream, file.FileName));
            }

            // Send the email via a default Postal.EmailService object.
            // This will use the web.config smtp settings.
            email.Send();

            
            // In 'real code' you probably want to use the Postal.IEmailService interface
            // to allow for mocking out the sending of email in tests.
            // 
            // The controller's constructor would look like this:
            //   public HomeController(IEmailService emailService) { 
            //     this.emailService = emailService;
            //   }
            // 
            // Then actions can send email using:
            //   emailService.Send(email);

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
