using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Collections.Generic;

namespace Postal
{
    /// <summary>
    /// Renders <see cref="Email"/> view's into raw strings using the MVC ViewEngine infrastructure.
    /// </summary>
    public class EmailViewRenderer : IEmailViewRenderer
    {
        public EmailViewRenderer(ViewEngineCollection viewEngines, string urlHostName)
        {
            this.viewEngines = viewEngines;
            this.urlHostName = urlHostName ?? GetHostNameFromHttpContext();
            EmailViewDirectoryName = "Emails";
        }

        readonly ViewEngineCollection viewEngines;
        readonly string urlHostName;

        /// <summary>
        /// The name of the directory in "Views" that contains the email views.
        /// By default, this is "Emails".
        /// </summary>
        public string EmailViewDirectoryName { get; set; }

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
            var httpContext = new EmailHttpContext(urlHostName);
            var routeData = new RouteData();
            routeData.Values["controller"] = EmailViewDirectoryName;
            var requestContext = new RequestContext(httpContext, routeData);
            return new ControllerContext(requestContext, new StubController());
        }

        IView CreateView(string viewName, ControllerContext controllerContext)
        {
            var result = viewEngines.FindPartialView(controllerContext, viewName);
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

        string GetHostNameFromHttpContext()
        {
            var url = HttpContext.Current.Request.Url;
            if (url.IsDefaultPort) return url.Host;
            return url.Host + ":" + url.Port;
        }

        // StubController so we can create a ControllerContext.
        class StubController : Controller { }
    }
}
