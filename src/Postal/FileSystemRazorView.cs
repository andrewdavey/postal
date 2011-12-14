using System.IO;
using System.Linq;
using System.Reflection;
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
        readonly static MethodInfo genericParseMethod;

        public FileSystemRazorView(string filename)
        {
            template = File.ReadAllText(filename);
            cacheName = filename + File.GetLastWriteTimeUtc(filename).Ticks.ToString();
        }

        static FileSystemRazorView()
        {
            // HACK: We need to strongly-type the call to Razor.Parse,
            // so cache the generic MethodInfo.
            genericParseMethod = typeof(Razor)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "Parse" && m.IsGenericMethod && 
                    // HACK: There are better ways to query method signatures, but there are only 2 
                    // Prase methods today with sparing forward durability...
                    (m.GetParameters().Count() == 3 &&
                        m.GetParameters()[0].ParameterType == typeof(string) &&
                        // Should check for the generic type here.
                        m.GetParameters()[2].ParameterType == typeof(string)));
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
                new object[] { template, viewContext.ViewData.Model, cacheName }
            );

            writer.Write(content);
            writer.Flush();
        }
    }
}
