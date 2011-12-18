using System.IO;
using System.Reflection;
using System.Web.Mvc;
using RazorEngine;

namespace Postal
{
    public class ResourceRazorView : IView
    {
        private readonly string resourcePath;
        private readonly string template;

        public ResourceRazorView(Assembly sourceAssembly, string resourcePath)
        {
            this.resourcePath = resourcePath;
            // We've already ensured that the resource exists in ResourceRazorViewEngine
            // ReSharper disable AssignNullToNotNullAttribute
            using (var stream = sourceAssembly.GetManifestResourceStream(resourcePath))
            using (var reader = new StreamReader(stream))
                template = reader.ReadToEnd();
            // ReSharper restore AssignNullToNotNullAttribute
        }

        public void Render(ViewContext viewContext, TextWriter writer)
        {
            Razor.Compile(template, viewContext.ViewData.Model.GetType(), resourcePath);
            var content = Razor.Run(resourcePath, viewContext.ViewData.Model);

            writer.Write(content);
            writer.Flush();
        }
    }
}