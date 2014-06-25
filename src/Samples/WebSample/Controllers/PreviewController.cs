using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Postal;

namespace WebSample.Controllers
{
    public class PreviewController : Controller
    {
        public ActionResult Simple()
        {
            dynamic email = new Email("Simple");
            email.Date = DateTime.UtcNow.ToString();
            
            return new EmailViewResult(email);
        }

        public ActionResult SimpleHtml()
        {
            dynamic email = new Email("SimpleHtml");
            email.Date = DateTime.UtcNow.ToString();

            return new EmailViewResult(email);
        }
        
        public ActionResult MultiPart()
        {
            dynamic email = new Email("MultiPart");
            email.Date = DateTime.UtcNow.ToString();
            
            return new EmailViewResult(email);
        }

        public ActionResult Typed()
        {
            var email = new TypedEmail();
            email.Date = DateTime.UtcNow.ToString();
            
            return new EmailViewResult(email);
        }
    }
}