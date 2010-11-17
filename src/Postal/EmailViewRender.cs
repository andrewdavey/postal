using System;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Postal
{
    class EmailViewRender
    {
        public EmailViewRender(ViewEngineCollection viewEngines, string urlHostName)
        {
            this.viewEngines = viewEngines;
            this.urlHostName = urlHostName ?? GetHostNameFromHttpContext();
        }

        readonly ViewEngineCollection viewEngines;
        readonly string urlHostName;

        public string Render(Email email)
        {
            var controllerContext = CreateControllerContext(urlHostName);
            var view = CreateView(email.ViewName, controllerContext);
            if (view == null) throw new Exception("View not found for email: " + email.ViewName);

            var emailString = RenderView(view, email.ViewData, controllerContext);
            return emailString;
        }

        string GetHostNameFromHttpContext()
        {
            var url = HttpContext.Current.Request.Url;
            if (url.IsDefaultPort)
            {
                return url.Host;
            }
            else
            {
                return url.Host + ":" + url.Port;
            }
        }

        IView CreateView(string viewName, ControllerContext controllerContext)
        {
            var result = viewEngines.FindView(controllerContext, viewName, null);
            return result.View;
        }

        ControllerContext CreateControllerContext(string urlHostName)
        {
            var httpContext = new EmailHttpContext(urlHostName);
            var routeData = new RouteData();
            routeData.Values["controller"] = "Emails";
            var requestContext = new RequestContext(httpContext, routeData);
            return new ControllerContext(requestContext, new StubController());
        }

        string RenderView(IView view, ViewDataDictionary viewData, ControllerContext controllerContext)
        {
            var builder = new StringBuilder();
            using (var writer = new StringWriter(builder))
            {
                var viewContext = new ViewContext(controllerContext, view, viewData, new TempDataDictionary(), writer);
                view.Render(viewContext, writer);
            }
            return builder.ToString();
        }

        class StubController : Controller { }
    }
}
