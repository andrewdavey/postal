using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace Postal
{
    /// <summary>
    /// Contains helper methods for creating emails with multiple alternative bodies.
    /// </summary>
    public static class EmailBody
    {
        /// <summary>Creates an email with multiple alternative bodies.</summary>
        /// <param name="viewSubNames">e.g. "Html" in an email view called "Example" will look for another view called "Example_Html".</param>
        public static IHtmlString EmailBodyAlternatives(this HtmlHelper html, params string[] viewSubNames)
        {
            var bodies = CreateBodies(html, viewSubNames);
            // We only have to store the bodies for now.
            // The EmailParser will assemble put the bodies into the MailMessage.
            html.ViewContext.HttpContext.Items[EmailBodiesKey] = bodies;

            // I prefer the syntax:
            //   @Html.EmailBodyAlternatives("Text", "Html")
            // over
            //   @{ Html.EmailBodyAlternatives("Text", "Html"); }
            // So this function returns an empty string, instead of being void.
            return MvcHtmlString.Empty;
        }

        internal static readonly string EmailBodiesKey = "__Postal__email_bodies";

        static Dictionary<string, string> CreateBodies(HtmlHelper html, string[] viewSubNames)
        {
            var prefix = html.ViewData["__Postal__view_name"] + ".";
            return viewSubNames.Select(name =>
            {
                var partName = prefix + name;
                var all = html.Partial(partName).ToString();

                using (var reader = new StringReader(all))
                {
                    var contentType = FindContentType(reader);
                    if (string.IsNullOrWhiteSpace(contentType))
                        throw new Exception("Missing 'Content-Type' header in email view '" + partName + "'.");

                    var content = reader.ReadToEnd();
                    return new { contentType, content };
                }
            }).ToDictionary(x => x.contentType, x => x.content);
        }

        static string FindContentType(StringReader reader)
        {
            string contentType = null;
            ParserUtils.ParseHeaders(reader, (key, value) =>
            {
                if (key.Equals("content-type", StringComparison.OrdinalIgnoreCase))
                {
                    contentType = value;
                }
            });
            return contentType;
        }
    }
}
