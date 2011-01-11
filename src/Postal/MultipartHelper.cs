using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Text.RegularExpressions;
using System.IO;
using System.Web;

namespace Postal
{
    public static class MultipartHelper
    {
        /// <param name="viewSubNames">e.g. "Html" in an email view called "Example" will look for another view called "Example_Html".</param>
        public static IHtmlString MultipartEmail(this HtmlHelper html, params string[] viewSubNames)
        {
            var prefix = GetWebFormViewName(html.ViewContext.View) + "_";
            var outputs = viewSubNames.Select(name => {
                var partName = prefix + name;
                var all = html.Partial(partName).ToString();
                var match = Regex.Match(all, @"content\-type\:\s*(.*?)(\r|\n)", RegexOptions.IgnoreCase);
                if (!match.Success) throw new Exception("Missing 'Content-Type' header in email view '" + partName + "'.");
                var contentType = match.Groups[1].Value.Trim();
                var content = all.Substring(match.Index + match.Length);
                return new{contentType, content};
            });

            html.ViewContext.HttpContext.Items["__Postal__parts"] = outputs.ToDictionary(x => x.contentType, x => x.content);

            return MvcHtmlString.Empty;
        }

        public static string GetWebFormViewName(this IView view)
        {
            var view2 = view as BuildManagerCompiledView;
            if (view2 != null)
            {
                string viewUrl = view2.ViewPath;
                string viewFileName = viewUrl.Substring(viewUrl.LastIndexOf('/'));
                string viewFileNameWithoutExtension = Path.GetFileNameWithoutExtension(viewFileName);
                return (viewFileNameWithoutExtension);
            }
            else
            {
                throw (new InvalidOperationException("This view is not a WebFormView"));
            }
        }
    }
}
