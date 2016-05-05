using System.Linq;
using System.IO;
using System.Web.Mvc;
using RazorEngine;
using RazorEngine.Templating;

namespace Postal
{
    /// <summary>
    /// A view that uses the Razor engine to render a templates loaded directly from the
    /// file system. This means it will work outside of ASP.NET.
    /// </summary>
    public class FileSystemRazorView : IView
    {
        private readonly string template;
        private readonly string cacheName;
        private readonly IRazorEngineService razorEngine;

        /// <summary>
        /// Creates a new <see cref="FileSystemRazorView"/> using the given view filename.
        /// </summary>
        /// <param name="filename">The filename of the view.</param>
        /// <param name="razorEngine">The RazorEngine instance.</param>
        public FileSystemRazorView(string filename, IRazorEngineService razorEngine = null)
        {
            template = File.ReadAllText(filename);
            cacheName = filename;
            this.razorEngine = razorEngine;
        }

        /// <summary>
        /// Renders the view into the given <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="viewContext">The <see cref="ViewContext"/> that contains the view data model.</param>
        /// <param name="writer">The <see cref="TextWriter"/> used to write the rendered output.</param>
        public void Render(ViewContext viewContext, TextWriter writer)
        {
            string content = "";
            if (razorEngine != null)
            {
                content = razorEngine.RunCompile(template, cacheName, viewContext.ViewData.ModelMetadata.ModelType, viewContext.ViewData.Model);
            }
            else
            {
                content = Engine.Razor.RunCompile(template, cacheName, viewContext.ViewData.ModelMetadata.ModelType, viewContext.ViewData.Model);
            }

            writer.Write(content);
            writer.Flush();
        }
    }
}
