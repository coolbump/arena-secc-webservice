
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.Text;
using System.Web;
using System.Web.SessionState;
using System.Xml;
using System.Xml.Serialization;
using Arena.Core;
using Arena.Services;
using Arena.Services.Behaviors.ErrorHandling;
using Arena.Services.Exceptions;


namespace Arena.Custom.HDC.WebService
{
	/// <summary>
	/// Provides all the information needed to find and call a method that
	/// has been registered in the API system.
	/// </summary>
	class RestMethodInfo
	{
		Object _instance;
		MethodInfo _methodInfo;
		UriTemplate _uriTemplate;
		String _method;

		public RestMethodInfo(Object instance, String httpMethod, String uri, MethodInfo mi)
		{
			_instance = instance;
			_methodInfo = mi;
			_method = httpMethod;
			_uriTemplate = new UriTemplate(uri);
		}

		public Object instance { get { return _instance; } }

		public MethodInfo methodInfo { get { return _methodInfo; } }

		public UriTemplate uriTemplate { get { return _uriTemplate; } }

		public String method { get { return _method; } }
	}

	/// <summary>
	/// When this attribute is applied to a WebGet or WebInvoke enabled
	/// it becomes an anonymous and does not require authentication to
	/// be called.
	/// </summary>
	public class RestApiAnonymous : System.Attribute
	{
	}

	[DataContract]
	public class Version
	{
		[DataMember]
		public string Number { get; set; }
	}

	public class RestServiceApi
	{
		public void RegisterHandlers(String baseUrl, RestApi api)
		{
		}
	}

	public class CustomServiceApi : RestServiceApi
	{
		[WebGet(UriTemplate = "/version")]
		[RestApiAnonymous]
		public Version Version()
		{
			Version v = new Version();
			v.Number = "1.0.2";
			return v;
		}
		[WebGet(UriTemplate = "/fault")]
		[RestApiAnonymous]
		public void Fault()
		{
			throw new Exception("This is an exception");
		}
	}

	/// <summary>
	/// The NoOp interface is a junk interface, it does nothing except
	/// provide a means for creating an OperationContract.
	/// </summary>
	[ServiceContract]
	interface NoOp
	{
		[OperationContract]
		void NoOp();
	}

	public class RestApi : IHttpHandler
	{
		ArrayList registeredHandlers = null;
		StringBuilder initLog = new StringBuilder();


		#region Handler registration code
		/// <summary>
		/// Register all handlers in the system both internal and
		/// external.
		/// </summary>
		void RegisterHandlers()
		{
			RegisterInternalHandlers();
			RegisterExternalHandlers();
		}


		/// <summary>
		/// Register all internal handlers that are a part of this
		/// assembly.
		/// </summary>
		void RegisterInternalHandlers()
		{
			RegisterObjectContractHandlers("/", this, this.GetType());

			CoreRpc rpc = new CoreRpc("00000000-0000-0000-0000-000000000000");
			RegisterHandler(rpc, "GET", "/contact", rpc.GetType().GetMethod("GetPersonContactInformation"));
			RegisterHandler(rpc, "GET", "/roots?profileType={profileType}", rpc.GetType().GetMethod("GetProfileRoots"));
		}

		/// <summary>
		/// Ths is a debug method that provides information about what is
		/// registered and the registration log.
		/// </summary>
		/// <param name="showLog"></param>
		/// <returns></returns>
		[WebGet(UriTemplate = "/info?showLog={showLog}")]
		[RestApiAnonymous()]
		public Stream Info(int showLog)
		{
			StringBuilder sb = new StringBuilder();

			HttpContext.Current.Response.ContentType = "text/plain";
			foreach (RestMethodInfo rmi in registeredHandlers)
			{
				sb.AppendLine(rmi.uriTemplate.ToString());
			}
			sb.AppendLine("");

			if (showLog == 1)
			{
				sb.AppendLine("Log:");
				sb.AppendLine(initLog.ToString());
				sb.AppendLine("");
			}

			return new MemoryStream(ASCIIEncoding.Default.GetBytes(sb.ToString()));
		}


		/// <summary>
		/// Register all external handlers by calling the registration methods
		/// of each registered library in the lookup table.
		/// </summary>
		void RegisterExternalHandlers()
		{
			String assemblyName, namespaceName, className;


			assemblyName = "Arena.Custom.HDC.WebService";
			namespaceName = "Arena.Custom.HDC.WebService";
			className = "CustomServiceApi";

			RegisterExternalClass("/", assemblyName, namespaceName, className);

			RegisterExternalClass("/core", "Arena.Services", "Arena.Services", "ArenaAPI");
		}


		/// <summary>
		/// Register the specified class given its assembly name (dll), namespace and
		/// class name. A new instance of that class is created and registered into the
		/// base url.
		/// </summary>
		/// <param name="baseUrl">The base url to use when registering this object.</param>
		/// <param name="assemblyName">The assembly (dll) name to load the class from.</param>
		/// <param name="namespaceName">The namespace that the class is a part of.</param>
		/// <param name="className">The name of the class to create an instance of.</param>
		void RegisterExternalClass(string baseUrl, String assemblyName, string namespaceName, string className)
		{
			Object instance;
			RestServiceApi service;
			Assembly asm;

	
			//
			// Try to load the assembly for the given class.
			//
			asm = Assembly.Load(assemblyName);
			if (asm == null)
				throw new Exception("Cannot load assembly");

			//
			// Try to load the class that will handle API service calls.
			//
			instance = asm.CreateInstance(namespaceName + "." + className);
			if (instance == null)
				throw new Exception("Cannot instantiate service");
			Type t = asm.GetType(namespaceName + "." + className);
			if (t == null)
				throw new Exception("Frank did it");
			while (t != null)
			{
				initLog.AppendLine("Checking type " + t.ToString());
				foreach (MethodInfo mi in t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
				{
					initLog.AppendLine(mi.Name);
					foreach (Object o in System.Attribute.GetCustomAttributes(mi, true))
					{
						initLog.AppendLine(mi.Name + "[" + o.ToString() + "]");
					}
				}
				t = t.BaseType;
			}

			//
			// If this object is a subclass of the RestServiceApi then call
			// the standard registration handler method which allows a subclass
			// to do any custom registration it needs to.
			//
			if (typeof(RestServiceApi).IsAssignableFrom(instance.GetType()) == true)
			{
				service = (RestServiceApi)instance;

				//
				// Initialize the API service and have it register handlers.
				//
				service.RegisterHandlers(baseUrl, this);
			}

			RegisterObjectContractHandlers(baseUrl, instance, instance.GetType());
		}


		/// <summary>
		/// Register the given method with the specified url.
		/// </summary>
		/// <param name="url">The URL that will be used, relative to the service.api handler.</param>
		/// <param name="mi">The method to be invoked.</param>
		public void RegisterHandler(object instance, String method, String url, MethodInfo mi)
		{
			RestMethodInfo rmi;

			//
			// Create the root level if it does not exist.
			//
			if (registeredHandlers == null)
			{
				registeredHandlers = new ArrayList();
			}

			//
			// Create the REST state method information.
			//
			rmi = new RestMethodInfo(instance, method.ToUpper(), url, mi);

			//
			// Add the new method information into the list of handlers.
			//
			registeredHandlers.Add(rmi);
		}


		/// <summary>
		/// Look for any WCF style methods that contain a WebGet or WebInvoke
		/// attribute. On any found methods, register the method as a url
		/// handler for that instance.
		/// </summary>
		/// <param name="baseUrl">The base URL to use when registering methods for this instance, pass an empty string for no base url.</param>
		/// <param name="instance">The object whose methods will be registered into the URL handlers.</param>
		public void RegisterObjectContractHandlers(String baseUrl, object instance, Type objectType)
		{
			//
			// Strip any trailing "/" character.
			//
			if (baseUrl.Length > 0 && baseUrl[baseUrl.Length - 1] == '/')
				baseUrl = baseUrl.Substring(0, baseUrl.Length - 1);

			foreach (MethodInfo mi in objectType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
			{
				Object[] attribs;
				WebGetAttribute[] webgets;
				WebInvokeAttribute[] webinvokes;
				String url;

				initLog.AppendLine("Checking method " + mi.Name);
				//
				// Get any "WebGet" attributes for this method.
				//
				attribs = (Object[])mi.GetCustomAttributes(false);
				foreach (Object attr in attribs)
				{
					initLog.AppendLine("  Found attribute " + attr.ToString());
				}

				webgets = (WebGetAttribute[])mi.GetCustomAttributes(typeof(WebGetAttribute), true);
				initLog.AppendLine("  Found " + webgets.Length.ToString() + " get attributes");
				if (webgets.Length > 0)
				{
					url = webgets[0].UriTemplate;
					if (url.Length > 0 && url[0] == '/')
						url = baseUrl + url;
					else
						url = baseUrl + "/" + url;

					RegisterHandler(instance, "GET", url, mi);
					continue;
				}

				//
				// Get any "WebInvoke" attributes for this method.
				//
				webinvokes = (WebInvokeAttribute[])mi.GetCustomAttributes(typeof(WebInvokeAttribute), true);
				if (webinvokes.Length > 0)
				{
					url = webinvokes[0].UriTemplate;
					if (url.Length > 0 && url[0] == '/')
						url = baseUrl + url;
					else
						url = baseUrl + "/" + url;

					RegisterHandler(instance, webinvokes[0].Method, url, mi);

					continue;
				}
			}

			foreach (Type t in objectType.GetInterfaces())
			{
				RegisterObjectContractHandlers(baseUrl, instance, t);
			}
		}


		/// <summary>
		/// Given the URL, find the associated method handler.
		/// </summary>
		/// <param name="baseUrl">The base URL of the web service handler.</param>
		/// <param name="url">The relative URL of the web service for the specific request.</param>
		/// <param name="match">The UriTemplateMatch object that contains information about the UriTemplate that was matched.</param>
		/// <returns>Either null or a valid MethodInfo reference to the method to be invoked.</returns>
		RestMethodInfo FindHandler(String method, Uri baseUrl, Uri url, ref UriTemplateMatch match)
		{
			if (registeredHandlers == null)
				return null;

			//
			// Loop through and look for a matching method signature.
			//
			foreach (RestMethodInfo rmi in registeredHandlers)
			{
				//
				// Ensure the proper method is in use.
				//
				if (rmi.method.ToUpper() != method.ToUpper())
					continue;

				//
				// See if there is a match on the URI.
				//
				if ((match = rmi.uriTemplate.Match(baseUrl, url)) != null)
					return rmi;
			}

			return null;
		}
		#endregion


		#region Http Handler methods

		/// <summary>
		/// Process the web request.
		/// </summary>
		/// <param name="context">The context of this single web request.</param>
		public void ProcessRequest(HttpContext context)
		{
			UriTemplateMatch templateMatch = null;
			RestMethodInfo rmi = null;
			ArrayList finalParameters = null;
			Object result = null, p;


			//
			// Initialization phase, register all handlers and then find a match.
			//
			try
			{
				//
				// Register all handlers.
				//
				RegisterHandlers();

				String baseUrl = context.Request.Url.Scheme + "://" + context.Request.Url.Authority + context.Request.FilePath;
				rmi = FindHandler(context.Request.HttpMethod.ToUpper(), new Uri(baseUrl), context.Request.Url, ref templateMatch);
				if (rmi == null)
					throw new MissingMethodException();
			}
			catch (Exception e)
			{
				context.Response.Write(String.Format("Exception occurred at init: {0}", e.Message + e.StackTrace));

				return;
			}

			//
			// Parse out any parameters for the method call.
			//
			try
			{
				finalParameters = new ArrayList();

				//
				// Walk each parameter in the method and see if we can convert
				// one of the query variables to the proper type.
				//
				foreach (ParameterInfo pi in rmi.methodInfo.GetParameters())
				{
					if (typeof(Stream).IsAssignableFrom(pi.ParameterType))
					{
						p = context.Request.InputStream;
					}
					else if (templateMatch.BoundVariables.AllKeys.Contains(pi.Name.ToUpper()) == true)
					{
						p = Convert.ChangeType(templateMatch.BoundVariables[pi.Name.ToUpper()], pi.ParameterType);
					}
					else
						p = null;

					finalParameters.Add(p);
				}
			}
			catch (Exception e)
			{
				context.Response.Write(String.Format("Exception occurred at parameter parse: {0} at {1}", e.Message, e.StackTrace));

				return;
			}

			//
			// Force the context to be anonymous, then authenticate if the user
			// is calling a non-anonymous method.
			//
			try
			{
				ArenaContext.Current.SetWebServiceProperties(ArenaContext.Current.CreatePrincipal(""), new Arena.Core.Person());
				if (rmi.uriTemplate.ToString() != "/core/version" &&
					rmi.uriTemplate.ToString() != "/core/login" &&
					rmi.uriTemplate.ToString() != "/core/help" &&
					rmi.methodInfo.GetCustomAttributes(typeof(RestApiAnonymous), true).Length == 0)
				{
					String PathAndQuery = context.Request.RawUrl;

					PathAndQuery = context.Request.RawUrl.Substring(context.Request.FilePath.Length + 1);
					AuthenticationManager.SetupSessionForRequest(context.Request.QueryString["api_session"], false);
					AuthenticationManager.VerifySignature(context.Request.Url, PathAndQuery, context.Request.QueryString["api_session"]);
				}
			}
			catch (Exception e)
			{
				RESTException restEx = e as RESTException;

				if (restEx != null)
				{
					result = new RestErrorMessage(restEx);
				}
				else
				{
					result = new RestErrorMessage(System.Net.HttpStatusCode.InternalServerError, e.ToString(), string.Empty);
				}
			}

			//
			// Perform the actual method call.
			//
			if (result == null)
			{
				try
				{
					//
					// Set some default response information.
					//
					context.Response.ContentType = "application/xml; charset=utf-8";

					if (TypeIsServiceContract(rmi.instance.GetType()) == true)
					{
						//
						// Run the request inside of a operation context so response information
						// can be set. This is a bit of a cheat, but it works.
						//
						WebChannelFactory<NoOp> factory = new WebChannelFactory<NoOp>(new Uri("http://localhost/"));
						NoOp channel = factory.CreateChannel();
						using (new OperationContextScope((IContextChannel)channel))
						{
							result = rmi.methodInfo.Invoke(rmi.instance, (object[])finalParameters.ToArray(typeof(object)));
							if (WebOperationContext.Current.OutgoingResponse.ContentType != null)
								context.Response.ContentType = WebOperationContext.Current.OutgoingResponse.ContentType;
						}
					}
					else
					{
						//
						// This is a standard method call, just call it.
						//
						result = rmi.methodInfo.Invoke(rmi.instance, (object[])finalParameters.ToArray(typeof(object)));
					}
				}
				catch (Exception e)
				{
					RESTException restEx;

					if (e.InnerException != null)
						e = e.InnerException;

					restEx = e as RESTException;
					if (restEx != null)
					{
						result = new RestErrorMessage(restEx);
					}
					else
					{
						result = new RestErrorMessage(System.Net.HttpStatusCode.InternalServerError, e.ToString(), string.Empty);
					}
				}
			}

			//
			// Deal with the response that was generated.
			//
			try
			{
				if (result != null)
				{
					//
					// There is probably a better way to do this, but this is the best
					// I can come up with. Somebody feel free to make this cleaner.
					//
					if (typeof(Stream).IsAssignableFrom(result.GetType()) == true)
					{
						Stream s = (Stream)result;
						byte[] buf = new byte[8192];
						int count = 8192, offset = 0;

						//
						// Response is a data stream, just copy it to the response
						// stream.
						//
						for (offset = 0; count == 8192; offset += count)
						{
							int avail = (int)(s.Length - offset);

							count = s.Read(buf, offset, (avail > 8192 ? 8192 : avail));
							context.Response.BinaryWrite(buf);
						}
					}
					else if (typeof(Message).IsAssignableFrom(result.GetType()) == true)
					{
						Message msg = (Message)result;
						StringBuilder sb = new StringBuilder();
						StringWriter sw = new StringWriter(sb);
						XmlTextWriter xtw = new XmlTextWriter(sw);

						//
						// Response is a Message object. Write it out as an XML
						// stream.
						//
						msg.WriteMessage(xtw);
						context.Response.Write(sb.ToString());
					}
					else
					{
						DataContractSerializer serializer = new DataContractSerializer(result.GetType());

						//
						// Otherwise, use the DataContractSerializer to convert the object into
						// an XML stream.
						//
						serializer.WriteObject(context.Response.OutputStream, result);
					}
				}
			}
			catch (Exception e)
			{
				context.Response.Write(String.Format("Exception sending response: {0}", e.Message));

				return;
			}
		}

		/// <summary>
		/// Check the object type and any interfaces to see if it has any
		/// ServiceContract attributes.
		/// </summary>
		/// <param name="objectType">The object type to check.</param>
		/// <returns>true if the objectType or it's interfaces has a ServiceContract.</returns>
		private bool TypeIsServiceContract(Type objectType)
		{
			if (objectType.GetCustomAttributes(typeof(ServiceContractAttribute), true).Count() > 0)
				return true;

			foreach (Type t in objectType.GetInterfaces())
			{
				if (TypeIsServiceContract(t) == true)
					return true;
			}

			return false;
		}

		/// <summary>
		/// This HTTP handler is not reusable. Whatever that means.
		/// </summary>
		public bool IsReusable
		{
			get { return false; }
		}

		/// <summary>
		/// Convert the HttpRequest's InputStream (post data) into a
		/// String object.
		/// </summary>
		/// <param name="request">The request whose POST data we are intersted in.</param>
		/// <returns>String representation of the input stream.</returns>
		private String RequestString(HttpRequest request)
		{
			StringBuilder strmContents;
			Int32 counter, strLen, strRead;

			//
			// Convert the input stream into a byte array.
			//
			strLen = Convert.ToInt32(request.InputStream.Length);
			byte[] strArr = new byte[strLen];
			strRead = request.InputStream.Read(strArr, 0, strLen);

			//
			// Convert byte array to a text string.
			//
			strmContents = new StringBuilder();
			for (counter = 0; counter < strLen; counter++)
			{
				strmContents.AppendFormat("{0}", (char)strArr[counter]);
			}

			return strmContents.ToString();
		}

		#endregion
	}
}
