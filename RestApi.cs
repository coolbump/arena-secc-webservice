
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


//
// If the called method has a parameter type of Stream and the method
// type is POST or PUT then dump the raw post/put data into the parameter.
//
// If the return type is Stream then send it raw as the response.
//
// In "auto-add" mode, the method must have OperationContract attribute AND
//	(WebGetAttribute OR WebInvokeAttribute).
// In "auto-add" mode, the WebGetAttribute contains a UriTemplate string for
// the URI to be used.
// In "auto-add" mode, the WebInvokeAttribute contains a UriTemplate string
// for the URI to be used and a Method attribute for the HTTP method to use.
// In "auto-add" mode, the URI must be truncated at the "?", if there is one.
//
namespace Arena.Custom.HDC.WebService
{
	class RestMethodInfo
	{
		Object _instance;
		MethodInfo _methodInfo;
		String _method, _uri;
		String[] _uriElements;

		public RestMethodInfo(Object instance, String httpMethod, String uri, MethodInfo mi)
		{
			_instance = instance;
			_methodInfo = mi;
			_method = httpMethod;
			_uri = uri;

			//
			// Bypass the first /
			//
			if (uri.Length == 0)
				_uriElements = new String[0];
			else if (uri[0] == '/')
				_uriElements = uri.Substring(1).Split('/');
			else
				_uriElements = uri.Split('/');
		}

		public Object instance { get { return _instance; } }

		public MethodInfo methodInfo { get { return _methodInfo; } }

		public String method { get { return _method; } }

		public String uri { get { return _uri; } }

		public String[] uriElements { get { return _uriElements; } }
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
		public Version Version()
		{
			Version v = new Version();
			v.Number = "1.0.2";
			return v;
		}
		[WebGet(UriTemplate = "/fault")]
		public RestErrorMessage Fault()
		{
			return new RestErrorMessage();
		}
	}

	public class RestErrorMessage : Message
	{
		protected override void OnWriteBodyContents(System.Xml.XmlDictionaryWriter writer)
		{
			writer.WriteStartElement("Error");
			writer.WriteElementString("StatusCode", "400");
			writer.WriteElementString("Message", "Some error occurred");
			writer.WriteEndElement();
		}
		public override MessageHeaders Headers
		{
			get { return new MessageHeaders(MessageVersion.None); }
		}
		public override MessageProperties Properties
		{
			get { return new MessageProperties(); }
		}
		public override MessageVersion Version
		{
			get { return MessageVersion.None; }
		}
	}

	public class RestApi : IHttpHandler
	{
		ArrayList registeredHandlers = null;


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

			RegisterObjectContractHandlers(baseUrl, instance);
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
		public void RegisterObjectContractHandlers(String baseUrl, object instance)
		{
			//
			// Strip any trailing "/" character.
			//
			if (baseUrl[baseUrl.Length - 1] == '/')
				baseUrl = baseUrl.Substring(0, baseUrl.Length - 1);

			foreach (MethodInfo mi in instance.GetType().GetMethods())
			{
				WebGetAttribute[] webgets;
				WebInvokeAttribute[] webinvokes;
				String url;

				//
				// Get any "WebGet" attributes for this method.
				//
				webgets = (WebGetAttribute[])mi.GetCustomAttributes(typeof(WebGetAttribute), true);
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
					url = webgets[0].UriTemplate;
					if (url.Length > 0 && url[0] == '/')
						url = baseUrl + url;
					else
						url = baseUrl + "/" + url;

					RegisterHandler(instance, webinvokes[0].Method, url, mi);

					continue;
				}
			}
		}


		/// <summary>
		/// Given the URL, find the associated method handler.
		/// </summary>
		/// <param name="url">The URL to be traced out.</param>
		/// <param name="parameters">Any parameters in the URL will be placed in this table.</param>
		/// <returns>Either null or a valid MethodInfo reference to the method to be invoked.</returns>
		RestMethodInfo FindHandler(String method, String url, Hashtable parameters)
		{
			String[] elements;
			int i;

			if (registeredHandlers == null)
				return null;

			//
			// Bypass the first / and create the elements array.
			//
			if (url.Length == 0)
				return null;
			if (url[0] == '/')
				url = url.Substring(1);
			elements = url.Split('/');

			//
			// Loop through and look for a matching method signature.
			//
			foreach (RestMethodInfo rmi in registeredHandlers)
			{
				//
				// Check the basics, correct method and right number of
				// elements.
				//
				if (rmi.method.Equals(method.ToUpper()) == false || rmi.uriElements.Length != elements.Length)
					continue;

				//
				// We need to check each element in turn and verify it is correct.
				//
				for (i = 0; i < rmi.uriElements.Length; i++)
				{
					String p = rmi.uriElements[i];
					String v = elements[i];

					//
					// If this is a parameter, just store it for use by the caller.
					//
					if (p.Length > 2 && p[0] == '{' && p[p.Length - 1] == '}')
					{
						if (parameters != null)
							parameters[p.Substring(1, p.Length - 2)] = v;
					}
					else if (p.Equals(v) == false)
						break;
				}

				//
				// See if we matched on all elements.
				//
				if (i == rmi.uriElements.Length)
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
			Hashtable parameters = new Hashtable();
			RestMethodInfo rmi = null;


			//
			// Register all handlers.
			//
			RegisterHandlers();

			//
			// Try to parse out the request information.
			//
			try
			{
				rmi = FindHandler(context.Request.HttpMethod.ToUpper(), context.Request.PathInfo, parameters);
				if (rmi == null)
					throw new MissingMethodException();
			}
			catch (Exception e)
			{
				context.Response.Write(String.Format("Exception occurred at init: {0}", e.Message));

				return;
			}

			//
			// Perform the actual method call.
			//
			try
			{
				Object result = null, p;
				ArrayList finalParameters = new ArrayList();

				foreach (ParameterInfo pi in rmi.methodInfo.GetParameters())
				{
					if (typeof(Stream).IsAssignableFrom(pi.ParameterType))
					{
						p = context.Request.InputStream;
					}
					else if (parameters.ContainsKey(pi.Name) == true)
					{
						p = Convert.ChangeType(parameters[pi.Name], pi.ParameterType);
					}
					else if (context.Request.QueryString.AllKeys.Contains(pi.Name) == true)
					{
						p = Convert.ChangeType(context.Request.QueryString[pi.Name], pi.ParameterType);
					}
					else
						p = null;

					finalParameters.Add(p);
				}

				result = rmi.methodInfo.Invoke(rmi.instance, (object[])finalParameters.ToArray(typeof(object)));

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

							msg.WriteMessage(xtw);
							context.Response.Write(sb.ToString());
						}
						else
						{
							DataContractSerializer serializer = new DataContractSerializer(result.GetType());

							serializer.WriteObject(context.Response.OutputStream, result);
						}
					}
				}
				catch (Exception e)
				{
					context.Response.Write(String.Format("Exception sending response: {0}", e.Message));
				}
			}
			catch (Exception e)
			{
				context.Response.Write(String.Format("Exception occurred at run: {0}", e.Message));

				return;
			}
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
