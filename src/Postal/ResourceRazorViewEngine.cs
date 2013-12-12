using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;

namespace Postal
{
    /// <summary>
    /// A view engine that uses the Razor engine to render a templates loaded from assembly resource.
    /// This means it will work outside of ASP.NET.
    /// </summary>
    public class ResourceRazorViewEngine : IViewEngine
    {
        private readonly Assembly viewSourceAssembly;
        private readonly string viewPathRoot;

        /// <summary>
        /// Creates a new <see cref="ResourceRazorViewEngine"/> that finds views in the given assembly.
        /// </summary>
        /// <param name="viewSourceAssembly">The assembly containing view resources.</param>
        /// <param name="viewPathRoot">A common resource path prefix.</param>
        public ResourceRazorViewEngine(Assembly viewSourceAssembly, string viewPathRoot)
        {
            this.viewSourceAssembly = viewSourceAssembly;
            this.viewPathRoot = viewPathRoot;
        }

        /// <summary>
        /// Tries to find a razor view (.cshtml or .vbhtml files).
        /// </summary>
        public ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache)
        {
            var possibleFilenames = new List<string>();

            if (!partialViewName.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase)
                && !partialViewName.EndsWith(".vbhtml", StringComparison.OrdinalIgnoreCase))
            {
                possibleFilenames.Add(partialViewName + ".cshtml");
                possibleFilenames.Add(partialViewName + ".vbhtml");
            }
            else
            {
                possibleFilenames.Add(partialViewName);
            }

            var possibleFullPaths = possibleFilenames.Select(GetViewFullPath).ToArray();

            var existingPath = possibleFullPaths.FirstOrDefault(ResourceExists);

            if (existingPath != null)
            {
                return new ViewEngineResult(new ResourceRazorView(viewSourceAssembly, existingPath), this);
            }
            
            return new ViewEngineResult(possibleFullPaths);
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
            // Nothing to do here - ResourceRazorView does not need disposing.
        }

        string GetViewFullPath(string path)
        {
            return String.Format("{0}.{1}", viewPathRoot, path);
        }

        bool ResourceExists(string name)
        {
            return viewSourceAssembly.GetManifestResourceNames().Contains(name);
        }
    }
}