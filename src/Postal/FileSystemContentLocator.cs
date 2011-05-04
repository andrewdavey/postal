using System.IO;

namespace Postal
{
    public class FileSystemTemplateLocator : ITemplateLocator
    {
        public FileSystemTemplateLocator(string templateDirectory)
        {
            TemplateDirectory = templateDirectory;

            if (!TemplateDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                TemplateDirectory += Path.DirectorySeparatorChar.ToString();
            }
        }

        public string GetTemplateText(string viewName)
        {
            var filePath = TemplateDirectory + viewName;

            // Assume cshtml if no extension exists
            if (!Path.HasExtension(filePath))
            {
                filePath += ".cshtml";
            }

            return File.ReadAllText(filePath);
        }

        public string TemplateDirectory { get; set; }
    }
}
