﻿using System;
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
        public EmailViewRenderer(ViewEngineCollection viewEngines, Uri url)
        {
            this.viewEngines = viewEngines;
            if (url != null)
            {
                this.getUrl = () => url;
            }
            else
            {
                // Delay asking for Url from HttpContext since EmailViewRenderer may be
                // created before any HttpContext exists.
                this.getUrl = () => GetUrlFromHttpContext() ?? DefaultUrlRatherThanNull();
            }

            EmailViewDirectoryName = "Emails";
        }

        readonly ViewEngineCollection viewEngines;
        readonly Func<Uri> getUrl;
        readonly Func<RouteData> getRouteData;

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
            var httpContext = new EmailHttpContext(getUrl());
            var routeData = GetRouteDataFromHttpContext() ?? new RouteData();
            routeData.Values["controller"] = EmailViewDirectoryName;
            var requestContext = new RequestContext(httpContext, routeData);
            return new ControllerContext(requestContext, new StubController());
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

        RouteData GetRouteDataFromHttpContext() {
          if (HttpContext.Current == null) return null;
          var wrapper = new HttpContextWrapper(HttpContext.Current);
          return RouteTable.Routes.GetRouteData(wrapper);
        }

        Uri GetUrlFromHttpContext()
        {
            if (HttpContext.Current == null) return null;
            return HttpContext.Current.Request.Url;
        }

        Uri DefaultUrlRatherThanNull()
        {
            return new Uri("http://localhost");
        }

        // StubController so we can create a ControllerContext.
        class StubController : Controller { }
    }
}
