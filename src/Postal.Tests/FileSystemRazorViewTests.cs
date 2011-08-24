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
    }
}
