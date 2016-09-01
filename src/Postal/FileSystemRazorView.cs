using System.Linq;
using System.IO;
using RazorEngine;
using RazorEngine.Templating;
#if ASPNET5
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.Threading.Tasks;
#else
using System.Web.Mvc;
#endif

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

#if ASPNET5
        public string Path
        {
            get { return cacheName; }
        }

        /// <summary>
        /// Renders the view into the given <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="viewContext">The <see cref="ViewContext"/> that contains the view data model.</param>
        /// <param name="writer">The <see cref="TextWriter"/> used to write the rendered output.</param>
        public async Task RenderAsync(ViewContext viewContext)
        {
            var writer = viewContext.Writer;
            DynamicViewBag viewBag = new DynamicViewBag(viewContext.ViewData);
            string content = "";
            if (razorEngine != null)
            {
                content = razorEngine.RunCompile(template, cacheName, viewContext.ViewData.ModelMetadata.ModelType, viewContext.ViewData.Model, viewBag);
            }
            else
            {
                content = Engine.Razor.RunCompile(template, cacheName, viewContext.ViewData.ModelMetadata.ModelType, viewContext.ViewData.Model, viewBag);
            }

            await writer.WriteAsync(content);
            await writer.FlushAsync();
        }
#else
        /// <summary>
        /// Renders the view into the given <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="viewContext">The <see cref="ViewContext"/> that contains the view data model.</param>
        /// <param name="writer">The <see cref="TextWriter"/> used to write the rendered output.</param>
        public void Render(ViewContext viewContext, TextWriter writer)
        {
            DynamicViewBag viewBag = new DynamicViewBag(viewContext.ViewData);
            string content = "";
            if (razorEngine != null)
            {
                content = razorEngine.RunCompile(template, cacheName, viewContext.ViewData.ModelMetadata.ModelType, viewContext.ViewData.Model, viewBag);
            }
            else
            {
                content = Engine.Razor.RunCompile(template, cacheName, viewContext.ViewData.ModelMetadata.ModelType, viewContext.ViewData.Model, viewBag);
            }

            writer.Write(content);
            writer.Flush();
        }
#endif
    }
}
