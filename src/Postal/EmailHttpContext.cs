using System.Collections;
using System.Web;
using System.Web.Caching;

namespace Postal
{
    // Implement just enough HttpContext junk to allow the view engine and views to work.

    class EmailHttpContext : HttpContextBase
    {
        public EmailHttpContext(string urlHostName)
        {
            items = new Hashtable();
            request = new EmailHttpRequest(urlHostName);
            response = new EmailHttpResponse();
            cache = new Cache();
        }

        Hashtable items;
        HttpRequestBase request;
        HttpResponseBase response;
        Cache cache;

        public override IDictionary Items { get { return items; } }
        public override HttpRequestBase Request { get { return request; } }
        public override HttpResponseBase Response { get { return response; } }
        public override Cache Cache { get { return cache; } }
    }
}
