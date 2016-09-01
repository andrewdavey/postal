using RazorEngine.Templating;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if ASPNET5
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
#else
using System.Web.Mvc;
#endif

namespace Postal
{
    /// <summary>
    /// A view engine that uses the Razor engine to render a templates loaded directly from the
    /// file system. This means it will work outside of ASP.NET.
    /// </summary>
    public class FileSystemRazorViewEngine : IViewEngine
    {
        private readonly string viewPathRoot;
        private readonly IRazorEngineService razorEngine;

        /// <summary>
        /// Creates a new <see cref="FileSystemRazorViewEngine"/> that finds views within the given path.
        /// </summary>
        /// <param name="viewPathRoot">The root directory that contains views.</param>
        /// <param name="razorEngine">The RazorEngine instance.</param>
        public FileSystemRazorViewEngine(string viewPathRoot, IRazorEngineService razorEngine = null)
        {
            this.viewPathRoot = viewPathRoot;
            this.razorEngine = razorEngine;
        }

        string GetViewFullPath(string path)
        {
            return Path.Combine(viewPathRoot, path);
        }

#if !ASPNET5
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

            var existingPath = possibleFullPaths.FirstOrDefault(File.Exists);

            if (existingPath != null)
            {
                return new ViewEngineResult(new FileSystemRazorView(existingPath, razorEngine), this);
            }
            else
            {
                return new ViewEngineResult(possibleFullPaths);
            }
        }
#endif

#if !ASPNET5
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
#endif

#if ASPNET5
        public ViewEngineResult FindView(ActionContext context, string viewName, bool isMainPage)
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

            if (existingPath != null)
            {
                return ViewEngineResult.Found(viewName, new FileSystemRazorView(existingPath, razorEngine));
            }
            else
            {
                return ViewEngineResult.NotFound(viewName, possibleFullPaths);
            }
        }

        public ViewEngineResult GetView(string executingFilePath, string viewPath, bool isMainPage)
        {
            var applicationRelativePath = GetAbsolutePath(executingFilePath, viewPath);

            if (File.Exists(applicationRelativePath))
            {
                return ViewEngineResult.Found(viewPath, new FileSystemRazorView(applicationRelativePath, razorEngine));
            }
            else
            {
                return ViewEngineResult.NotFound(viewPath, new string[] { executingFilePath });
            }
        }

        //https://github.com/aspnet/Mvc/blob/dev/src/Microsoft.AspNetCore.Mvc.Razor/RazorViewEngine.cs
        private string GetAbsolutePath(string executingFilePath, string pagePath)
        {
            if (string.IsNullOrEmpty(pagePath))
            {
                // Path is not valid; no change required.
                return pagePath;
            }

            if (IsApplicationRelativePath(pagePath))
            {
                // An absolute path already; no change required.
                return pagePath;
            }

            if (!IsRelativePath(pagePath))
            {
                // A page name; no change required.
                return pagePath;
            }

            // Given a relative path i.e. not yet application-relative (starting with "~/" or "/"), interpret
            // path relative to currently-executing view, if any.
            if (string.IsNullOrEmpty(executingFilePath))
            {
                // Not yet executing a view. Start in app root.
                return "/" + pagePath;
            }

            // Get directory name (including final slash) but do not use Path.GetDirectoryName() to preserve path
            // normalization.
            var index = executingFilePath.LastIndexOf('/');
            System.Diagnostics.Debug.Assert(index >= 0);
            return executingFilePath.Substring(0, index + 1) + pagePath;
        }

        private static bool IsApplicationRelativePath(string name)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(name));
            return name[0] == '~' || name[0] == '/';
        }

        private static bool IsRelativePath(string name)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(name));

            // Though ./ViewName looks like a relative path, framework searches for that view using view locations.
            return name.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase);
        }
#endif
    }
}
