using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Postal
{
    /// <summary>
    /// Renders <see cref="Email"/> view's into raw strings using the MVC ViewEngine infrastructure.
    /// </summary>
    public class EmailViewRenderer : IEmailViewRenderer
    {
        /// <summary>
        /// Creates a new <see cref="EmailViewRenderer"/> that uses the given view engines.
        /// </summary>
        /// <param name="viewEngines">The view engines to use when rendering email views.</param>
        public EmailViewRenderer(ViewEngineCollection viewEngines)
        {
            this.viewEngines = viewEngines;
            EmailViewDirectoryName = "Emails";
        }

        readonly ViewEngineCollection viewEngines;

        /// <summary>
        /// The name of the directory in "Views" that contains the email views.
        /// By default, this is "Emails".
        /// </summary>
        public string EmailViewDirectoryName { get; set; }

        /// <summary>
        /// Renders an email view.
        /// </summary>
        /// <param name="email">The email to render.</param>
        /// <param name="viewName">Optional email view name override. If null then the email's ViewName property is used instead.</param>
        /// <returns>The rendered email view output.</returns>
        public string Render(Email email, string viewName = null)
        {
            viewName = viewName ?? email.ViewName;
            var controllerContext = CreateControllerContext();
            var view = CreateView(viewName, controllerContext);
            var viewOutput = RenderView(view, email.ViewData, controllerContext);
            return viewOutput;
        }

        ControllerContext CreateControllerContext()
        {
            // A dummy HttpContextBase that is enough to allow the view to be rendered.
            var httpContext = new HttpContextWrapper(
                new HttpContext(
                    new HttpRequest("", UrlRoot(), ""),
                    new HttpResponse(TextWriter.Null)
                )
            );
            var routeData = new RouteData();
            routeData.Values["controller"] = EmailViewDirectoryName;
            var requestContext = new RequestContext(httpContext, routeData);
            var stubController = new StubController();
            var controllerContext = new ControllerContext(requestContext, stubController);
            stubController.ControllerContext = controllerContext;
            return controllerContext;
        }

        string UrlRoot()
        {
            var httpContext = HttpContext.Current;
            if (httpContext == null)
            {
                return "http://localhost";
            }
            
            return httpContext.Request.Url.GetLeftPart(UriPartial.Authority) +
                   httpContext.Request.ApplicationPath;
        }

        IView CreateView(string viewName, ControllerContext controllerContext)
        {
            var result = viewEngines.FindView(controllerContext, viewName, null);
            if (result.View != null)
                return result.View;

            throw new Exception(
                "Email view not found for " + viewName + 
                ". Locations searched:" + Environment.NewLine +
                string.Join(Environment.NewLine, result.SearchedLocations)
            );
        }

        string RenderView(IView view, ViewDataDictionary viewData, ControllerContext controllerContext)
        {
            using (var writer = new StringWriter())
            {
                var viewContext = new ViewContext(controllerContext, view, viewData, new TempDataDictionary(), writer);
                view.Render(viewContext, writer);
                return writer.GetStringBuilder().ToString();
            }
        }

        // StubController so we can create a ControllerContext.
        class StubController : Controller { }
    }
}
