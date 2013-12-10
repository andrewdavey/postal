using System.Web.Mvc;
using Postal;

namespace ResourceSample
{
    class Program
    {
        static void Main()
        {
            var engines = new ViewEngineCollection
                          {
                              new ResourceRazorViewEngine(typeof(Program).Assembly, @"ResourceSample.Resources.Views")
                          };

            var service = new EmailService(engines);

            dynamic email = new Email("Test");
            email.Message = "Hello, non-asp.net world!";
            service.Send(email);
        }
    }

}
