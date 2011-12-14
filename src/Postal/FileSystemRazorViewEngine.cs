﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace Postal
{
    /// <summary>
    /// A view engine that uses the Razor engine to render a templates loaded directly from the
    /// file system. This means it will work outside of ASP.NET.
    /// </summary>
    public class FileSystemRazorViewEngine : IViewEngine
    {
        readonly string viewPathRoot;

        public FileSystemRazorViewEngine(string viewPathRoot)
        {
            this.viewPathRoot = viewPathRoot;
        }

        string GetViewFullPath(string path)
        {
            return Path.Combine(viewPathRoot, path);
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

            var existingPath = possibleFullPaths.FirstOrDefault(File.Exists);

            if (existingPath != null)
            {
                return new ViewEngineResult(new FileSystemRazorView(existingPath), this);
            }
            else
            {
                return new ViewEngineResult(possibleFullPaths);
            }
        }

        public ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
        {
            return FindPartialView(controllerContext, viewName, useCache);
        }

        public void ReleaseView(ControllerContext controllerContext, IView view)
        {
            // Nothing to do here - FileSystemRazorView does not need disposing.
        }
    }
}
