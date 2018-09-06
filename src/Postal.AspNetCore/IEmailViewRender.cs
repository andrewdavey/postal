using System;
using System.Threading.Tasks;

namespace Postal
{
    /// <summary>
    /// Renders an email view.
    /// </summary>
    public interface IEmailViewRender
    {
        /// <summary>
        /// Renders an email view based on the provided view name.
        /// </summary>
        /// <param name="email">The email data to pass to the view.</param>
        /// <param name="viewName">Optional, the name of the view. If null, the ViewName of the email will be used.</param>
        /// <returns>The string result of rendering the email.</returns>
        Task<string> RenderAsync(Email email);
        Task<string> RenderAsync(Email email, string viewName = null);
    }
}
