using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Postal.AspNetCore
{
    public class TemplateService : ITemplateService
    {
        private IRazorViewEngine _viewEngine;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITempDataProvider _tempDataProvider;

        public TemplateService(IRazorViewEngine viewEngine, IServiceProvider serviceProvider, ITempDataProvider tempDataProvider)
        {
            _viewEngine = viewEngine;
            _serviceProvider = serviceProvider;
            _tempDataProvider = tempDataProvider;
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
                httpContext.Request.Host = viewModel.RequestPath.Host;
                httpContext.Request.Scheme = viewModel.RequestPath.Scheme;
                httpContext.Request.PathBase = viewModel.RequestPath.PathBase;
            }

            var actionDescriptor = new ActionDescriptor
            {
                RouteValues = routeData.Values.ToDictionary(kv => kv.Key, kv => kv.Value.ToString())
            };
            var actionContext = new ActionContext(httpContext, routeData, actionDescriptor);

            using (var outputWriter = new StringWriter())
            {
                var viewResult = _viewEngine.FindView(actionContext, viewName, isMainPage);
                var viewDictionary = new ViewDataDictionary<TViewModel>(viewModel.ViewData, viewModel);
                if (additonalViewDictionary != null)
                {
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

                if (!viewResult.Success)
                {
                    throw new TemplateServiceException($"Failed to render template {viewName} because it was not found.");
                }

                try
                {
                    var viewContext = new ViewContext(actionContext, viewResult.View, viewDictionary,
                        tempDataDictionary, outputWriter, new HtmlHelperOptions());

                    await viewResult.View.RenderAsync(viewContext);
                }
                catch (Exception ex)
                {
                    throw new TemplateServiceException("Failed to render template due to a razor engine failure", ex);
                }

                await outputWriter.FlushAsync();
                return outputWriter.ToString();
            }
        }
    }
}