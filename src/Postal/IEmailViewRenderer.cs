using System;

namespace Postal
{
    /// <summary>
    /// Renders an email view.
    /// </summary>
    public interface IEmailViewRenderer
    {
        /// <summary>
        /// Renders an email view based on the provided view name.
        /// </summary>
        /// <param name="email">The email data to pass to the view.</param>
        /// <param name="viewName">Optional, the name of the view. If null, the ViewName of the email will be used.</param>
        /// <returns>The string result of rendering the email.</returns>
#if ASPNET5
        string Render(Email email, Microsoft.AspNetCore.Http.Features.IHttpRequestFeature requsetFeature, string viewName = null);
#else
        string Render(Email email, string viewName = null, System.Web.HttpRequestBase request = null);
#endif
    }
}
