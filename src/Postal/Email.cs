using System;
using System.Dynamic;
using System.Web.Mvc;

namespace Postal
{
    /// <summary>
    /// An Email object has the name of the MVC view to render and a view data dictionary
    /// to store the data to render. It is best used as a dynamic object, just like the 
    /// ViewModel property of a Controller. Any dynamic property access is mapped to the
    /// view data dictionary.
    /// </summary>
    public class Email : DynamicObject, IViewDataContainer
    {
        public Email(string viewName)
        {
            if (viewName == null) throw new ArgumentNullException("viewName");

            ViewName = viewName;
            ViewData = new ViewDataDictionary();
        }

        /// <summary>
        /// The name of the view containing the email template.
        /// </summary>
        public string ViewName { get; set; }

        /// <summary>
        /// The view data to pass to the view.
        /// </summary>
        public ViewDataDictionary ViewData { get; set; }

        // Any dynamic property access is delegated to view data dictionary.
        // This makes for sweet looking syntax - thank you C#4!

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