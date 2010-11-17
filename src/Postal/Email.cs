using System.Dynamic;
using System.Web.Mvc;

namespace Postal
{
    public class Email : DynamicObject, IViewDataContainer
    {
        public Email(string viewName)
        {
            ViewName = viewName;
            ViewData = new ViewDataDictionary();
        }

        public string ViewName { get; set; }
        public ViewDataDictionary ViewData { get; set; }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            ViewData[binder.Name] = value;
            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return ViewData.TryGetValue(binder.Name, out result);
        }
    }
}