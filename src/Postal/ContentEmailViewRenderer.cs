using RazorEngine;

namespace Postal
{
    public class ContentEmailViewRenderer : IEmailViewRenderer
    {
        public ContentEmailViewRenderer(string templateDirectory)
        {
            TemplateLocator = new FileSystemTemplateLocator(templateDirectory);
        }

        public ContentEmailViewRenderer(ITemplateLocator templateLocator)
        {
            TemplateLocator = templateLocator;
        }

        public string Render(Email email, string viewName = null)
        {
            viewName = viewName ?? email.ViewName;
            var template = TemplateLocator.GetTemplateText(viewName);
            
            return Razor.Parse(template, email);
        }

        public ITemplateLocator TemplateLocator { get; set; }
        public string TemplatePath { get; set; }
    }
}
