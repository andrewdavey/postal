using System.Collections;
using System.Web;
using System.Web.Caching;

namespace Postal
{
    // Implement just enough HttpContext junk to allow the view engine and views to work.
    // This allows the email rendering to occur on a non-web request thread, 
    // e.g. a background task.

    class EmailHttpContext : HttpContextBase
    {
        public EmailHttpContext(string urlHostName)
        {
            items = new Hashtable();
            request = new EmailHttpRequest(urlHostName);
            response = new EmailHttpResponse();
        }

        Hashtable items;
        HttpRequestBase request;
        HttpResponseBase response;

        public override IDictionary Items { get { return items; } }
        public override HttpRequestBase Request { get { return request; } }
        public override HttpResponseBase Response { get { return response; } }
        public override Cache Cache { get { return HttpRuntime.Cache; } }
    }
}
