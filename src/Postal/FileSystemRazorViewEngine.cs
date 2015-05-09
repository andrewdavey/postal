using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using RazorEngine.Configuration;
using RazorEngine.Templating;

namespace Postal
{
    /// <summary>
    /// A view engine that uses the Razor engine to render a templates loaded directly from the
    /// file system. This means it will work outside of ASP.NET.
    /// </summary>
    public class FileSystemRazorViewEngine : IViewEngine
    {
        readonly string viewPathRoot;
        readonly ITemplateService razorService;

        /// <summary>
        /// Creates a new <see cref="FileSystemRazorViewEngine"/> that finds views within the given path.
        /// </summary>
        /// <param name="viewPathRoot">The root directory that contains views.</param>
        public FileSystemRazorViewEngine(string viewPathRoot)
        {
            this.viewPathRoot = viewPathRoot;

            var razorConfig = new TemplateServiceConfiguration();
            razorConfig.Resolver = new DelegateTemplateResolver(ResolveTemplate);
            razorService = new TemplateService(razorConfig);
        }

        string GetViewFullPath(string path)
        {
            return Path.Combine(viewPathRoot, path);
        }

        private string ResolveTemplate(string viewName)
        {
            var path = ResolveTemplatePath(viewName);
            if (path == null) return null;
            return File.ReadAllText(path);
        }

        private string ResolveTemplatePath(string viewName)
        {
            IEnumerable<string> searchedPaths;
            var existingPath = ResolveTemplatePath(viewName, out searchedPaths);
            return existingPath;
        }

        private string ResolveTemplatePath(string viewName, out IEnumerable<string> searchedPaths )
        {
            var possibleFilenames = new List<string>();

            if (!viewName.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase)
                && !viewName.EndsWith(".vbhtml", StringComparison.OrdinalIgnoreCase))
            {
                possibleFilenames.Add(viewName + ".cshtml");
                possibleFilenames.Add(viewName + ".vbhtml");
            }
            else
            {
                possibleFilenames.Add(viewName);
            }

            var possibleFullPaths = possibleFilenames.Select(GetViewFullPath).ToArray();

            var existingPath = possibleFullPaths.FirstOrDefault(File.Exists);
            searchedPaths = possibleFullPaths;
            return existingPath;
        }

        /// <summary>
        /// Tries to find a razor view (.cshtml or .vbhtml files).
        /// </summary>
        public ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache)
        {
            IEnumerable<string> searchedPaths;
            var existingPath = ResolveTemplatePath(partialViewName, out searchedPaths);

            if (existingPath != null)
                return new ViewEngineResult(new FileSystemRazorView(razorService, existingPath), this);

            return new ViewEngineResult(searchedPaths);
        }

        /// <summary>
        /// Tries to find a razor view (.cshtml or .vbhtml files).
        /// </summary>
        public ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
        {
            return FindPartialView(controllerContext, viewName, useCache);
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        public void ReleaseView(ControllerContext controllerContext, IView view)
        {
            // Nothing to do here - FileSystemRazorView does not need disposing.
        }
    }
}
