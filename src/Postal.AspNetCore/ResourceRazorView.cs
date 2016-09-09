using System.IO;
using System.Reflection;
using RazorEngine;
using RazorEngine.Templating;
using System.Security.Cryptography;
using System.Text;
#if ASPNET5
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
#else
using System.Web.Mvc;
#endif

namespace Postal
{
    /// <summary>
    /// An <see cref="IView"/> that reads its content from an assembly resource.
    /// </summary>
    public class ResourceRazorView : IView
    {
        private readonly string resourcePath;
        private readonly string template;
        private readonly IRazorEngineService razorEngine;

        /// <summary>
        /// Creates a new <see cref="ResourceRazorView"/> for a given assembly and resource.
        /// </summary>
        /// <param name="sourceAssembly">The assembly containing the resource.</param>
        /// <param name="resourcePath">The resource path.</param>
        /// <param name="razorEngine">The RazorEngine instance.</param>
        public ResourceRazorView(Assembly sourceAssembly, string resourcePath, IRazorEngineService razorEngine = null)
        {
            this.resourcePath = resourcePath;
            // We've already ensured that the resource exists in ResourceRazorViewEngine
            // ReSharper disable AssignNullToNotNullAttribute
            using (var stream = sourceAssembly.GetManifestResourceStream(resourcePath))
            using (var reader = new StreamReader(stream))
                template = reader.ReadToEnd();
            // ReSharper restore AssignNullToNotNullAttribute
            this.razorEngine = razorEngine;
        }

#if ASPNET5
        public string Path
        {
            get { return this.resourcePath; }
        }

        /// <summary>
        /// Renders the view into the given <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="viewContext">Contains the view data model.</param>
        /// <param name="writer">The <see cref="TextWriter"/> used to write the rendered output.</param>
        public async Task RenderAsync(ViewContext viewContext)
        {
            var writer = viewContext.Writer;
            DynamicViewBag viewBag = new DynamicViewBag(viewContext.ViewData);
            string content = "";
            if (razorEngine != null)
            {
                content = razorEngine.RunCompile(template, resourcePath + GetMd5Hash(template), viewContext.ViewData.ModelMetadata.ModelType, viewContext.ViewData.Model, viewBag);
            }
            else
            {
                content = Engine.Razor.RunCompile(template, resourcePath + GetMd5Hash(template), viewContext.ViewData.ModelMetadata.ModelType, viewContext.ViewData.Model, viewBag);
            }
            await writer.WriteAsync(content);
            await writer.FlushAsync();
        }
#else
        /// <summary>
        /// Renders the view into the given <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="viewContext">Contains the view data model.</param>
        /// <param name="writer">The <see cref="TextWriter"/> used to write the rendered output.</param>
        public void Render(ViewContext viewContext, TextWriter writer)
        {
            DynamicViewBag viewBag = new DynamicViewBag(viewContext.ViewData);
            string content = "";
            if (razorEngine != null)
            {
                content = razorEngine.RunCompile(template, resourcePath + GetMd5Hash(template), viewContext.ViewData.ModelMetadata.ModelType, viewContext.ViewData.Model, viewBag);
            }
            else
            {
                content = Engine.Razor.RunCompile(template, resourcePath + GetMd5Hash(template), viewContext.ViewData.ModelMetadata.ModelType, viewContext.ViewData.Model, viewBag);
            }
            writer.Write(content);
            writer.Flush();
        }
#endif

        private static string GetMd5Hash(string input)
        {
            var md5 = MD5.Create();
            var inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            var hash = md5.ComputeHash(inputBytes);
            var sb = new StringBuilder();
            foreach (byte t in hash)
            {
                sb.Append(t.ToString("X2"));
            }
            return sb.ToString();
        }
    }
}