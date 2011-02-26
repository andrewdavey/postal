using System.Web;
using System.Web.Mvc;
using System;
using System.IO;

namespace Postal
{
    public static class HtmlExtensions
    {
        public static IHtmlString EmbedImage(this HtmlHelper html, string pathOrUrl, string alt = "")
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl)) throw new ArgumentException("Path or URL required", "pathOrUrl");

            if (IsFileName(pathOrUrl))
            {
                pathOrUrl = html.ViewContext.HttpContext.Server.MapPath(pathOrUrl);
            }
            var imageEmbedder = (ImageEmbedder)html.ViewData["Postal.ImageEmbedder"];
            var resource = imageEmbedder.AddImage(pathOrUrl);
            return new HtmlString(string.Format("<img src=\"cid:{0}\" alt=\"{1}\"/>", resource.ContentId, html.AttributeEncode(alt)));
        }

        static bool IsFileName(string pathOrUrl)
        {
            return !(pathOrUrl.StartsWith("http:", StringComparison.OrdinalIgnoreCase)
                     || pathOrUrl.StartsWith("https:", StringComparison.OrdinalIgnoreCase));
        }
    }
}
