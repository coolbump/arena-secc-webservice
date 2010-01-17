
using System;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Web.SessionState;
using System.Reflection;
using System.Linq;
using System.Text;


namespace Arena.Custom.HDC.WebService
{
	class RestApi : IHttpHandler
	{
		Hashtable registeredHandlers = null;


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
			RegisterHandler("/person/list", this.GetType().GetMethod("PersonList"));
			RegisterHandler("/person/{personId}", this.GetType().GetMethod("Person"));
			RegisterHandler("/person/{personId}/attribute/list", this.GetType().GetMethod("PersonAttributeList"));
			RegisterHandler("/person/{personId}/note", this.GetType().GetMethod("PersonNote"));
			RegisterHandler("/person/{personId}/note/list", this.GetType().GetMethod("PersonNoteList"));
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
		public void RegisterHandler(String url, MethodInfo mi)
		{
			String[] elements;
			String element;
			Hashtable handlers;
			int i;

			//
			// Create the root level if it does not exist.
			//
			if (registeredHandlers == null)
			{
				registeredHandlers = new Hashtable();
			}
			handlers = registeredHandlers;

			//
			// Bypass the first /
			//
			if (url.Length == 0)
				return;
			if (url[0] == '/')
				url = url.Substring(1);
			elements = url.Split('/');

			//
			// Walk the handler list and place or build the tree
			// as we go.
			//
			for (i = 0; i < elements.Length; i++)
			{
				element = elements[i];

				if (handlers.ContainsKey(element) == false)
				{
					//
					// The element does not exist at this level, create it.
					//
					if ((i + 1) == elements.Length)
					{
						//
						// Last item, just set the method info.
						//
						handlers[element] = mi;

						return;
					}
					else
					{
						//
						// Create a new level.
						//
						handlers[element] = new Hashtable();
						handlers = (Hashtable)handlers[element];

						continue;
					}
				}
				else
				{
					//
					// The element exists at this level, modify it.
					//
					if ((i + 1) == elements.Length)
					{
						if (handlers[element].GetType() == typeof(Hashtable))
						{
							handlers = (Hashtable)handlers[element];
							handlers[""] = mi;
						}
						else
						{
							//
							// Last item, just set the method info.
							//
							handlers[element] = mi;
						}

						return;
					}
					else
					{
						//
						// Traverse into the level.
						//
						if (handlers[element].GetType() == typeof(Hashtable))
						{
							handlers = (Hashtable)handlers[element];

							continue;
						}
						else
						{
							MethodInfo tempMi;

							//
							// Convert the method into a new level.
							//
							tempMi = (MethodInfo)handlers[element];
							handlers[element] = new Hashtable();
							handlers = (Hashtable)handlers[element];
							handlers[""] = tempMi;

							continue;
						}
					}
				}
			}

			//
			// We are working with an existing level, just add in the method.
			//
			handlers[""] = mi;
		}


		/// <summary>
		/// Given the URL, find the associated method handler.
		/// </summary>
		/// <param name="url">The URL to be traced out.</param>
		/// <param name="parameters">Any parameters in the URL will be placed in this table.</param>
		/// <returns>Either null or a valid MethodInfo reference to the method to be invoked.</returns>
		MethodInfo FindHandler(String url, Hashtable parameters)
		{
			String[] elements;
			Hashtable handlers;
			String element;
			int i;

			if (registeredHandlers == null)
				return null;
			handlers = registeredHandlers;

			//
			// Bypass the first /
			//
			if (url.Length == 0)
				return null;
			if (url[0] == '/')
				url = url.Substring(1);
			elements = url.Split('/');

			for (i = 0; i < elements.Length; i++)
			{
				element = elements[i];

				if (handlers.ContainsKey(element) == false)
				{
					Boolean found = false;

					//
					// Check for {*} parameters
					//
					foreach (String p in handlers.Keys)
					{
						if (p.Length > 2 && p[0] == '{' && p[p.Length - 1] == '}')
						{
							if (parameters != null)
								parameters[p.Substring(1, p.Length - 2)] = element;
							if (typeof(MethodInfo).IsInstanceOfType(handlers[p]))
							{
								//
								// If this is not the right number of elements then
								// return no match.
								//
								if ((i + 1) < elements.Length)
									return null;

								return (MethodInfo)handlers[p];
							}

							//
							// Move to the next element.
							//
							handlers = (Hashtable)handlers[p];
							found = true;
							break;
						}
					}

					if (found == true)
						continue;

					return null;
				}

				//
				// We have the key, decide if this is the final match or not.
				//
				if (typeof(MethodInfo).IsInstanceOfType(handlers[element]))
				{
					//
					// If this is not the right number of elements then
					// return no match.
					//
					if ((i + 1) < elements.Length)
						return null;

					return (MethodInfo)handlers[element];
				}

				//
				// Move to the next element.
				//
				handlers = (Hashtable)handlers[element];
			}

			if (handlers.ContainsKey("") == true)
			{
				return (MethodInfo)handlers[""];
			}

			return null;
		}
		#endregion


		#region Some Debug code
		public String DuplicateString(String str, int count)
		{
			StringBuilder sb = new StringBuilder();
			int i;

			for (i = 0; i < count; i++)
			{
				sb.Append(str);
			}

			return sb.ToString();
		}

		public String DumpHashtable(Hashtable table, int level)
		{
			StringBuilder str = new StringBuilder();

			str.Append("{<br />\n");
			foreach (string key in table.Keys)
			{
				if (table[key].GetType() == typeof(Hashtable))
				{
					str.Append(DuplicateString("&nbsp;", level * 4));
					str.Append(String.Format("{0}: {1}<br />\n", key, DumpHashtable((Hashtable)table[key], level + 1)));
				}
				else if (typeof(MethodInfo).IsInstanceOfType(table[key]))
				{
					str.Append(DuplicateString("&nbsp;", level * 4));
					str.Append(String.Format("{0}: {1}<br />\n", key, ((MethodInfo)table[key]).Name));
				}
				else if (typeof(String).IsInstanceOfType(table[key]))
				{
					str.Append(DuplicateString("&nbsp;", level * 4));
					str.Append(String.Format("{0}: {1}<br />\n", key, (String)table[key]));
				}
				else
				{
					str.Append(DuplicateString("&nbsp;", level * 4));
					str.Append(String.Format("{0}: (Unknown value type {1})<br />\n", key, table[key].GetType().ToString()));
				}
			}
			str.Append(DuplicateString("&nbsp;", level * 4));
			str.Append("}");

			return str.ToString();
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
				methodInfo = FindHandler(context.Request.PathInfo, parameters);
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
					if (parameters.ContainsKey(pi.Name) == true)
					{
						p = parameters[pi.Name];
					}
					else if (context.Request.QueryString.AllKeys.Contains(pi.Name) == true)
					{
						p = context.Request.QueryString[pi.Name];
					}
					else
						p = null;

					if (p != null)
						finalParameters.Add(Convert.ChangeType(p, pi.ParameterType));
					else
						finalParameters.Add(null);
				}

				result = methodInfo.Invoke(this, (object[])finalParameters.ToArray(typeof(object)));

				try
				{
					if (result != null)
						context.Response.Write(result.ToString());
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
