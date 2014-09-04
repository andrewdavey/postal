using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Postal
{
    /// <summary>
    /// An Email object has the name of the MVC view to render and a view data dictionary
    /// to store the data to render. It is best used as a dynamic object, just like the 
    /// ViewBag property of a Controller. Any dynamic property access is mapped to the
    /// view data dictionary.
    /// </summary>
    public class Email : DynamicObject, IViewDataContainer
    {
        /// <summary>
        /// Creates a new Email, that will render the given view.
        /// </summary>
        /// <param name="viewName">The name of the view to render</param>
        public Email(string viewName)
        {
            if (viewName == null) throw new ArgumentNullException("viewName");
            if (string.IsNullOrWhiteSpace(viewName)) throw new ArgumentException("View name cannot be empty.", "viewName");

            Attachments = new List<Attachment>();
            ViewName = viewName;
            ViewData = new ViewDataDictionary(this);
            ImageEmbedder = new ImageEmbedder();
        }

        /// <summary>Create an Email where the ViewName is derived from the name of the class.</summary>
        /// <remarks>Used when defining strongly typed Email classes.</remarks>
        protected Email()
        {
            Attachments = new List<Attachment>();
            ViewName = DeriveViewNameFromClassName();
            ViewData = new ViewDataDictionary(this);
            ImageEmbedder = new ImageEmbedder();
        }

        /// <summary>
        /// The name of the view containing the email template.
        /// </summary>
        public string ViewName { get; set; }

        /// <summary>
        /// The view data to pass to the view.
        /// </summary>
        public ViewDataDictionary ViewData { get; set; }

        /// <summary>
        /// The attachments to send with the email.
        /// </summary>
        public List<Attachment> Attachments { get; set; }

        internal ImageEmbedder ImageEmbedder { get; private set; }

        /// <summary>
        /// Adds an attachment to the email.
        /// </summary>
        /// <param name="attachment">The attachment to add.</param>
        public void Attach(Attachment attachment)
        {
            Attachments.Add(attachment);
        }

        /// <summary>
        /// Convenience method that sends this email via a default EmailService. 
        /// </summary>
        public void Send()
        {
            CreateEmailService().Send(this);
        }

        /// <summary>
        /// Convenience method that sends this email asynchronously via a default EmailService. 
        /// </summary>
        public Task SendAsync()
        {
            return CreateEmailService().SendAsync(this);
        }

        /// <summary>
        /// A function that returns an instance of <see cref="IEmailService"/>.
        /// </summary>
        public static Func<IEmailService> CreateEmailService = () => new EmailService();

        // Any dynamic property access is delegated to view data dictionary.
        // This makes for sweet looking syntax - thank you C#4!

        /// <summary>
        /// Stores the given value into the <see cref="ViewData"/>.
        /// </summary>
        /// <param name="binder">Provides the name of the view data property.</param>
        /// <param name="value">The value to store.</param>
        /// <returns>Always returns true.</returns>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            ViewData[binder.Name] = value;
            return true;
        }

        /// <summary>
        /// Tries to get a stored value from <see cref="ViewData"/>.
        /// </summary>
        /// <param name="binder">Provides the name of the view data property.</param>
        /// <param name="result">If found, this is the view data property value.</param>
        /// <returns>True if the property was found, otherwise false.</returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return ViewData.TryGetValue(binder.Name, out result);
        }

        string DeriveViewNameFromClassName()
        {
            var viewName = GetType().Name;
            if (viewName.EndsWith("Email")) viewName = viewName.Substring(0, viewName.Length - "Email".Length);
            return viewName;
        }
    }
}