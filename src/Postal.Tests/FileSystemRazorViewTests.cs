using Xunit;
using System.IO;
using Moq;
using System.Web.Mvc;

namespace Postal
{
    public class FileSystemRazorViewTests
    {
        [Fact]
        public void GivenViewFoundOnce_WhenFileChanged_ThenViewIsReloaded()
        {
            var filename = Path.Combine(Path.GetTempPath(), "test.cshtml");
            File.WriteAllText(filename, "test-1");
            try
            {
                var view1 = new FileSystemRazorView(filename);
                var context = new Mock<ViewContext>();
                context.Setup(c => c.ViewData).Returns(new ViewDataDictionary(new object()));
                using (var writer = new StringWriter())
                {
                    view1.Render(context.Object, writer);
                    var first = writer.GetStringBuilder().ToString();
                    Assert.Equal("test-1", first);
                }

                // Overwrite the file with new content.
                File.WriteAllText(filename, "test-2");
                // Views are single-use, so create a new one.
                var view2 = new FileSystemRazorView(filename);
                using (var writer = new StringWriter())
                {
                    view2.Render(context.Object, writer);
                    var second = writer.GetStringBuilder().ToString();
                    Assert.Equal("test-2", second);
                }
            }
            finally
            {
                File.Delete(filename);
            }
        }

        [Fact]
        public void Layout_template_is_supported()
        {
            var tempPath = Path.GetTempPath();
            var layoutFilename = Path.Combine(tempPath, "layout.cshtml");
            var bodyFilename = Path.Combine(tempPath, "body.cshtml");
            File.WriteAllText(layoutFilename, "layout-test\r\n@RenderBody()");
            File.WriteAllText(bodyFilename, "@{Layout=\"layout.cshtml\";}\r\nbody-test");
            try
            {
                var engine = new FileSystemRazorViewEngine(tempPath);
                var view = engine.FindView(null, "body", null, true).View;
                var context = new Mock<ViewContext>();
                context.Setup(c => c.ViewData).Returns(new ViewDataDictionary(new object()));
                using (var writer = new StringWriter())
                {
                    view.Render(context.Object, writer);
                    var content = writer.GetStringBuilder().ToString();
                    Assert.Equal("layout-test\r\n\r\nbody-test", content);
                }
            }
            finally
            {
                File.Delete(layoutFilename);
                File.Delete(bodyFilename);
            }
        }
    }
}
