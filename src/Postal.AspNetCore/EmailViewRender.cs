using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Postal.AspNetCore;

namespace Postal
{
    /// <summary>
    /// Renders <see cref="Email"/> view's into raw strings using the MVC ViewEngine infrastructure.
    /// </summary>
    public class EmailViewRender : IEmailViewRender
    {
        /// <summary>
        /// Creates a new <see cref="EmailViewRender"/> that uses the given view engines.
        /// </summary>
        /// <param name="viewEngines">The view engines to use when rendering email views.</param>
        public EmailViewRender(ITemplateService templateService)
        {
            _templateService = templateService;
            EmailViewDirectoryName = "Emails";
        }

        readonly ITemplateService _templateService;

        /// <summary>
        /// The name of the directory in "Views" that contains the email views.
        /// By default, this is "Emails".
        /// </summary>
        public string EmailViewDirectoryName { get; set; }

        /// <summary>
        /// Renders an email view.
        /// </summary>
        /// <param name="email">The email to render.</param>
        /// <returns>The rendered email view output.</returns>
        public virtual Task<string> RenderAsync(Email email)
        {
            return RenderAsync(email, null);
        }

        /// <summary>
        /// Renders an email view.
        /// </summary>
        /// <param name="email">The email to render.</param>
        /// <param name="viewName">Optional email view name override. If null then the email's ViewName property is used instead.</param>
        /// <returns>The rendered email view output.</returns>
        public virtual async Task<string> RenderAsync(Email email, string viewName = null)
        {
            viewName = viewName ?? email.ViewName;
            //var controllerContext = CreateControllerContext(email.AreaName, url);
            //var view = CreateView(viewName, controllerContext);

            var routeData = new Microsoft.AspNetCore.Routing.RouteData();
            if (email.Route != null)
            {
                routeData.Routers.Add(email.Route);
            }
            routeData.Values["controller"] = EmailViewDirectoryName;
            if (!string.IsNullOrWhiteSpace(email.AreaName))
            {
                routeData.Values["area"] = email.AreaName;
                routeData.DataTokens["area"] = email.AreaName;
            }

            Dictionary<string, object> viewData = new Dictionary<string, object>();
            viewData[ImageEmbedder.ViewDataKey] = email.ImageEmbedder;            
            var viewOutput = await _templateService.RenderTemplateAsync(routeData, viewName, email, viewData, true);
            viewData.Remove(ImageEmbedder.ViewDataKey);
            return viewOutput;
        }
    }
}
