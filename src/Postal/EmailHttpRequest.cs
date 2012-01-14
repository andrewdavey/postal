using System;
using System.Collections.Specialized;
using System.Web;

namespace Postal
{
    // Implement just enough HttpRequest junk to allow the view engine and views to work.
    // This allows the email rendering to occur on a non-web request thread, 
    // e.g. a background task.

    class EmailHttpRequest : HttpRequestBase
    {
        readonly Uri url;
        readonly NameValueCollection serverVariables = new NameValueCollection();
        readonly Lazy<HttpBrowserCapabilitiesBase> browser = new Lazy<HttpBrowserCapabilitiesBase>(CreateHttpBrowserCapabilities);

        public EmailHttpRequest(Uri url)
        {
            this.url = url;
        }

        public override string ApplicationPath
        {
            get { return HttpRuntime.AppDomainAppVirtualPath; }
        }

        public override NameValueCollection ServerVariables
        {
            get { return serverVariables; }
        }

        public override Uri Url
        {
            get { return url; }
        }

        public override bool IsLocal
        {
            get
            {
                return !url.IsAbsoluteUri;
            }
        }

        public override HttpBrowserCapabilitiesBase Browser
        {
            get
            {
                return browser.Value;
            }
        }

        static HttpBrowserCapabilitiesWrapper CreateHttpBrowserCapabilities()
        {
            return new HttpBrowserCapabilitiesWrapper(
                HttpContext.Current == null
                    ? new HttpBrowserCapabilities()
                    : new HttpBrowserCapabilities
                    {
                        Capabilities = HttpContext.Current.Request.Browser.Capabilities
                    }
            );
        }
    }
}
