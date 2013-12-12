using System.Web;
using System.Web.Mvc;
using System;

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
        public static IHtmlString EmbedImage(this HtmlHelper html, string imagePathOrUrl, string alt = "")
        {
            if (string.IsNullOrWhiteSpace(imagePathOrUrl)) throw new ArgumentException("Path or URL required", "imagePathOrUrl");

            if (IsFileName(imagePathOrUrl))
            {
                imagePathOrUrl = html.ViewContext.HttpContext.Server.MapPath(imagePathOrUrl);
            }
            var imageEmbedder = (ImageEmbedder)html.ViewData["Postal.ImageEmbedder"];
            var resource = imageEmbedder.ReferenceImage(imagePathOrUrl);
            return new HtmlString(string.Format("<img src=\"cid:{0}\" alt=\"{1}\"/>", resource.ContentId, html.AttributeEncode(alt)));
        }

        static bool IsFileName(string pathOrUrl)
        {
            return !(pathOrUrl.StartsWith("http:", StringComparison.OrdinalIgnoreCase)
                     || pathOrUrl.StartsWith("https:", StringComparison.OrdinalIgnoreCase));
        }
    }
}
