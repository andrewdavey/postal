using System;
using System.Web.Mvc;
using Postal;

namespace WebSample.Controllers
{
    public class EmailController : Controller
    {
        [HttpPost]
        public ActionResult SendSimple()
        {
            dynamic email = new Email("Simple");
            email.Date = DateTime.UtcNow.ToString();
            email.Send();

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public ActionResult SendMultiPart()
        {
            dynamic email = new Email("MultiPart");
            email.Date = DateTime.UtcNow.ToString();
            email.Send();

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public ActionResult SendTypedEmail()
        {
            var email = new TypedEmail();
            email.Date = DateTime.UtcNow.ToString();
            email.Send();

            return RedirectToAction("Index", "Home");
        }
    }

    public class TypedEmail : Email
    {
        public string Date { get; set; }
    }
}
