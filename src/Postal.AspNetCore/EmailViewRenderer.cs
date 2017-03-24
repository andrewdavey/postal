using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

#if ASPNET5
        /// <summary>
        /// Renders an email view.
        /// </summary>
        /// <param name="email">The email to render.</param>
        /// <param name="requsetFeature">IHttpRequestFeature</param>
        /// <param name="viewName">Optional email view name override. If null then the email's ViewName property is used instead.</param>
        /// <returns>The rendered email view output.</returns>
        public virtual string Render(Email email, RequestUrl url, string viewName = null)
#else
        /// <summary>
        /// Renders an email view.
        /// </summary>
        /// <param name="email">The email to render.</param>
        /// <param name="viewName">Optional email view name override. If null then the email's ViewName property is used instead.</param>
        /// <param name="request">Optional HttpRequestBase.</param>
        /// <returns>The rendered email view output.</returns>
        public string Render(Email email, string viewName = null, HttpRequestBase request = null)
#endif
        {
            viewName = viewName ?? email.ViewName;
#if ASPNET5
            var controllerContext = CreateControllerContext(email.AreaName, url);
            var view = CreateView(viewName, controllerContext);
            var viewOutput = RenderView(view, email.ViewData, controllerContext, email.ImageEmbedder).Result;
#else
            var controllerContext = CreateControllerContext(email.AreaName, request);
            var view = CreateView(viewName, controllerContext);
            var viewOutput = RenderView(view, email.ViewData, controllerContext, email.ImageEmbedder);
#endif
            return viewOutput;
        }

#if ASPNET5
        /// <summary>
        /// 
        /// </summary>
        /// <param name="areaName">The name of the area containing the Emails view folder if applicable</param>
        /// <param name="requsetFeature">IHttpRequestFeature</param>
        /// <returns></returns>
        ActionContext CreateControllerContext(string areaName, RequestUrl url)
        {
            var routeData = new Microsoft.AspNetCore.Routing.RouteData();
            routeData.Values["controller"] = EmailViewDirectoryName;

            // if populated will add searching the named Area for the view
            if (!string.IsNullOrWhiteSpace(areaName))
            {
                routeData.Values["area"] = areaName;
                routeData.DataTokens["area"] = areaName;
            }

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.RouteValues = routeData.Values.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
            FeatureCollection featureCollection = new FeatureCollection();
            var requsetFeature_local = new HttpRequestFeature();
            requsetFeature_local.Method = "GET";
            requsetFeature_local.Protocol = url?.Protocol;
            requsetFeature_local.PathBase = url?.PathBase;
            requsetFeature_local.Scheme = url?.Scheme;
            featureCollection.Set<IHttpRequestFeature>(requsetFeature_local);
            featureCollection.Set<IHttpResponseFeature>(new HttpResponseFeature());
            var httpContext = new DefaultHttpContext(featureCollection);
            httpContext.RequestServices = serviceProvider;
            var actionContext = new ActionContext(httpContext, routeData, actionDescriptor);
            actionContext.RouteData = routeData;
            
            return actionContext;
        }
#else
        /// <summary>
        /// 
        /// </summary>
        /// <param name="areaName">The name of the area containing the Emails view folder if applicable</param>
        /// <param name="request">Optional HttpRequestBase</param>
        /// <returns></returns>
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
                var result = viewEngines[i].FindView(context, viewName, true);
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
        async Task<string> RenderView(IView view, ViewDataDictionary viewData, ActionContext actionContext, ImageEmbedder imageEmbedder)
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
                await view.RenderAsync(viewContext);
                viewData.Remove(ImageEmbedder.ViewDataKey);
                await writer.FlushAsync();
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

#if !ASPNET5
        // StubController so we can create a ControllerContext.
        class StubController : Controller { }
#endif
    }
}
