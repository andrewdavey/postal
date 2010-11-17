using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Postal
{
    public interface IEmailSender
    {
        void Send(Email email);
    }

    public class EmailSender : IEmailSender
    {
        /// <param name="urlHostName">The host name of the website. This is for the UrlHelper used when generating Urls in a view.</param>
        public EmailSender(ViewEngineCollection viewEngines, string urlHostName)
        {
            this.viewEngines = viewEngines;
            controllerContext = CreateControllerContext(urlHostName);
        }

        readonly ViewEngineCollection viewEngines;
        readonly ControllerContext controllerContext;

        public void Send(Email email)
        {
            var view = CreateView(email.ViewName);
            if (view == null) throw new Exception("View not found for email: " + email.ViewName);

            using (var message = new MailMessage())
            {
                var output = RenderView(view, email.ViewData);
                InitializeMailMessage(message, output);
                using (var smtp = new SmtpClient())
                {
                    smtp.Send(message);
                }
            }
        }

        IView CreateView(string viewName)
        {
            var result = viewEngines.FindView(controllerContext, viewName, null);
            return result.View;
        }

        ControllerContext CreateControllerContext(string urlHostName)
        {
            var httpContext = new EmailHttpContext(urlHostName);
            var routeData = new RouteData();
            routeData.Values["controller"] = "Emails";
            var requestContext = new RequestContext(httpContext, routeData);
            return new ControllerContext(requestContext, new StubController());
        }

        string RenderView(IView view, ViewDataDictionary viewData)
        {
            var builder = new StringBuilder();
            using (var writer = new StringWriter(builder))
            {
                var viewContext = new ViewContext(controllerContext, view, viewData, new TempDataDictionary(), writer);
                view.Render(viewContext, writer);
            }
            return builder.ToString();
        }

        void InitializeMailMessage(MailMessage message, string viewOutput)
        {
            using (var reader = new StringReader(viewOutput))
            {
                ParseHeaders(message, reader);
                message.Body = reader.ReadToEnd();
                if (message.Body.StartsWith("<")) message.IsBodyHtml = true;
            }
        }

        /// <summary>
        /// Headers are of the form "(key): (value)" e.g. "Subject: Hello, world".
        /// The headers block is terminated by an empty line.
        /// </summary>
        void ParseHeaders(MailMessage message, StringReader reader)
        {
            string line;
            while (!string.IsNullOrWhiteSpace(line = reader.ReadLine()))
            {
                var index = line.IndexOf(':');
                if (index > 0)
                {
                    var key = line.Substring(0, index).ToLowerInvariant().Trim();
                    var value = line.Substring(index + 1).Trim();

                    if (key == "to")           message.To.Add(value);
                    else if (key == "from")    message.From = new MailAddress(value);
                    else if (key == "subject") message.Subject = value;
                    else if (key == "cc")      message.CC.Add(value);
                    else if (key == "bcc")     message.Bcc.Add(value);
                    else                       message.Headers[key] = value;
                }
            }
        }

        
        // Implement just enough HttpContext junk to allow the view engine and views to work.

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
        }

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

        class EmailHttpResponse : HttpResponseBase
        {
            public override string ApplyAppPathModifier(string virtualPath)
            {
                return virtualPath;
            }
        }

        class StubController : Controller { }    
    }
}