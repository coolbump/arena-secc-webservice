
using System;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Web.SessionState;
using System.Reflection;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;


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
		MethodInfo _methodInfo;
		String _method, _uri;
		String[] _uriElements;

		public RestMethodInfo(String httpMethod, String uri, MethodInfo mi)
		{
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

		public MethodInfo methodInfo { get { return _methodInfo; } }

		public String method { get { return _method; } }

		public String uri { get { return _uri; } }

		public String[] uriElements { get { return _uriElements; } }
	}

	class RestApi : IHttpHandler
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
		/// class.
		/// </summary>
		void RegisterInternalHandlers()
		{
			RegisterHandler("GET", "/person/list", this.GetType().GetMethod("PersonList"));
			RegisterHandler("GET", "/person/{personId}", this.GetType().GetMethod("Person"));
			RegisterHandler("GET", "/person/{personId}/attribute/list", this.GetType().GetMethod("PersonAttributeList"));
			RegisterHandler("GET", "/person/{personId}/note", this.GetType().GetMethod("PersonNote"));
			RegisterHandler("GET", "/person/{personId}/note/list", this.GetType().GetMethod("PersonNoteList"));
			RegisterHandler("GET", "/test", this.GetType().GetMethod("Test"));
		}


		/// <summary>
		/// Register all external handlers by calling the registration methods
		/// of each registered library in the lookup table.
		/// </summary>
		void RegisterExternalHandlers()
		{
		}


		/// <summary>
		/// Register the given method with the specified url.
		/// </summary>
		/// <param name="url">The URL that will be used, relative to the service.api handler.</param>
		/// <param name="mi">The method to be invoked.</param>
		public void RegisterHandler(String method, String url, MethodInfo mi)
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
			rmi = new RestMethodInfo(method.ToUpper(), url, mi);

			//
			// Add the new method information into the list of handlers.
			//
			registeredHandlers.Add(rmi);
		}


		/// <summary>
		/// Given the URL, find the associated method handler.
		/// </summary>
		/// <param name="url">The URL to be traced out.</param>
		/// <param name="parameters">Any parameters in the URL will be placed in this table.</param>
		/// <returns>Either null or a valid MethodInfo reference to the method to be invoked.</returns>
		MethodInfo FindHandler(String method, String url, Hashtable parameters)
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
					return rmi.methodInfo;
			}

			return null;
		}
		#endregion


		#region Internal method handlers
		public void PersonList()
		{
		}
		public void Person(int personId)
		{
		}
		public void PersonAttributeList(int personId, String group)
		{
		}
		public string PersonNote(int personId, int value, string note)
		{
			if (note != null)
				return String.Format("Got person ID {0} and value {1} with note \"{2}\"", personId, value, note);

			return String.Format("Got person ID {0} and value {1}", personId, value);
		}
		public string PersonNoteList(int personId)
		{
			return String.Format("Got person ID {0}", personId);
		}
		public Stream Test()
		{
			byte[] buf = new byte[5];

			buf[0] = (byte)'h'; buf[1] = (byte)'E'; buf[2] = (byte)'l'; buf[3] = (byte)'L'; buf[4] = (byte)'o';
			return new MemoryStream(buf);
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
			MethodInfo methodInfo = null;


			//
			// Register all handlers.
			//
			RegisterHandlers();

			//
			// Try to parse out the request information.
			//
			try
			{
				context.Response.Write(String.Format("Method {0}<br />\n", context.Request.HttpMethod));
				context.Response.Write(String.Format("URL {0}<br />\n", context.Request.PathInfo));
				methodInfo = FindHandler(context.Request.HttpMethod.ToUpper(), context.Request.PathInfo, parameters);
				if (methodInfo == null)
					throw new MissingMethodException();
			}
			catch (Exception e)
			{
				context.Response.Write(String.Format("Exception occurred: {0}", e.Message));

				return;
			}

			//
			// Perform the actual method call.
			//
			try
			{
				Object result = null, p;
				ArrayList finalParameters = new ArrayList();

				foreach (ParameterInfo pi in methodInfo.GetParameters())
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

				result = methodInfo.Invoke(this, (object[])finalParameters.ToArray(typeof(object)));

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
				context.Response.Write(String.Format("Exception occurred: {0}", e.Message));

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
