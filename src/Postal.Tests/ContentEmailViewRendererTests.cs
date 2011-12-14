using System;
using System.IO;
using Should;
using Xunit;

namespace Postal
{
    public class ContentEmailViewRendererTests
    {
        [Fact]
        public void Render_returns_email_string_created_by_file_system_view()
        {
            var renderer = new ContentEmailViewRenderer(new FakeLocator());

            dynamic email = new Email("Test");
            email.To = "test@user.com";
            email.From = "other@user.com";

            var actualEmailString = renderer.Render(email);

            actualEmailString.ShouldEqual("To: test@user.com, From: other@user.com");
        }

        class FakeLocator : ITemplateLocator
        {
            public string GetTemplateText(string viewName)
            {
                return "To: @Model.To, From: @Model.From";
            }
        }
    }
}
