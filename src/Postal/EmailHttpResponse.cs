using System.Web;

namespace Postal
{
    // Implement just enough HttpResponse junk to allow the view engine and views to work.
    // This allows the email rendering to occur on a non-web request thread, 
    // e.g. a background task.

    class EmailHttpResponse : HttpResponseBase
    {
        readonly HttpCookieCollection cookies = new HttpCookieCollection();

        public override string ApplyAppPathModifier(string virtualPath)
        {
            return virtualPath;
        }

        public override HttpCookieCollection Cookies
        {
            get
            {
                return cookies;
            }
        }
    }
}
