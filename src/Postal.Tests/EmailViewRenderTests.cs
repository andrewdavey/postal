using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Postal.AspNetCore;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Postal
{
    public class EmailViewRenderTests
    {
        [Fact]
        public async Task Render_returns_email_string_created_by_view()
        {
            var viewEngine = new Mock<IRazorViewEngine>();
            var view = new FakeView();
            viewEngine.Setup(e => e.FindView(It.IsAny<ActionContext>(), "Test", It.IsAny<bool>()))
                       .Returns(ViewEngineResult.Found("Test", view)).Verifiable();

            var serviceProvider = new Mock<IServiceProvider>();
            var tempDataProvider = new Mock<ITempDataProvider>();
            var hostingEnvironment = new Mock<IWebHostEnvironment>();
            ITemplateService templateService = new TemplateService(viewEngine.Object, serviceProvider.Object, tempDataProvider.Object, hostingEnvironment.Object);
            var renderer = new EmailViewRender(templateService);

            var actualEmailString = await renderer.RenderAsync(new Email("Test"));

            actualEmailString.ShouldBe("Fake");

            viewEngine.Verify();
        }

        [Fact]
        public async Task Render_returns_email_string_created_by_view_retrievepath()
        {
            var viewEngine = new Mock<IRazorViewEngine>();
            var view = new FakeView();
            viewEngine.Setup(e => e.FindView(It.IsAny<ActionContext>(), "~/Views/TestFolder/Test", It.IsAny<bool>()))
                       .Returns(ViewEngineResult.NotFound("Test", new string[0]));
            viewEngine.Setup(e => e.GetView(It.IsAny<string>(), "~/Views/TestFolder/Test", It.IsAny<bool>()))
                       .Returns(ViewEngineResult.Found("~/Views/TestFolder/Test", view)).Verifiable();

            var serviceProvider = new Mock<IServiceProvider>();
            var tempDataProvider = new Mock<ITempDataProvider>();
            var hostingEnvironment = new Mock<IWebHostEnvironment>();
            ITemplateService templateService = new TemplateService(viewEngine.Object, serviceProvider.Object, tempDataProvider.Object, hostingEnvironment.Object);
            var renderer = new EmailViewRender(templateService);

            var actualEmailString = await renderer.RenderAsync(new Email("~/Views/TestFolder/Test"));

            actualEmailString.ShouldBe("Fake");

            viewEngine.Verify();
        }

        class FakeView : IView
        {
            public FakeView()
            {
                TemplateString = _ => "Fake";
            }

            public FakeView(Func<ViewContext, string> templateString)
            {
                TemplateString = templateString;
            }

            public string Path => throw new NotImplementedException();

            public Func<ViewContext, string> TemplateString { get; private set; }

            public Task RenderAsync(ViewContext context)
            {
                return context.Writer.WriteAsync(TemplateString(context));
            }
        }

        [Fact]
        public async Task Render_throws_exception_when_email_view_not_found()
        {
            var viewEngine = new Mock<IRazorViewEngine>();
            viewEngine.Setup(e => e.FindView(It.IsAny<ActionContext>(), "Test", It.IsAny<bool>()))
                       .Returns(ViewEngineResult.NotFound("Test", new[] { "Test" })).Verifiable();
            var serviceProvider = new Mock<IServiceProvider>();
            var tempDataProvider = new Mock<ITempDataProvider>();
            var hostingEnvironment = new Mock<IWebHostEnvironment>();
            ITemplateService templateService = new TemplateService(viewEngine.Object, serviceProvider.Object, tempDataProvider.Object, hostingEnvironment.Object);
            var renderer = new EmailViewRender(templateService);

            await Assert.ThrowsAsync<TemplateServiceException>(() => renderer.RenderAsync(new Email("Test")));

            viewEngine.Verify();
        }

        [Fact]
        public async Task Render_throws_exception_when_email_view_retrievepath_not_found()
        {
            var viewEngine = new Mock<IRazorViewEngine>();
            viewEngine.Setup(e => e.GetView(It.IsAny<string>(), "~/Views/TestFolder/Test", It.IsAny<bool>()))
                       .Returns(ViewEngineResult.NotFound("~/Views/TestFolder/Test", new[] { "Test" })).Verifiable();

            var serviceProvider = new Mock<IServiceProvider>();
            var tempDataProvider = new Mock<ITempDataProvider>();
            var hostingEnvironment = new Mock<IWebHostEnvironment>();
            ITemplateService templateService = new TemplateService(viewEngine.Object, serviceProvider.Object, tempDataProvider.Object, hostingEnvironment.Object);
            var renderer = new EmailViewRender(templateService);

            await Assert.ThrowsAsync<TemplateServiceException>(() => renderer.RenderAsync(new Email("~/Views/TestFolder/Test")));

            viewEngine.Verify();
        }

        [Fact]
        public async Task Render_returns_email_string_with_img_created_by_view()
        {
            var email = new Email("Test");
            var cid = email.ImageEmbedder.ReferenceImage("https://www.google.com/images/branding/googlelogo/1x/googlelogo_color_272x92dp.png");

            var mvcViewOptions = new Mock<Microsoft.Extensions.Options.IOptions<MvcViewOptions>>();

            //var tmp = controller.Resolver.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
            //ICompositeViewEngine engine = new CompositeViewEngine(mvcViewOptions.Object);

            var viewEngine = new Mock<IRazorViewEngine>();
            var view = new FakeView(_ => _.ViewData[ImageEmbedder.ViewDataKey] != null ? "True" : "False");
            viewEngine.Setup(e => e.FindView(It.IsAny<ActionContext>(), "Test", It.IsAny<bool>()))
                       .Returns(ViewEngineResult.Found("Test", view)).Verifiable();

            var serviceProvider = new Mock<IServiceProvider>();
            var tempDataProvider = new Mock<ITempDataProvider>();
            var hostingEnvironment = new Mock<IWebHostEnvironment>();
            ITemplateService templateService = new TemplateService(viewEngine.Object, serviceProvider.Object, tempDataProvider.Object, hostingEnvironment.Object);
            var renderer = new EmailViewRender(templateService);

            var actualEmailString = await renderer.RenderAsync(email);

            actualEmailString.ShouldBe("True");

            viewEngine.Verify();
        }

        [Fact]
        public async Task Render_returns_email_string_with_img_created_by_view_retrievepath()
        {
            var email = new Email("~/Views/TestFolder/Test");
            var cid = email.ImageEmbedder.ReferenceImage("https://www.google.com/images/branding/googlelogo/1x/googlelogo_color_272x92dp.png");

            var mvcViewOptions = new Mock<Microsoft.Extensions.Options.IOptions<MvcViewOptions>>();

            //var tmp = controller.Resolver.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
            //ICompositeViewEngine engine = new CompositeViewEngine(mvcViewOptions.Object);

            var viewEngine = new Mock<IRazorViewEngine>();
            var view = new FakeView(_ => _.ViewData[ImageEmbedder.ViewDataKey] != null ? "True" : "False");
            viewEngine.Setup(e => e.GetView(It.IsAny<string>(), "~/Views/TestFolder/Test", It.IsAny<bool>()))
                       .Returns(ViewEngineResult.Found("~/Views/TestFolder/Test", view)).Verifiable();

            var serviceProvider = new Mock<IServiceProvider>();
            var tempDataProvider = new Mock<ITempDataProvider>();
            var hostingEnvironment = new Mock<IWebHostEnvironment>();
            ITemplateService templateService = new TemplateService(viewEngine.Object, serviceProvider.Object, tempDataProvider.Object, hostingEnvironment.Object);
            var renderer = new EmailViewRender(templateService);

            var actualEmailString = await renderer.RenderAsync(email);

            actualEmailString.ShouldBe("True");

            viewEngine.Verify();
        }

        [Fact]
        public async Task Render_returns_email_string_created_by_view_generic_host()
        {
            var viewEngine = new Mock<IRazorViewEngine>();
            var view = new FakeView();
            viewEngine.Setup(e => e.FindView(It.IsAny<ActionContext>(), "Test", It.IsAny<bool>()))
                       .Returns(ViewEngineResult.Found("Test", view)).Verifiable();

            var serviceProvider = new Mock<IServiceProvider>();
            var tempDataProvider = new Mock<ITempDataProvider>();
            var hostingEnvironment = new Mock<Microsoft.Extensions.Hosting.IHostEnvironment>();
            ITemplateService templateService = new TemplateService(viewEngine.Object, serviceProvider.Object, tempDataProvider.Object, hostingEnvironment.Object);
            var renderer = new EmailViewRender(templateService);

            var actualEmailString = await renderer.RenderAsync(new Email("Test"));

            actualEmailString.ShouldBe("Fake");

            viewEngine.Verify();
        }
    }
}
