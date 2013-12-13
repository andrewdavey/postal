using System.Web.Mvc;

namespace WebSample.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Samples()
        {
            return View();
        }

        public ActionResult Sent()
        {
            return View();
        }
    }
}
