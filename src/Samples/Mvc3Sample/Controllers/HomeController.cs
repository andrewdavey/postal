using System.Web.Mvc;
using Postal;

namespace Mvc3Sample.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            dynamic email = new Email("Test");
            email.Send();

            return Content("Email sent");
        }

    }
}
