using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Postal.AspNetCore;
using Shouldly;
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
                       .Returns(ViewEngineResult.Found("Test", view));

            var serviceProvider = new Mock<IServiceProvider>();
            var tempDataProvider = new Mock<ITempDataProvider>();
            ITemplateService templateService = new TemplateService(viewEngine.Object, serviceProvider.Object, tempDataProvider.Object);
            var renderer = new EmailViewRender(templateService);

            var actualEmailString = await renderer.RenderAsync(new Email("Test"));

            actualEmailString.ShouldBe("Fake");

            viewEngine.Verify();
        }

        class FakeView : IView
        {
            public string Path => throw new NotImplementedException();

            public Task RenderAsync(ViewContext context)
            {
                return context.Writer.WriteAsync("Fake");
            }
        }

        [Fact]
        public async Task Render_throws_exception_when_email_view_not_found()
        {
            var viewEngine = new Mock<IRazorViewEngine>();
            viewEngine.Setup(e => e.FindView(It.IsAny<ActionContext>(), "Test", It.IsAny<bool>()))
                       .Returns(ViewEngineResult.NotFound("Test", new[] { "Test" }));
            var serviceProvider = new Mock<IServiceProvider>();
            var tempDataProvider = new Mock<ITempDataProvider>();
            ITemplateService templateService = new TemplateService(viewEngine.Object, serviceProvider.Object, tempDataProvider.Object);
            var renderer = new EmailViewRender(templateService);

            await Assert.ThrowsAsync<TemplateServiceException>(() => renderer.RenderAsync(new Email("Test")));

            viewEngine.Verify();
        }

    }
}
