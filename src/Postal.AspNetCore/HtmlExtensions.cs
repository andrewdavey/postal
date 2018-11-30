using System;
#if ASPNET5
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace Postal
{
    /// <summary>
    /// Helper methods that extend <see cref="HtmlHelper"/>.
    /// </summary>
    public static class HtmlExtensions
    {
        /// <summary>
        /// Embeds the given image into the email and returns an HTML &lt;img&gt; tag referencing the image.
        /// </summary>
        /// <param name="html">The <see cref="HtmlHelper"/>.</param>
        /// <param name="imagePathOrUrl">An image file path or URL. A file path can be relative to the web application root directory.</param>
        /// <param name="alt">The content for the &lt;img alt&gt; attribute.</param>
        /// <returns>An HTML &lt;img&gt; tag.</returns>
        public static IHtmlContent EmbedImage(this IHtmlHelper html, string imagePathOrUrl, string alt = "", string style = "")
        {
            if (string.IsNullOrWhiteSpace(imagePathOrUrl)) throw new ArgumentException("Path or URL required", "imagePathOrUrl");

            if (IsFileName(imagePathOrUrl))
            {
                var hosting = html.ViewContext.HttpContext.RequestServices.GetService<IHostingEnvironment>();
                imagePathOrUrl = hosting.WebRootPath + System.IO.Path.DirectorySeparatorChar + imagePathOrUrl.Replace('/', System.IO.Path.DirectorySeparatorChar).Replace('\\', System.IO.Path.DirectorySeparatorChar);
            }
            var imageEmbedder = (ImageEmbedder)html.ViewData[ImageEmbedder.ViewDataKey];
            var resource = imageEmbedder.ReferenceImage(imagePathOrUrl);
            return new HtmlString(string.Format("<img src=\"cid:{0}\" alt=\"{1}\" style=\"{2}\"/>", resource.ContentId, html.Encode(alt), html.Encode(style)));
        }

        static bool IsFileName(string pathOrUrl)
        {
            return !(pathOrUrl.StartsWith("http:", StringComparison.OrdinalIgnoreCase)
                     || pathOrUrl.StartsWith("https:", StringComparison.OrdinalIgnoreCase));
        }
    }
}
