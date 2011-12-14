using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;

namespace Postal
{
    public class ResourceRazorViewEngine : IViewEngine
    {
        private readonly Assembly viewSourceAssembly;
        private readonly string viewPathRoot;

        public ResourceRazorViewEngine(Assembly viewSourceAssembly, string viewPathRoot)
        {
            this.viewSourceAssembly = viewSourceAssembly;
            this.viewPathRoot = viewPathRoot;
        }

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

            var possibleFullPaths = possibleFilenames.Select(GetViewFullPath);

            var existingPath = possibleFullPaths.FirstOrDefault(ResourceExists);

            if (existingPath != null)
            {
                return new ViewEngineResult(new ResourceRazorView(viewSourceAssembly, existingPath), this);
            }
            
            return new ViewEngineResult(possibleFullPaths);
        }

        public ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
        {
            return FindPartialView(controllerContext, viewName, useCache);
        }

        public void ReleaseView(ControllerContext controllerContext, IView view)
        {
            // Nothing to do here - ResourceRazorView does not need disposing.
        }

        private string GetViewFullPath(string path)
        {
            return String.Format("{0}.{1}", viewPathRoot, path);
        }

        private bool ResourceExists(string name)
        {
            return viewSourceAssembly.GetManifestResourceNames().Contains(name);
        }
    }
}