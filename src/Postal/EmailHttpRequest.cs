using System;
using System.Collections.Specialized;
using System.Web;

namespace Postal
{
    // Implement just enough HttpRequest junk to allow the view engine and views to work.

    class EmailHttpRequest : HttpRequestBase
    {
        readonly string urlHostName;
        readonly NameValueCollection serverVariables = new NameValueCollection();

        public EmailHttpRequest(string urlHostName)
        {
            this.urlHostName = urlHostName;
        }

        public override string ApplicationPath
        {
            get
            {
                return HttpRuntime.AppDomainAppVirtualPath;
            }
        }

        public override NameValueCollection ServerVariables
        {
            get
            {
                return serverVariables;
            }
        }

        public override Uri Url
        {
            get
            {
                return new Uri("http://" + urlHostName);
            }
        }
    }
}
