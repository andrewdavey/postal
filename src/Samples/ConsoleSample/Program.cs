using System.IO;
using System.Net.Mail;
using System.Web.Mvc;
using Postal;

namespace ConsoleSample
{
    class Program // That's right, no asp.net runtime required!
    {
        static void Main(string[] args)
        {
            // Get the path to the directory containing views
            var viewsPath = Path.GetFullPath(@"..\..\Views");

            var engines = new ViewEngineCollection();
            engines.Add(new FileSystemRazorViewEngine(viewsPath));

            var service = new EmailService(engines);

            dynamic email = new Email("Test");
            email.Message = "Hello, non-asp.net world!";
            service.Send(email);

            // Alternatively, set the service factory like this:
            /*
            Email.CreateEmailService = () => new EmailService(engines);
            
            dynamic email = new Email("Test");
            email.Message = "Hello, non-asp.net world!";
            email.Send();
            */
        }
    }

}
