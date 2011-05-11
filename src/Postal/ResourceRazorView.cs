using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using RazorEngine;

namespace Postal
{
    public class ResourceRazorView : IView
    {
        private readonly string resourcePath;
        private readonly static MethodInfo genericParseMethod;
        private readonly string template;

        static ResourceRazorView()
        {
            // HACK: We need to strongly-type the call to Razor.Parse,
            // so cache the generic MethodInfo.
            genericParseMethod = typeof(Razor)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(m => m.Name == "Parse" && m.IsGenericMethod);
        }

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
            // HACK: There should be a way to do this without reflection.
            // RazorEngine needs a Parse method that takes an Object and uses GetType
            // instead of requiring a generic parameter... ah well...
            var parseMethod = genericParseMethod
                .MakeGenericMethod(viewContext.ViewData.Model.GetType());

            var content = (string)parseMethod.Invoke(
                null,
                new[] { template, viewContext.ViewData.Model, resourcePath }
            );

            writer.Write(content);
            writer.Flush();
        }
    }
}