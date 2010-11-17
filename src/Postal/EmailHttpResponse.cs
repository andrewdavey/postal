using System.Web;

namespace Postal
{
    // Implement just enough HttpResponse junk to allow the view engine and views to work.

    class EmailHttpResponse : HttpResponseBase
    {
        public override string ApplyAppPathModifier(string virtualPath)
        {
            return virtualPath;
        }
    }
}
