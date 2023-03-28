using System;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Postal.AspNetCore;
using Xunit;

namespace Postal
{
    public class EmailViewResultTests
    {
        //https://github.com/aspnet/Mvc/blob/a67d9363e22be8ef63a1a62539991e1da3a6e30e/test/Microsoft.AspNetCore.Mvc.ViewFeatures.Test/ViewResultTest.cs
        private ActionContext GetActionContext(IEmailViewRender render, IEmailParser parser = null)
        {
            return new ActionContext(GetHttpContext(render, parser), new RouteData(), new ActionDescriptor());
        }

        private HttpContext GetHttpContext(IEmailViewRender render, IEmailParser parser = null)
        {
            var options = Options.Create(new MvcViewOptions());

            var viewExecutor = new EmailViewResultExecutor(
                NullLoggerFactory.Instance,
                new TestHttpResponseStreamWriterFactory(),
                render,
                parser);

            var services = new ServiceCollection();
            services.AddSingleton<IActionResultExecutor<EmailViewResult>>(viewExecutor);

            var httpContext = new DefaultHttpContext();
            var memoryStream = new MemoryStream();
            httpContext.Response.Body = memoryStream;
            httpContext.RequestServices = services.BuildServiceProvider();
            return httpContext;
        }

        private IEmailViewRender GetRender(Email email, string template, string textTemplate = null, string htmlTemplate = null)
        {
            var renderer = new Mock<IEmailViewRender>();

            renderer.Setup(r => r.RenderAsync(email)).Returns(Task.FromResult(template));
            if (!string.IsNullOrEmpty(textTemplate))
                renderer.Setup(r => r.RenderAsync(email, "~/Views/Emails/Test.Text.cshtml")).Returns(Task.FromResult(textTemplate));
            if (!string.IsNullOrEmpty(htmlTemplate))
                renderer.Setup(r => r.RenderAsync(email, "~/Views/Emails/Test.Html.cshtml")).Returns(Task.FromResult(htmlTemplate));

            return renderer.Object;
        }

        private EmailViewResult Create()
        {
            var email = new Email("~/Views/Emails/Test.cshtml");
            return new EmailViewResult(email);
        }

        //https://github.com/aspnet/Mvc/blob/a67d9363e22be8ef63a1a62539991e1da3a6e30e/test/Microsoft.AspNetCore.Mvc.ViewFeatures.Test/ViewComponentResultTest.cs#L624
        private static string ReadBody(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);

            using (var reader = new StreamReader(response.Body))
            {
                return reader.ReadToEnd();
            }
        }


        [Fact]
        public async Task ExecuteResult_should_write()
        {
            var result = Create();
            var output = await GetOutput(result, SimpleTextOutput);
            Assert.NotEmpty(output);
        }

        [Fact]
        public async Task ExecuteResult_returns_text_content_type()
        {
            var result = Create();
            var actionContext = GetActionContext(GetRender(result.Email, SimpleTextOutput));
            await result.ExecuteResultAsync(actionContext);

            var contentType = actionContext.HttpContext.Response.ContentType;
            Assert.Equal("text/plain", contentType);
        }

        [Fact]
        public async Task ExecuteResult_should_write_correctly()
        {
            var result = Create();
            var output = await GetOutput(result, SimpleTextOutput);
            Assert.Equal(SimpleTextOutput, output);
        }

        [Fact]
        public async Task ExecuteResult_on_html_writers_header_in_comment()
        {
            var result = Create();
            var output = await GetOutput(result, SimpleHtmlOutput);
            Assert.Contains("<!--" + Environment.NewLine +
                            "To: test@example.org" + Environment.NewLine +
                            "From: test@example.org" + Environment.NewLine +
                            "Subject: Simple email example" + Environment.NewLine +
                            Environment.NewLine +
                            "-->", output);
        }

        [Fact]
        public async Task ExecuteResult_on_html_returns_html_content_type()
        {
            var result = Create();
            var actionContext = GetActionContext(GetRender(result.Email, SimpleHtmlOutput));
            await result.ExecuteResultAsync(actionContext);
            var contentType = actionContext.HttpContext.Response.ContentType;
            Assert.Equal("text/html", contentType);
        }

        [Fact]
        public async Task ExecuteResult_with_text_format_on_html_fails()
        {
            var result = Create();
            await Assert.ThrowsAsync<NotSupportedException>(() => GetOutput(result, SimpleHtmlOutput, format: "text"));
        }

        [Fact]
        public async Task ExecuteResult_with_html_format_on_text_fails()
        {
            var result = Create();
            await Assert.ThrowsAsync<NotSupportedException>(() => GetOutput(result, SimpleTextOutput, format: "html"));
        }

        [Fact]
        public async Task ExecuteResult_on_multipart_without_format_renders_default()
        {
            var result = Create();
            var output = await GetOutput(result, MultiPartOutput, MultiPartTextOutput, MultiPartHtmlOutput);
            Assert.Equal(MultiPartOutput, output);
        }

        [Fact]
        public async Task ExecuteResult_on_multipart_without_format_returns_text_content_type()
        {
            var result = Create();
            var actionContext = GetActionContext(GetRender(result.Email, MultiPartOutput, MultiPartTextOutput, MultiPartHtmlOutput));
            await result.ExecuteResultAsync(actionContext);
            var contentType = actionContext.HttpContext.Response.ContentType;
            Assert.Equal("text/plain", contentType);
        }

        [Fact]
        public async Task ExecuteResult_on_multipart_with_text_format_renders_text()
        {
            var result = Create();
            var output = await GetOutput(result, MultiPartOutput, MultiPartTextOutput, MultiPartHtmlOutput, format: "text");
            Assert.Equal(@"This is a plain text message

Generated by Postal on 2014/06/20", output);
        }

        [Fact]
        public async Task ExecuteResult_on_multipart_with_html_format_renders_html()
        {
            var result = Create();
            var output = await GetOutput(result, MultiPartOutput, MultiPartTextOutput, MultiPartHtmlOutput, format: "html");
            Assert.Equal(@"<html>
    <body>
        <p>This is an <code>HTML</code> message</p>
        <p>Generated by <a href=""http://aboutcode.net/postal"">Postal</a> on @ViewBag.Date</p>        
    </body>
</html>", output);
        }

        [Fact]
        public async Task ExecuteResult_on_multipart_with_html_format_returns_html_content_type()
        {
            var result = Create();
            var actionContext = GetActionContext(GetRender(result.Email, MultiPartOutput, MultiPartTextOutput, MultiPartHtmlOutput));
            actionContext.HttpContext.Request.QueryString = new QueryString("?format=html");
            await result.ExecuteResultAsync(actionContext);
            var contentType = actionContext.HttpContext.Response.ContentType;
            Assert.Equal("text/html", contentType);
        }

        [Fact]
        public async Task ExecuteResult_on_multipart_with_text_format_returns_text_content_type()
        {
            var result = Create();
            var actionContext = GetActionContext(GetRender(result.Email, MultiPartOutput, MultiPartTextOutput, MultiPartHtmlOutput));
            actionContext.HttpContext.Request.QueryString = new QueryString("?format=text");
            await result.ExecuteResultAsync(actionContext);
            var contentType = actionContext.HttpContext.Response.ContentType;
            Assert.Equal("text/plain", contentType);
        }

        [Fact]
        public async Task ReplaceLinkedImagesWithEmbeddedImages_replaces_cid_reference()
        {
            var embedder = new ImageEmbedder();
            var resource = await embedder.ReferenceImageAsync("postal.png");

            string body = "<img src=\"cid:" + resource.ContentId + @"""/>";
            var view = AlternateView.CreateAlternateViewFromString(body);
            embedder.AddImagesToView(view);

            string replaced = EmailViewResultExecutor.ReplaceLinkedImagesWithEmbeddedImages(view, body);
            Assert.DoesNotContain("cid:", replaced);
        }

        [Fact]
        public async Task ReplaceLinkedImagesWithEmbeddedImages_replaces_cid_reference_with_correct_mime()
        {
            var embedder = new ImageEmbedder();
            var resource = await embedder.ReferenceImageAsync("postal.png");

            string body = "<img src=\"cid:" + resource.ContentId + @"""/>";
            var view = AlternateView.CreateAlternateViewFromString(body);
            embedder.AddImagesToView(view);

            string replaced = EmailViewResultExecutor.ReplaceLinkedImagesWithEmbeddedImages(view, body);
            Assert.Contains("data:image/png;base64,", replaced);
        }

        async Task<string> GetOutput(EmailViewResult result, string template, string textTemplate = null, string htmlTemplate = null, string format = null)
        {
            var actionContext = GetActionContext(GetRender(result.Email, template, textTemplate, htmlTemplate));
            if (!string.IsNullOrWhiteSpace(format))
            {
                actionContext.HttpContext.Request.QueryString = new QueryString("?format=" + format);
            }
            await result.ExecuteResultAsync(actionContext);
            var body = ReadBody(actionContext.HttpContext.Response);
            return body;
        }

        const string SimpleTextOutput = @"To: test@example.org
From: test@example.org
Subject: Simple email example

Hello, world!

The date is: 2014/06/20";

        const string SimpleHtmlOutput = @"To: test@example.org
From: test@example.org
Subject: Simple email example

<html>
    <body>
        <p>The date is 2014/06/20</p>
    </body>
</html>";

        const string MultiPartOutput = @"To: test@example.org
From: test@example.org
Subject: Multi-part email example
Views: Html,Text";

        const string MultiPartTextOutput = @"Content-Type: text/plain; charset=utf8

This is a plain text message

Generated by Postal on 2014/06/20";

        const string MultiPartHtmlOutput = @"Content-Type: text/html; charset=utf8

<html>
    <body>
        <p>This is an <code>HTML</code> message</p>
        <p>Generated by <a href=""http://aboutcode.net/postal"">Postal</a> on @ViewBag.Date</p>        
    </body>
</html>";


    }
}
