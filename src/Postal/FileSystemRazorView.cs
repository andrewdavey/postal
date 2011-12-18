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
            cacheName = filename + File.GetLastWriteTimeUtc(filename).Ticks.ToString();
        }

        public void Render(ViewContext viewContext, TextWriter writer)
        {
            Razor.Compile(template, viewContext.ViewData.Model.GetType(), cacheName);
            var content = Razor.Run(cacheName, viewContext.ViewData.Model);

            writer.Write(content);
            writer.Flush();
        }
    }
}
