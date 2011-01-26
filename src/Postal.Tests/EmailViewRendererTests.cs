using System;
using System.IO;
using System.Web.Mvc;
using Moq;
using Should;
using Xunit;

namespace Postal
{
    public class EmailViewRendererTests
    {
        [Fact]
        public void Render_returns_email_string_created_by_view()
        {
            var viewEngines = new Mock<ViewEngineCollection>();
            var view = new FakeView();
            viewEngines.Setup(e => e.FindView(It.IsAny<ControllerContext>(), "Test", null))
                       .Returns(new ViewEngineResult(view, Mock.Of<IViewEngine>()));
            var renderer = new EmailViewRenderer(viewEngines.Object, "test.com");

            var actualEmailString = renderer.Render(new Email("Test"));

            actualEmailString.ShouldEqual("Fake");
        }

        class FakeView : IView
        {
            public void Render(ViewContext viewContext, TextWriter writer)
            {
                writer.Write("Fake");
            }
        }

        [Fact]
        public void Render_throws_exception_when_email_view_not_found()
        {
            var viewEngines = new Mock<ViewEngineCollection>();
            viewEngines.Setup(e => e.FindView(It.IsAny<ControllerContext>(), "Test", It.IsAny<string>()))
                       .Returns(new ViewEngineResult(new[] { "Test" }));
            var renderer = new EmailViewRenderer(viewEngines.Object, "test.com");

            Assert.Throws<Exception>(delegate
            {
                renderer.Render(new Email("Test"));
            });
        }

    }
}
