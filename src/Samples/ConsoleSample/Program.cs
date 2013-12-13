using System.IO;
using System.Web.Mvc;
using Postal;

namespace ConsoleSample
{
    /* 
    Before running this sample, please start the SMTP development server,
    found in the Postal code directory: tools\smtp4dev.exe

    Use the SMTP development server to inspect the contents of generated email (headers, content, etc).
    No email is really sent, so it's perfect for debugging.
    */

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
