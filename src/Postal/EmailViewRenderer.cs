using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if ASPNET5
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
#else
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
#endif

namespace Postal
{
    /// <summary>
    /// Renders <see cref="Email"/> view's into raw strings using the MVC ViewEngine infrastructure.
    /// </summary>
    public class EmailViewRenderer : IEmailViewRenderer
    {
#if ASPNET5
        /// <summary>
        /// Creates a new <see cref="EmailViewRenderer"/> that uses the given view engines.
        /// </summary>
        /// <param name="viewEngines">The view engines to use when rendering email views.</param>
        public EmailViewRenderer(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            EmailViewDirectoryName = "Emails";
        }

        readonly IServiceProvider serviceProvider;

#else
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
#endif

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
#if ASPNET5
        public string Render(Email email, IHttpRequestFeature requsetFeature, string viewName = null)
#else
        public string Render(Email email, string viewName = null, HttpRequestBase request = null)
#endif
        {
            viewName = viewName ?? email.ViewName;
#if ASPNET5
            var controllerContext = CreateControllerContext(email.AreaName, requsetFeature);
#else
            var controllerContext = CreateControllerContext(email.AreaName, request);
#endif
            var view = CreateView(viewName, controllerContext);
            var viewOutput = RenderView(view, email.ViewData, controllerContext, email.ImageEmbedder);
            return viewOutput;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="areaName">The name of the area containing the Emails view folder if applicable</param>
        /// <returns></returns>
#if ASPNET5
        ActionContext CreateControllerContext(string areaName, HttpRequest request)
        {
            var routeData = new Microsoft.AspNetCore.Routing.RouteData();
            routeData.Values["controller"] = EmailViewDirectoryName;

            // if populated will add searching the named Area for the view
            if (!string.IsNullOrWhiteSpace(areaName))
                routeData.DataTokens["Area"] = areaName;

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.RouteValues = routeData.Values.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
            FeatureCollection featureCollection = new FeatureCollection();
            var requsetFeature_local = new HttpRequestFeature();
            requsetFeature_local.Method = "GET";
            requsetFeature_local.Protocol = request.Protocol;
            requsetFeature_local.PathBase = request.PathBase;
            requsetFeature_local.Scheme = request.Scheme;
            featureCollection.Set<IHttpRequestFeature>(requsetFeature_local);
            featureCollection.Set<IHttpResponseFeature>(new HttpResponseFeature());
            var httpContext = new DefaultHttpContext(featureCollection);
            var actionContext = new ActionContext(httpContext, routeData, actionDescriptor);
            actionContext.RouteData = routeData;
            
            return actionContext;
        }
#else
        ControllerContext CreateControllerContext(string areaName, HttpRequestBase request = null)
        {
            // A dummy HttpContextBase that is enough to allow the view to be rendered.
            var httpContext = new HttpContextWrapper(
                new HttpContext(
                    new HttpRequest("", UrlRoot(request), ""),
                    new HttpResponse(TextWriter.Null)
                )
            );
            var routeData = new RouteData();
            routeData.Values["controller"] = EmailViewDirectoryName;

            // if populated will add searching the named Area for the view
            if (!string.IsNullOrWhiteSpace(areaName))
                routeData.DataTokens["Area"] = areaName;

            var requestContext = new RequestContext(httpContext, routeData);
            var stubController = new StubController();
            var controllerContext = new ControllerContext(requestContext, stubController);
            stubController.ControllerContext = controllerContext;
            return controllerContext;
        }
#endif

#if !ASPNET5
        string UrlRoot(HttpRequestBase request = null)
        {
            if (request != null)
            {
                return request.Url.GetLeftPart(UriPartial.Authority) + request.ApplicationPath;
            }
            var httpContext = HttpContext.Current;
            if (httpContext == null)
            {
                return "http://localhost";
            }

            return httpContext.Request.Url.GetLeftPart(UriPartial.Authority) +
                   httpContext.Request.ApplicationPath;
        }
#endif

#if ASPNET5
        IView CreateView(string viewName, ActionContext context)
        {
            //https://github.com/aspnet/Mvc/blob/dev/src/Microsoft.AspNetCore.Mvc.ViewFeatures/ViewEngines/CompositeViewEngine.cs
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MvcViewOptions>>();
            var viewEngines = options.Value.ViewEngines;
            if (viewEngines.Count == 0)
            {
                throw new InvalidOperationException($"No view engines when rendering email {viewName} :  {typeof(MvcViewOptions).FullName}, {nameof(MvcViewOptions.ViewEngines)}, {typeof(IViewEngine).FullName}");
            }
            // Do not allocate in the common cases: ViewEngines contains one entry or initial attempt is successful.
            IEnumerable<string> searchedLocations = null;
            List<string> searchedList = null;
            for (var i = 0; i < viewEngines.Count; i++)
            {
                var result = viewEngines[i].FindView(context, viewName, false);
                if (result.Success)
                {
                    return result.View;
                }
                if (searchedLocations == null)
                {
                    // First failure.
                    searchedLocations = result.SearchedLocations;
                }
                else
                {
                    if (searchedList == null)
                    {
                        // Second failure.
                        searchedList = new List<string>(searchedLocations);
                        searchedLocations = searchedList;
                    }

                    searchedList.AddRange(result.SearchedLocations);
                }
            }

            throw new Exception(
                $"Email view not found for {viewName}. " +
                $"Locations searched: \r\n{string.Join(Environment.NewLine, searchedList)}");
        }
#else
        IView CreateView(string viewName, ControllerContext controllerContext)
        {
            var result = viewEngines.FindView(controllerContext, viewName, null);
            if (result.View != null)
                return result.View;

            throw new Exception(
                $"Email view not found for {viewName}. " +
                $"Locations searched: \r\n{string.Join(Environment.NewLine, result.SearchedLocations)}");
        }
#endif

#if ASPNET5
        string RenderView(IView view, ViewDataDictionary viewData, ActionContext actionContext, ImageEmbedder imageEmbedder)
        {
            //https://github.com/aspnet/Mvc/blob/master/src/Microsoft.AspNetCore.Mvc.ViewFeatures/ViewFeatures/ViewExecutor.cs
            var response = actionContext.HttpContext.Response;
            var tempDataDictionaryFactory = serviceProvider.GetRequiredService<ITempDataDictionaryFactory>();
            var viewOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<MvcViewOptions>>();
            using (var writer = new StringWriter())
            {
                var tempData = tempDataDictionaryFactory.GetTempData(actionContext.HttpContext);
                var viewContext = new ViewContext(actionContext, view, viewData, tempData, writer, viewOptions.Value.HtmlHelperOptions);
                viewData[ImageEmbedder.ViewDataKey] = imageEmbedder;
                view.RenderAsync(viewContext);
                viewData.Remove(ImageEmbedder.ViewDataKey);
                return writer.GetStringBuilder().ToString();
            }
        }

#else
        string RenderView(IView view, ViewDataDictionary viewData, ControllerContext controllerContext, ImageEmbedder imageEmbedder)
        {
            var tempData = new TempDataDictionary();
            using (var writer = new StringWriter())
            {
                var viewContext = new ViewContext(controllerContext, view, viewData, tempData, writer);
                viewData[ImageEmbedder.ViewDataKey] = imageEmbedder;
                view.Render(viewContext, writer);
                viewData.Remove(ImageEmbedder.ViewDataKey);
                return writer.GetStringBuilder().ToString();
            }
        }
#endif

        // StubController so we can create a ControllerContext.
        class StubController : Controller { }
    }
}
