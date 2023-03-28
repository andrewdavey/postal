using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Postal.AspNetCore
{
    public class TemplateService : ITemplateService
    {
        public static readonly string ViewExtension = ".cshtml";

        private readonly ILogger<TemplateService> _logger;
        private readonly IRazorViewEngine _viewEngine;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly Microsoft.Extensions.Hosting.IHostEnvironment _hostingEnvironment;
        private readonly IRazorPageActivator _pageActivator;
        private readonly System.Text.Encodings.Web.HtmlEncoder _htmlEncoder;
        private readonly DiagnosticListener _diagnosticListener;

        public TemplateService(
            ILogger<TemplateService> logger,
            IRazorPageActivator pageActivator,
            IRazorViewEngine viewEngine,
            IServiceProvider serviceProvider,
            ITempDataProvider tempDataProvider,
            Microsoft.Extensions.Hosting.IHostEnvironment hostingEnvironment,
            System.Text.Encodings.Web.HtmlEncoder htmlEncoder,
            DiagnosticListener diagnosticListener
            )
        {
            _logger = logger;
            _viewEngine = viewEngine;
            _serviceProvider = serviceProvider;
            _tempDataProvider = tempDataProvider;
            _hostingEnvironment = hostingEnvironment;
            _pageActivator = pageActivator;
            _htmlEncoder = htmlEncoder;
            _diagnosticListener = diagnosticListener;
        }

        public async Task<string> RenderTemplateAsync<TViewModel>(RouteData routeData,
            string viewName, TViewModel viewModel, Dictionary<string, object> additonalViewDictionary = null, bool isMainPage = true) where TViewModel : IViewData
        {
            var httpContext = new DefaultHttpContext
            {
                RequestServices = _serviceProvider
            };
            if (viewModel.RequestPath != null)
            {
                httpContext.Request.Host = HostString.FromUriComponent(viewModel.RequestPath.Host);
                httpContext.Request.Scheme = viewModel.RequestPath.Scheme;
                httpContext.Request.PathBase = PathString.FromUriComponent(viewModel.RequestPath.PathBase);

                _logger.LogDebug($"RequestPath != null");
                _logger.LogTrace($"\tHost: {viewModel.RequestPath.Host} -> {httpContext.Request.Host}");
                _logger.LogTrace($"\tScheme: {viewModel.RequestPath.Scheme}");
                _logger.LogTrace($"\tPathBase: {viewModel.RequestPath.PathBase} -> {httpContext.Request.PathBase}");
            }

            var actionDescriptor = new ActionDescriptor
            {
                RouteValues = routeData.Values.ToDictionary(kv => kv.Key, kv => kv.Value.ToString())
            };

            var actionContext = new ActionContext(httpContext, routeData, actionDescriptor);

            using (var outputWriter = new StringWriter())
            {
                Microsoft.AspNetCore.Mvc.ViewEngines.ViewEngineResult viewResult = null;
                RazorPageResult? razorPageResult = null;
                if (IsApplicationRelativePath(viewName) || IsRelativePath(viewName))
                {
                    _logger.LogDebug($"Relative path");
                    if (_hostingEnvironment is Microsoft.AspNetCore.Hosting.IWebHostEnvironment webHostEnvironment)
                    {
                        _logger.LogDebug($"_hostingEnvironment is IWebHostEnvironment -> GetView from WebRootPath: {webHostEnvironment.WebRootPath}");
                        viewResult = _viewEngine.GetView(webHostEnvironment.WebRootPath, viewName, isMainPage);
                    }
                    else
                    {
                        _logger.LogDebug($"_hostingEnvironment is IHostEnvironment -> GetView from ContentRootPath: {_hostingEnvironment.ContentRootPath}");
                        viewResult = _viewEngine.GetView(_hostingEnvironment.ContentRootPath, viewName, isMainPage);
                    }
                }
                else
                {
                    _logger.LogDebug($"Not a relative path");
                    viewResult = _viewEngine.FindView(actionContext, viewName, isMainPage);
                    razorPageResult = _viewEngine.FindPage(actionContext, viewName);
                }

                var viewDictionary = new ViewDataDictionary<TViewModel>(viewModel.ViewData, viewModel);
                if (additonalViewDictionary != null)
                {
                    _logger.LogDebug($"additonalViewDictionary count: {additonalViewDictionary.Count}");
                    foreach (var kv in additonalViewDictionary)
                    {
                        if (!viewDictionary.ContainsKey(kv.Key))
                        {
                            viewDictionary.Add(kv);
                        }
                        else
                        {
                            viewDictionary[kv.Key] = kv.Value;
                        }
                    }
                }

                var tempDataDictionary = new TempDataDictionary(httpContext, _tempDataProvider);

                if (!viewResult.Success && (razorPageResult == null || (razorPageResult != null && razorPageResult?.Page == null)))
                {
                    var searchedLocations = viewResult.SearchedLocations;
                    if (razorPageResult?.SearchedLocations != null)
                    {
                        searchedLocations = viewResult.SearchedLocations.Union(razorPageResult?.SearchedLocations);
                    }
                    _logger.LogError($"Failed to render template {viewName} because it was not found. \r\nThe following locations are searched: \r\n{string.Join("\r\n", searchedLocations)}");
                    throw new TemplateServiceException($"Failed to render template {viewName} because it was not found. \r\nThe following locations are searched: \r\n{string.Join("\r\n", searchedLocations)}");
                }

                try
                {
                    if (viewResult.Success)
                    {
                        var viewContext = new ViewContext(actionContext, viewResult.View, viewDictionary,
                            tempDataDictionary, outputWriter, new HtmlHelperOptions());

                        await viewResult.View.RenderAsync(viewContext);
                    }
                    else if (razorPageResult?.Page != null)
                    {
                        var page = razorPageResult?.Page;
                        var razorView = new RazorView(
                            _viewEngine,
                            _pageActivator,
                            new List<IRazorPage>(),
                            page,
                            _htmlEncoder,
                            _diagnosticListener
                        );

                        var viewContext = new ViewContext(actionContext, razorView, viewDictionary,
                            tempDataDictionary, outputWriter, new HtmlHelperOptions());

                        await viewResult.View.RenderAsync(viewContext);
                        var pageNormal = ((Page)page);
                        pageNormal.PageContext = new PageContext();
                        pageNormal.ViewContext = viewContext;
                        _pageActivator.Activate(pageNormal, viewContext);
                        await page.ExecuteAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to render template due to a razor engine failure");
                    throw new TemplateServiceException("Failed to render template due to a razor engine failure", ex);
                }

                await outputWriter.FlushAsync();
                return outputWriter.ToString();
            }
        }

        // https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Razor/src/RazorViewEngine.cs#L478
        private static bool IsApplicationRelativePath(string name)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));
            return name[0] == '~' || name[0] == '/';
        }

        private static bool IsRelativePath(string name)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));

            // Though ./ViewName looks like a relative path, framework searches for that view using view locations.
            return name.EndsWith(ViewExtension, StringComparison.OrdinalIgnoreCase);
        }
    }
}