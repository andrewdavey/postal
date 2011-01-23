using System;

namespace Postal
{
    public interface IEmailViewRenderer
    {
        string Render(Email email, string viewName = null);
    }
}
