using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;
using System.Web.Hosting;
using System.Web.SessionState;


namespace Postal
{
    public enum HttpVerb
    {
        GET,
        HEAD,
        POST,
        PUT,
        DELETE,
    }

    /// <summary>
    /// Useful class for simulating the HttpContext. This does not actually 
    /// make an HttpRequest, it merely simulates the state that your code 
    /// would be in "as if" handling a request. Thus the HttpContext.Current 
    /// property is populated.
    /// </summary>
    public class HttpSimulator : IDisposable
    {
        private const string defaultPhysicalAppPath = @"c:\InetPub\wwwRoot\";
        private StringBuilder builder;
        private Uri _referer;
        private readonly NameValueCollection _formVars = new NameValueCollection();
        private readonly NameValueCollection _headers = new NameValueCollection();

        public HttpSimulator() : this("/", defaultPhysicalAppPath)
        {
        }

        public HttpSimulator(string applicationPath) : this(applicationPath, defaultPhysicalAppPath)
        {

        }

        public HttpSimulator(string applicationPath, string physicalApplicationPath)
        {
            ApplicationPath = applicationPath;
            PhysicalApplicationPath = physicalApplicationPath;
        }

        /// <summary>
        /// Sets up the HttpContext objects to simulate a GET request.
        /// </summary>
        /// <remarks>
        /// Simulates a request to http://localhost/
        /// </remarks>
        public HttpSimulator SimulateRequest()
        {
            return SimulateRequest(new Uri("http://localhost/"));
        }

        /// <summary>
        /// Sets up the HttpContext objects to simulate a GET request.
        /// </summary>
        /// <param name="url"></param>
        public HttpSimulator SimulateRequest(Uri url)
        {
            return SimulateRequest(url, HttpVerb.GET);
        }

        /// <summary>
        /// Sets up the HttpContext objects to simulate a request.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="httpVerb"></param>
        public HttpSimulator SimulateRequest(Uri url, HttpVerb httpVerb)
        {
            return SimulateRequest(url, httpVerb, null, null);
        }

        /// <summary>
        /// Sets up the HttpContext objects to simulate a POST request.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="formVariables"></param>
        public HttpSimulator SimulateRequest(Uri url, NameValueCollection formVariables)
        {
            return SimulateRequest(url, HttpVerb.POST, formVariables, null);
        }

        /// <summary>
        /// Sets up the HttpContext objects to simulate a POST request.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="formVariables"></param>
        /// <param name="headers"></param>
        public HttpSimulator SimulateRequest(Uri url, NameValueCollection formVariables, NameValueCollection headers)
        {
            return SimulateRequest(url, HttpVerb.POST, formVariables, headers);
        }

        /// <summary>
        /// Sets up the HttpContext objects to simulate a request.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="httpVerb"></param>
        /// <param name="headers"></param>
        public HttpSimulator SimulateRequest(Uri url, HttpVerb httpVerb, NameValueCollection headers)
        {
            return SimulateRequest(url, httpVerb, null, headers);
        }

        /// <summary>
        /// Sets up the HttpContext objects to simulate a request.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="httpVerb"></param>
        /// <param name="formVariables"></param>
        /// <param name="headers"></param>
        protected virtual HttpSimulator SimulateRequest(Uri url, HttpVerb httpVerb, NameValueCollection formVariables, NameValueCollection headers)
        {
            HttpContext.Current = null;

            ParseRequestUrl(url);

            if (responseWriter == null)
            {
                builder = new StringBuilder();
                responseWriter = new StringWriter(builder);
            }

            SetHttpRuntimeInternals();

            var query = ExtractQueryStringPart(url);

            if (formVariables != null)
            {
                _formVars.Add(formVariables);
            }

            if (_formVars.Count > 0)
            {
                httpVerb = HttpVerb.POST; //Need to enforce 
            }

            if (headers != null)
            {
                _headers.Add(headers);
            }

            workerRequest = new SimulatedHttpRequest(ApplicationPath, PhysicalApplicationPath, PhysicalPath, Page, query, responseWriter, host, port, httpVerb.ToString());

            workerRequest.Form.Add(_formVars);
            workerRequest.Headers.Add(_headers);

            if (_referer != null)
            {
                workerRequest.SetReferer(_referer);
            }

            InitializeSession();

            InitializeApplication();

            #region Console Debug Info

            Console.WriteLine("host: " + host);
            Console.WriteLine("virtualDir: " + applicationPath);
            Console.WriteLine("page: " + localPath);
            Console.WriteLine("pathPartAfterApplicationPart: " + _page);
            Console.WriteLine("appPhysicalDir: " + physicalApplicationPath);
            Console.WriteLine("Request.Url.LocalPath: " + HttpContext.Current.Request.Url.LocalPath);
            Console.WriteLine("Request.Url.Host: " + HttpContext.Current.Request.Url.Host);
            Console.WriteLine("Request.FilePath: " + HttpContext.Current.Request.FilePath);
            Console.WriteLine("Request.Path: " + HttpContext.Current.Request.Path);
            Console.WriteLine("Request.RawUrl: " + HttpContext.Current.Request.RawUrl);
            Console.WriteLine("Request.Url: " + HttpContext.Current.Request.Url);
            Console.WriteLine("Request.Url.Port: " + HttpContext.Current.Request.Url.Port);
            Console.WriteLine("Request.ApplicationPath: " + HttpContext.Current.Request.ApplicationPath);
            Console.WriteLine("Request.PhysicalPath: " + HttpContext.Current.Request.PhysicalPath);
            Console.WriteLine("HttpRuntime.AppDomainAppPath: " + HttpRuntime.AppDomainAppPath);
            Console.WriteLine("HttpRuntime.AppDomainAppVirtualPath: " + HttpRuntime.AppDomainAppVirtualPath);
            Console.WriteLine("HostingEnvironment.ApplicationPhysicalPath: " + HostingEnvironment.ApplicationPhysicalPath);
            Console.WriteLine("HostingEnvironment.ApplicationVirtualPath: " + HostingEnvironment.ApplicationVirtualPath);

            #endregion

            return this;
        }

        private static void InitializeApplication()
        {
            var appFactoryType = Type.GetType("System.Web.HttpApplicationFactory, System.Web, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            var appFactory = ReflectionHelper.GetStaticFieldValue<object>("_theApplicationFactory", appFactoryType);
            ReflectionHelper.SetPrivateInstanceFieldValue("_state", appFactory, HttpContext.Current.Application);
        }

        private void InitializeSession()
        {
            HttpContext.Current = new HttpContext(workerRequest);
            HttpContext.Current.Items.Clear();
            var session = (HttpSessionState)ReflectionHelper.Instantiate(typeof(HttpSessionState), new[] { typeof(IHttpSessionState) }, new FakeHttpSessionState());

            HttpContext.Current.Items.Add("AspSession", session);
        }

        public class FakeHttpSessionState : NameObjectCollectionBase, IHttpSessionState
        {
            private readonly string sessionID = Guid.NewGuid().ToString();
            private int timeout = 30; //minutes
            private const bool isNewSession = true;
            private readonly HttpStaticObjectsCollection staticObjects = new HttpStaticObjectsCollection();
            private readonly object syncRoot = new Object();

            ///<summary>
            /// Ends the current session.
            ///</summary>
            public void Abandon()
            {
                BaseClear();
            }

            ///<summary>
            /// Adds a new item to the session-state collection.
            ///</summary>
            ///
            ///<param name="name">The name of the item to add to the session-state collection. </param>
            ///<param name="value">The value of the item to add to the session-state collection. </param>
            public void Add(string name, object value)
            {
                BaseAdd(name, value);
            }

            ///<summary>
            ///Deletes an item from the session-state item collection.
            ///</summary>
            ///
            ///<param name="name">The name of the item to delete from the session-state item collection. </param>
            public void Remove(string name)
            {
                BaseRemove(name);
            }

            ///<summary>
            ///Deletes an item at a specified index from the session-state item collection.
            ///</summary>
            ///
            ///<param name="index">The index of the item to remove from the session-state collection. </param>
            public void RemoveAt(int index)
            {
                BaseRemoveAt(index);
            }

            ///<summary>
            ///Clears all values from the session-state item collection.
            ///</summary>
            ///
            public void Clear()
            {
                BaseClear();
            }

            ///<summary>
            ///Clears all values from the session-state item collection.
            ///</summary>
            ///
            public void RemoveAll()
            {
                BaseClear();
            }

            ///<summary>
            ///Copies the collection of session-state item values to a one-dimensional array, starting at the specified index in the array.
            ///</summary>
            ///
            ///<param name="array">The <see cref="T:System.Array"></see> that receives the session values. </param>
            ///<param name="index">The index in array where copying starts. </param>
            public void CopyTo(Array array, int index)
            {
                throw new NotImplementedException();
            }

            ///<summary>
            ///Gets the unique session identifier for the session.
            ///</summary>
            ///
            ///<returns>
            ///The session ID.
            ///</returns>
            ///
            public string SessionID
            {
                get { return sessionID; }
            }

            ///<summary>
            ///Gets and sets the time-out period (in minutes) allowed between requests before the session-state provider terminates the session.
            ///</summary>
            ///<returns>
            ///The time-out period, in minutes.
            ///</returns>
            public int Timeout
            {
                get { return timeout; }
                set { timeout = value; }
            }

            ///<summary>
            ///Gets a value indicating whether the session was created with the current request.
            ///</summary>
            ///<returns>
            ///true if the session was created with the current request; otherwise, false.
            ///</returns>
            public bool IsNewSession
            {
                get { return isNewSession; }
            }

            ///<summary>
            ///Gets the current session-state mode.
            ///</summary>
            ///<returns>
            ///One of the <see cref="T:System.Web.SessionState.SessionStateMode"></see> values.
            ///</returns>
            public SessionStateMode Mode
            {
                get { return SessionStateMode.InProc; }
            }

            ///<summary>
            ///Gets a value indicating whether the session ID is embedded in the URL or stored in an HTTP cookie.
            ///</summary>
            ///
            ///<returns>
            ///true if the session is embedded in the URL; otherwise, false.
            ///</returns>
            ///
            public bool IsCookieless
            {
                get { return false; }
            }

            ///<summary>
            ///Gets a value that indicates whether the application is configured for cookieless sessions.
            ///</summary>
            ///
            ///<returns>
            ///One of the <see cref="T:System.Web.HttpCookieMode"></see> values that indicate whether the application is configured for cookieless sessions. The default is <see cref="F:System.Web.HttpCookieMode.UseCookies"></see>.
            ///</returns>
            ///
            public HttpCookieMode CookieMode
            {
                get { return HttpCookieMode.UseCookies; }
            }

            ///<summary>
            ///Gets or sets the locale identifier (LCID) of the current session.
            ///</summary>
            ///
            ///<returns>
            ///A <see cref="T:System.Globalization.CultureInfo"></see> instance that specifies the culture of the current session.
            ///</returns>
            ///
            public int LCID { get; set; }

            ///<summary>
            ///Gets or sets the code-page identifier for the current session.
            ///</summary>
            ///
            ///<returns>
            ///The code-page identifier for the current session.
            ///</returns>
            ///
            public int CodePage { get; set; }

            ///<summary>
            ///Gets a collection of objects declared by &lt;object Runat="Server" Scope="Session"/&gt; tags within the ASP.NET application file Global.asax.
            ///</summary>
            ///
            ///<returns>
            ///An <see cref="T:System.Web.HttpStaticObjectsCollection"></see> containing objects declared in the Global.asax file.
            ///</returns>
            ///
            public HttpStaticObjectsCollection StaticObjects
            {
                get { return staticObjects; }
            }

            ///<summary>
            ///Gets or sets a session-state item value by name.
            ///</summary>
            ///
            ///<returns>
            ///The session-state item value specified in the name parameter.
            ///</returns>
            ///
            ///<param name="name">The key name of the session-state item value. </param>
            public object this[string name]
            {
                get { return BaseGet(name); }
                set { BaseSet(name, value); }
            }

            ///<summary>
            ///Gets or sets a session-state item value by numerical index.
            ///</summary>
            ///
            ///<returns>
            ///The session-state item value specified in the index parameter.
            ///</returns>
            ///
            ///<param name="index">The numerical index of the session-state item value. </param>
            public object this[int index]
            {
                get { return BaseGet(index); }
                set { BaseSet(index, value); }
            }

            ///<summary>
            ///Gets an object that can be used to synchronize access to the collection of session-state values.
            ///</summary>
            ///
            ///<returns>
            ///An object that can be used to synchronize access to the collection.
            ///</returns>
            ///
            public object SyncRoot
            {
                get { return syncRoot; }
            }



            ///<summary>
            ///Gets a value indicating whether access to the collection of session-state values is synchronized (thread safe).
            ///</summary>
            ///<returns>
            ///true if access to the collection is synchronized (thread safe); otherwise, false.
            ///</returns>
            ///
            public bool IsSynchronized
            {
                get { return true; }
            }

            ///<summary>
            ///Gets a value indicating whether the session is read-only.
            ///</summary>
            ///
            ///<returns>
            ///true if the session is read-only; otherwise, false.
            ///</returns>
            ///
            bool IHttpSessionState.IsReadOnly
            {
                get
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Sets the referer for the request. Uses a fluent interface.
        /// </summary>
        /// <param name="referer"></param>
        /// <returns></returns>
        public HttpSimulator SetReferer(Uri referer)
        {
            if (workerRequest != null)
                workerRequest.SetReferer(referer);
            _referer = referer;
            return this;
        }

        /// <summary>
        /// Sets a form variable.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public HttpSimulator SetFormVariable(string name, string value)
        {
            //TODO: Change this ordering requirement.
            if (workerRequest != null)
            {
                throw new InvalidOperationException("Cannot set form variables after calling Simulate().");
            }

            _formVars.Add(name, value);

            return this;
        }

        /// <summary>
        /// Sets a header value.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public HttpSimulator SetHeader(string name, string value)
        {
            //TODO: Change this ordering requirement.
            if (workerRequest != null)
            {
                throw new InvalidOperationException("Cannot set headers after calling Simulate().");
            }

            _headers.Add(name, value);

            return this;
        }

        private void ParseRequestUrl(Uri url)
        {
            if (url == null)
            {
                return;
            }
            host = url.Host;
            port = url.Port;
            localPath = url.LocalPath;
            _page = StripPrecedingSlashes(RightAfter(url.LocalPath, ApplicationPath));
            physicalPath = Path.Combine(physicalApplicationPath, _page.Replace("/", @"\"));
        }

        static string RightAfter(string original, string search)
        {
            if (search.Length > original.Length || search.Length == 0)
            {
                return original;
            }

            var searchIndex = original.IndexOf(search, 0, StringComparison.InvariantCultureIgnoreCase);

            return searchIndex < 0 
                ? original 
                : original.Substring(original.IndexOf(search) + search.Length);
        }

        public string Host
        {
            get { return host; }
        }

        private string host;

        public string LocalPath
        {
            get { return localPath; }
        }

        private string localPath;

        public int Port
        {
            get { return port; }
        }

        private int port;

        /// <summary>
        /// Portion of the URL after the application.
        /// </summary>
        public string Page
        {
            get { return _page; }
        }

        private string _page;

        /// <summary>
        /// The same thing as the IIS Virtual directory. It's 
        /// what gets returned by Request.ApplicationPath.
        /// </summary>
        public string ApplicationPath
        {
            get { return applicationPath; }
            set
            {
                applicationPath = value ?? "/";
                applicationPath = NormalizeSlashes(applicationPath);
            }
        }
        private string applicationPath = "/";

        /// <summary>
        /// Physical path to the application (used for simulation purposes).
        /// </summary>
        public string PhysicalApplicationPath
        {
            get { return physicalApplicationPath; }
            set
            {
                physicalApplicationPath = value ?? defaultPhysicalAppPath;
                //strip trailing backslashes.
                physicalApplicationPath = StripTrailingBackSlashes(physicalApplicationPath) + @"\";
            }
        }

        private string physicalApplicationPath = defaultPhysicalAppPath;

        /// <summary>
        /// Physical path to the requested file (used for simulation purposes).
        /// </summary>
        public string PhysicalPath
        {
            get { return physicalPath; }
        }

        private string physicalPath = defaultPhysicalAppPath;

        public TextWriter ResponseWriter
        {
            get { return responseWriter; }
            set { responseWriter = value; }
        }

        /// <summary>
        /// Returns the text from the response to the simulated request.
        /// </summary>
        public string ResponseText
        {
            get
            {
                return (builder ?? new StringBuilder()).ToString();
            }
        }

        private TextWriter responseWriter;

        public SimulatedHttpRequest WorkerRequest
        {
            get { return workerRequest; }
        }

        private SimulatedHttpRequest workerRequest;

        private static string ExtractQueryStringPart(Uri url)
        {
            var query = url.Query;// ?? string.Empty;
            return query.StartsWith("?") ? query.Substring(1) : query;
        }

        void SetHttpRuntimeInternals()
        {
            //We cheat by using reflection.

            // get singleton property value
            var runtime = ReflectionHelper.GetStaticFieldValue<HttpRuntime>("_theRuntime", typeof(HttpRuntime));

            // set app path property value
            ReflectionHelper.SetPrivateInstanceFieldValue("_appDomainAppPath", runtime, PhysicalApplicationPath);
            // set app virtual path property value
            const string vpathTypeName = "System.Web.VirtualPath, System.Web, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            var virtualPath = ReflectionHelper.Instantiate(vpathTypeName, new[] { typeof(string) }, new object[] { ApplicationPath });
            ReflectionHelper.SetPrivateInstanceFieldValue("_appDomainAppVPath", runtime, virtualPath);

            // set codegen dir property value
            ReflectionHelper.SetPrivateInstanceFieldValue("_codegenDir", runtime, PhysicalApplicationPath);

            var environment = GetHostingEnvironment();
            ReflectionHelper.SetPrivateInstanceFieldValue("_appPhysicalPath", environment, PhysicalApplicationPath);
            ReflectionHelper.SetPrivateInstanceFieldValue("_appVirtualPath", environment, virtualPath);
            ReflectionHelper.SetPrivateInstanceFieldValue("_configMapPath", environment, new ConfigMapPath(this));
        }

        protected static HostingEnvironment GetHostingEnvironment()
        {
            HostingEnvironment environment;
            try
            {
                environment = new HostingEnvironment();
            }
            catch (InvalidOperationException)
            {
                //Shoot, we need to grab it via reflection.
                environment = ReflectionHelper.GetStaticFieldValue<HostingEnvironment>("_theHostingEnvironment", typeof(HostingEnvironment));
            }
            return environment;
        }

        #region --- Text Manipulation Methods for slashes ---
        protected static string NormalizeSlashes(string s)
        {
            if (String.IsNullOrEmpty(s) || s == "/")
                return "/";

            s = s.Replace(@"\", "/");

            //Reduce multiple slashes in row to single.
            var normalized = Regex.Replace(s, "(/)/+", "$1");
            //Strip left.
            normalized = StripPrecedingSlashes(normalized);
            //Strip right.
            normalized = StripTrailingSlashes(normalized);
            return "/" + normalized;
        }

        protected static string StripPrecedingSlashes(string s)
        {
            return Regex.Replace(s, "^/*(.*)", "$1");
        }

        protected static string StripTrailingSlashes(string s)
        {
            return Regex.Replace(s, "(.*)/*$", "$1", RegexOptions.RightToLeft);
        }

        protected static string StripTrailingBackSlashes(string s)
        {
            return String.IsNullOrEmpty(s)
                ? string.Empty 
                : Regex.Replace(s, @"(.*)\\*$", "$1", RegexOptions.RightToLeft);
        }

        #endregion

        internal class ConfigMapPath : IConfigMapPath
        {
            private readonly HttpSimulator _requestSimulation;
            public ConfigMapPath(HttpSimulator simulation)
            {
                _requestSimulation = simulation;
            }

            public string GetMachineConfigFilename()
            {
                throw new NotImplementedException();
            }

            public string GetRootWebConfigFilename()
            {
                throw new NotImplementedException();
            }

            public void GetPathConfigFilename(string siteID, string path, out string directory, out string baseName)
            {
                throw new NotImplementedException();
            }

            public void GetDefaultSiteNameAndID(out string siteName, out string siteID)
            {
                throw new NotImplementedException();
            }

            public void ResolveSiteArgument(string siteArgument, out string siteName, out string siteID)
            {
                throw new NotImplementedException();
            }

            public string MapPath(string siteID, string path)
            {
                var page = StripPrecedingSlashes(RightAfter(path, _requestSimulation.ApplicationPath));
                return Path.Combine(_requestSimulation.PhysicalApplicationPath, page.Replace("/", @"\"));
            }

            public string GetAppPathForPath(string siteID, string path)
            {
                return _requestSimulation.ApplicationPath;
            }
        }

        ///<summary>
        ///Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        ///</summary>
        ///<filterpriority>2</filterpriority>
        public void Dispose()
        {
            if (HttpContext.Current != null)
            {
                HttpContext.Current = null;
            }
        }
    }

    /// <summary>
    /// Helper class to simplify common reflection tasks.
    /// </summary>
    public static class ReflectionHelper
    {
        /// <summary>
        /// Returns the value of the private member specified.
        /// </summary>
        /// <param name="fieldName">Name of the member.</param>
        /// <param name="type">Type of the member.</param>
        public static T GetStaticFieldValue<T>(string fieldName, Type type)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            if (field != null)
            {
                return (T)field.GetValue(type);
            }
            return default(T);
        }

        /// <summary>
        /// Returns the value of the private member specified.
        /// </summary>
        /// <param name="fieldName">Name of the member.</param>
        /// <param name="typeName"></param>
        public static T GetStaticFieldValue<T>(string fieldName, string typeName)
        {
            var type = Type.GetType(typeName, true);
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);

            if (field != null)
            {
                return (T)field.GetValue(type);
            }
            return default(T);
        }

        /// <summary>
        /// Sets the value of the private static member.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        public static void SetStaticFieldValue<T>(string fieldName, Type type, T value)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null)
            {
                throw new ArgumentException(string.Format("Could not find the private instance field '{0}'", fieldName));
            }

            field.SetValue(null, value);
        }

        /// <summary>
        /// Sets the value of the private static member.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="typeName"></param>
        /// <param name="value"></param>
        public static void SetStaticFieldValue<T>(string fieldName, string typeName, T value)
        {
            var type = Type.GetType(typeName, true);
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null)
            {
                throw new ArgumentException(string.Format("Could not find the private instance field '{0}'", fieldName));
            }

            field.SetValue(null, value);
        }

        /// <summary>
        /// Returns the value of the private member specified.
        /// </summary>
        /// <param name="fieldName">Name of the member.</param>
        /// <param name="source">The object that contains the member.</param>
        public static T GetPrivateInstanceFieldValue<T>(string fieldName, object source)
        {
            var field = source.GetType().GetField(fieldName, BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                return (T)field.GetValue(source);
            }
            return default(T);
        }

        /// <summary>
        /// Returns the value of the private member specified.
        /// </summary>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="source">The object that contains the member.</param>
        /// <param name="value">The value to set the member to.</param>
        public static void SetPrivateInstanceFieldValue(string memberName, object source, object value)
        {
            var field = source.GetType().GetField(memberName, BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
                throw new ArgumentException(string.Format("Could not find the private instance field '{0}'", memberName));

            field.SetValue(source, value);
        }

        public static object Instantiate(string typeName)
        {
            return Instantiate(typeName, null, null);
        }

        public static object Instantiate(string typeName, Type[] constructorArgumentTypes, params object[] constructorParameterValues)
        {
            return Instantiate(Type.GetType(typeName, true), constructorArgumentTypes, constructorParameterValues);
        }

        public static object Instantiate(Type type, Type[] constructorArgumentTypes, params object[] constructorParameterValues)
        {
            var constructor = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, constructorArgumentTypes, null);
            return constructor.Invoke(constructorParameterValues);
        }

        /// <summary>
        /// Invokes a non-public static method.
        /// </summary>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="type"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static TReturn InvokeNonPublicMethod<TReturn>(Type type, string methodName, params object[] parameters)
        {
            var paramTypes = Array.ConvertAll(parameters, o => o.GetType());

            var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static, null, paramTypes, null);
            if (method == null)
            {
                throw new ArgumentException(string.Format("Could not find a method with the name '{0}'", methodName), "method");
            }

            return (TReturn)method.Invoke(null, parameters);
        }

        public static TReturn InvokeNonPublicMethod<TReturn>(object source, string methodName, params object[] parameters)
        {
            var paramTypes = Array.ConvertAll(parameters, o => o.GetType());

            var method = source.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance, null, paramTypes, null);
            if (method == null)
            {
                throw new ArgumentException(string.Format("Could not find a method with the name '{0}'", methodName), "method");
            }

            return (TReturn)method.Invoke(source, parameters);
        }

        public static TReturn InvokeProperty<TReturn>(object source, string propertyName)
        {
            var propertyInfo = source.GetType().GetProperty(propertyName);
            if (propertyInfo == null)
            {
                throw new ArgumentException(string.Format("Could not find a propertyName with the name '{0}'", propertyName), "propertyName");
            }

            return (TReturn)propertyInfo.GetValue(source, null);
        }

        public static TReturn InvokeNonPublicProperty<TReturn>(object source, string propertyName)
        {
            var propertyInfo = source.GetType().GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance, null, typeof(TReturn), new Type[0], null);
            if (propertyInfo == null)
            {
                throw new ArgumentException(string.Format("Could not find a propertyName with the name '{0}'", propertyName), "propertyName");
            }

            return (TReturn)propertyInfo.GetValue(source, null);
        }

        public static object InvokeNonPublicProperty(object source, string propertyName)
        {
            var propertyInfo = source.GetType().GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (propertyInfo == null)
            {
                throw new ArgumentException(string.Format("Could not find a propertyName with the name '{0}'", propertyName), "propertyName");
            }

            return propertyInfo.GetValue(source, null);
        }
    }

    /// <summary>
    /// Used to simulate an HttpRequest.
    /// </summary>
    public class SimulatedHttpRequest : SimpleWorkerRequest
    {
        Uri _referer;
        readonly string _host;
        readonly string _verb;
        readonly int _port;
        readonly string _physicalFilePath;

        /// <summary>
        /// Creates a new <see cref="SimulatedHttpRequest"/> instance.
        /// </summary>
        /// <param name="applicationPath">App virtual dir.</param>
        /// <param name="physicalAppPath">Physical Path to the app.</param>
        /// <param name="physicalFilePath">Physical Path to the file.</param>
        /// <param name="page">The Part of the URL after the application.</param>
        /// <param name="query">Query.</param>
        /// <param name="output">Output.</param>
        /// <param name="host">Host.</param>
        /// <param name="port">Port to request.</param>
        /// <param name="verb">The HTTP Verb to use.</param>
        public SimulatedHttpRequest(string applicationPath, string physicalAppPath, string physicalFilePath, string page, string query, TextWriter output, string host, int port, string verb) : base(applicationPath, physicalAppPath, page, query, output)
        {
            if (host == null)
            {
                throw new ArgumentNullException("host", "Host cannot be null.");
            }

            if (host.Length == 0)
            {
                throw new ArgumentException("Host cannot be empty.", "host");
            }

            if (applicationPath == null)
            {
                throw new ArgumentNullException("applicationPath", "Can't create a request with a null application path. Try empty string.");
            }

            _host = host;
            _verb = verb;
            _port = port;
            _physicalFilePath = physicalFilePath;
        }

        internal void SetReferer(Uri referer)
        {
            _referer = referer;
        }

        /// <summary>
        /// Returns the specified member of the request header.
        /// </summary>
        /// <returns>
        /// The HTTP verb returned in the request
        /// header.
        /// </returns>
        public override string GetHttpVerbName()
        {
            return _verb;
        }

        /// <summary>
        /// Gets the name of the server.
        /// </summary>
        /// <returns></returns>
        public override string GetServerName()
        {
            return _host;
        }

        public override int GetLocalPort()
        {
            return _port;
        }

        /// <summary>
        /// Gets the headers.
        /// </summary>
        /// <value>The headers.</value>
        public NameValueCollection Headers
        {
            get
            {
                return headers;
            }
        }

        private readonly NameValueCollection headers = new NameValueCollection();

        /// <summary>
        /// Gets the format exception.
        /// </summary>
        /// <value>The format exception.</value>
        public NameValueCollection Form
        {
            get
            {
                return formVariables;
            }
        }
        private readonly NameValueCollection formVariables = new NameValueCollection();

        /// <summary>
        /// Get all nonstandard HTTP header name-value pairs.
        /// </summary>
        /// <returns>An array of header name-value pairs.</returns>
        public override string[][] GetUnknownRequestHeaders()
        {
            if (headers == null || headers.Count == 0)
            {
                return null;
            }
            var headersArray = new string[headers.Count][];
            for (var i = 0; i < headers.Count; i++)
            {
                headersArray[i] = new string[2];
                headersArray[i][0] = headers.Keys[i];
                headersArray[i][1] = headers[i];
            }
            return headersArray;
        }

        public override string GetKnownRequestHeader(int index)
        {
            if (index == 0x24)
            {
                return _referer == null ? string.Empty : _referer.ToString();
            }

            if (index == 12 && _verb == "POST")
            {
                return "application/x-www-form-urlencoded";
            }

            return base.GetKnownRequestHeader(index);
        }

        /// <summary>
        /// Returns the virtual path to the currently executing
        /// server application.
        /// </summary>
        /// <returns>
        /// The virtual path of the current application.
        /// </returns>
        public override string GetAppPath()
        {
            var appPath = base.GetAppPath();
            return appPath;
        }

        public override string GetAppPathTranslated()
        {
            var path = base.GetAppPathTranslated();
            return path;
        }

        public override string GetUriPath()
        {
            var uriPath = base.GetUriPath();
            return uriPath;
        }

        public override string GetFilePathTranslated()
        {
            return _physicalFilePath;
        }

        /// <summary>
        /// Reads request data from the client (when not preloaded).
        /// </summary>
        /// <returns>The number of bytes read.</returns>
        public override byte[] GetPreloadedEntityBody()
        {
            var formText = formVariables.Keys.Cast<string>()
                .Aggregate(string.Empty, (current, key) => current + 
                    string.Format("{0}={1}&", key, formVariables[key]));

            return Encoding.UTF8.GetBytes(formText);
        }

        /// <summary>
        /// Returns a value indicating whether all request data
        /// is available and no further reads from the client are required.
        /// </summary>
        /// <returns>
        /// 	<see langword="true"/> if all request data is available; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public override bool IsEntireEntityBodyIsPreloaded()
        {
            return true;
        }
    }
}
