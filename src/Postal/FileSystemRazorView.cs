using System.IO;
using System.Web.Mvc;
using RazorEngine;

namespace Postal
{
    /// <summary>
    /// A view that uses the Razor engine to render a templates loaded directly from the
    /// file system. This means it will work outside of ASP.NET.
    /// </summary>
    public class FileSystemRazorView : IView
    {
        readonly string template;
        readonly string cacheName;
        
        public FileSystemRazorView(string filename)
        {
            template = File.ReadAllText(filename);
            cacheName = filename;
        }

        public void Render(ViewContext viewContext, TextWriter writer)
        {
            var content = Razor.Parse(template, viewContext.ViewData.Model, cacheName);

            writer.Write(content);
            writer.Flush();
        }
    }
}
