using System.IO;
using System.Web.Mvc;
using RazorEngine.Templating;

namespace Postal
{
    /// <summary>
    /// A view that uses the Razor engine to render a templates loaded directly from the
    /// file system. This means it will work outside of ASP.NET.
    /// </summary>
    public class FileSystemRazorView : IView
    {
        static readonly ITemplateService DefaultRazorService = new TemplateService();

        readonly ITemplateService razorService;
        readonly string template;
        readonly string cacheName;

        /// <summary>
        /// Creates a new <see cref="FileSystemRazorView"/> using the given view filename.
        /// </summary>
        /// <param name="filename">The filename of the view.</param>
        public FileSystemRazorView(string filename) : this(DefaultRazorService, filename)
        {
        }

        /// <summary>
        /// Creates a new <see cref="FileSystemRazorView"/> using the given view filename.
        /// </summary>
        /// <param name="razorService">The RazorEngine ITemplateService to use to render the view</param>
        /// <param name="filename">The filename of the view.</param>
        public FileSystemRazorView(ITemplateService razorService, string filename)
        {
            this.razorService = razorService;
            template = File.ReadAllText(filename);
            cacheName = filename;
        }

        /// <summary>
        /// Renders the view into the given <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="viewContext">The <see cref="ViewContext"/> that contains the view data model.</param>
        /// <param name="writer">The <see cref="TextWriter"/> used to write the rendered output.</param>
        public void Render(ViewContext viewContext, TextWriter writer)
        {
            var content = razorService.Parse(template, viewContext.ViewData.Model, null, cacheName);

            writer.Write(content);
            writer.Flush();
        }
    }
}
