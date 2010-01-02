
using System;
using System.Collections;
using System.Web;
using System.Web.SessionState;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Procurios.Public;


namespace Arena.Custom.HDC.WebService
{
	public class JsonRpcHandler : IHttpHandler
	{
		public void ProcessRequest(HttpContext context)
		{
			Hashtable response = new Hashtable();
			Hashtable request;
			String method;
			Object parameters, requestID;
			Type thisType = this.GetType();
			ArrayList paramList = new ArrayList();
			StringBuilder paramTypes = new StringBuilder();
			MethodInfo methodInfo = null;
			int i;

			//
			// Try to parse out the request information.
			//
			try
			{
				if (context.Request.Params["value"] != null)
					request = (Hashtable)JSON.JsonDecode(context.Request.Params["value"]);
				else
					request = (Hashtable)JSON.JsonDecode(RequestString(context.Request));
			}
			catch (Exception e)
			{
				Hashtable error = new Hashtable();

				error["code"] = -32700;
				error["message"] = e.Message;
				response["error"] = error;
				context.Response.Write(JSON.JsonEncode(response));

				return;
			}

			//
			// Try to parse out the information about the request.
			//
			try
			{
				requestID = request["id"];
				method = (String)request["method"];
				parameters = request["params"];
				response["id"] = requestID;

				//
				// Parse the parameters if they are ones we support.
				//
				if (parameters == null)
				{
					parameters = new ArrayList();
				}
				else if (parameters.GetType() == typeof(ArrayList))
				{
					foreach (Object param in (ArrayList)parameters)
					{
						paramList.Add(param.GetType());
						paramTypes.AppendFormat("{0}, ", param.GetType().ToString());
					}

					if (paramTypes.Length > 0)
						paramTypes.Remove(paramTypes.Length - 2, 2);
				}
				else
				{
					Hashtable error = new Hashtable();

					error["code"] = -32602;
					error["message"] = "Unsupported parameter list specified.";
					response["error"] = error;
					context.Response.Write(JSON.JsonEncode(response));

					return;
				}
			}
			catch (Exception e)
			{
				Hashtable error = new Hashtable();

				error["code"] = -32600;
				error["message"] = e.Message;
				response["error"] = error;
				context.Response.Write(JSON.JsonEncode(response));

				return;
			}

			//
			// Make an attempt to find the requested method.
			//
			try
			{
				MethodInfo[] methodList = thisType.GetMethods();

				for (i = 0; i < methodList.Length; i++)
				{
					if (methodList[i].IsPublic == false || methodList[i].IsStatic == true)
						continue;

					if (methodList[i].Name == method && methodList[i].GetParameters().Length == paramList.Count)
					{
						methodInfo = methodList[i];
						break;
					}
				}

				if (methodInfo == null)
					throw new Exception(String.Format("Method {0}({1}) not found.", method, paramTypes.ToString()));
			}
			catch (Exception e)
			{
				Hashtable error = new Hashtable();

				error["code"] = -32601;
				error["message"] = e.Message;
				response["error"] = e;
				context.Response.Write(JSON.JsonEncode(response));

				return;
			}

			//
			// Perform the actual method call.
			//
			try
			{
				Object result = null;
				ArrayList finalParameters = new ArrayList();

				for (i = 0; i < ((ArrayList)parameters).Count; i++)
				{
					finalParameters.Add(Convert.ChangeType(((ArrayList)parameters)[i], methodInfo.GetParameters()[i].ParameterType));
				}

				try
				{
					result = methodInfo.Invoke(this, (object[])finalParameters.ToArray(typeof(object)));
				}
				catch (Exception e)
				{
					throw e.InnerException;
				}

				if (result != null)
					response["result"] = result;
			}
			catch (Exception e)
			{
				Hashtable error = new Hashtable();

				error["code"] = -32603;
				error["message"] = e.Message;
				response["error"] = error;
				context.Response.Write(JSON.JsonEncode(response));

				return;
			}

			try
			{
				context.Response.Write(JSON.JsonEncode(response));
			}
			catch
			{
				context.Response.Write("{\"error\": { \"code\": -32099, \"message\": \"Internal server error.\" } }");
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
	}

    public class JsonRpc : JsonRpcHandler
    {
        #region Anonymous (non-authenticated) methods.

        public IDictionary Version()
        {
            return (IDictionary)JsonConverter.EncodeObject(CoreRpc.Version());
        }


        public bool IsClientVersionSupported(int major, int minor)
        {
            return CoreRpc.IsClientVersionSupported(major, minor);
        }


        public string Login(string loginID, string password)
        {
            return CoreRpc.Login(loginID, password);
        }

        #endregion


        #region Methods for working with people records.

        public int[] FindPeopleByName(string authorization, string firstName, string lastName)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return rpc.FindPeopleByName(firstName, lastName);
        }


        public int[] FindPeopleByPhone(string authorization, string phone)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return rpc.FindPeopleByPhone(phone);
        }


        public int[] FindPeopleByEmail(string authorization, string email)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return rpc.FindPeopleByEmail(email);
        }


        public int[] FindPeople(string authorization, IDictionary query)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return rpc.FindPeople((RpcPeopleQuery)JsonConverter.DecodeObject(query, typeof(RpcPeopleQuery)));
        }


        public IDictionary GetPersonInformation(string authorization, int personID)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return (IDictionary)JsonConverter.EncodeObject(rpc.GetPersonInformation(personID));
        }


        public IDictionary GetPersonContactInformation(string authorization, int personID)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return (IDictionary)JsonConverter.EncodeObject(rpc.GetPersonContactInformation(personID));
        }


        public IDictionary GetPersonPeers(string authorization, int personID, int start, int count)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return (IDictionary)JsonConverter.EncodeObject(rpc.GetPersonPeers(personID, start, count));
        }
        

        public IDictionary GetPersonProfiles(string authorization, int personID)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return (IDictionary)JsonConverter.EncodeObject(rpc.GetPersonProfiles(personID));
        }


        public Object[] GetPersonNotes(string authorization, int personID)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return (Object[])JsonConverter.EncodeObject(rpc.GetPersonNotes(personID));
        }


		public void UpdatePersonImage(string authorization, int personID, string imageData)
		{
			CoreRpc rpc = new CoreRpc(authorization);
			byte[] data = Convert.FromBase64String(imageData);

			rpc.UpdatePersonImage(personID, data);
		}


        #endregion


        #region Methods for working with profile records.

        public IDictionary GetProfileInformation(string authorization, int profileID)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return (IDictionary)JsonConverter.EncodeObject(rpc.GetProfileInformation(profileID));
        }


        public IDictionary GetProfileMemberInformation(string authorization, int profileID, int personID)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return (IDictionary)JsonConverter.EncodeObject(rpc.GetProfileMemberInformation(profileID, personID));
        }


		public Object[] GetProfileMemberActivity(string authorization, int profileID, int personID)
		{
			CoreRpc rpc = new CoreRpc(authorization);

			return (Object[])JsonConverter.EncodeObject(rpc.GetProfileMemberActivity(profileID, personID));
		}


        public int[] GetProfileChildren(string authorization, int profileID)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return rpc.GetProfileChildren(profileID);
        }


        public int[] GetProfileRoots(string authorization, int profileType)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return rpc.GetProfileRoots(profileType);
        }


        public int[] GetProfileMembers(string authorization, int profileID)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return rpc.GetProfileMembers(profileID);
        }


        public int[] GetProfileOccurrences(string authorization, int profileID)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return rpc.GetProfileOccurrences(profileID);
        }

        #endregion


		#region Small Group Methods

		public int[] GetSmallGroupCategories(string authorization)
		{
			CoreRpc rpc = new CoreRpc(authorization);

			return rpc.GetSmallGroupCategories();
		}

		public IDictionary GetSmallGroupCategoryInformation(string authorization, int categoryID)
		{
			CoreRpc rpc = new CoreRpc(authorization);

			return (IDictionary)JsonConverter.EncodeObject(rpc.GetSmallGroupCategoryInformation(categoryID));
		}

		public int[] GetSmallGroupRootClusters(string authorization, int categoryID)
		{
			CoreRpc rpc = new CoreRpc(authorization);

			return rpc.GetSmallGroupRootClusters(categoryID);
		}

		public IDictionary GetSmallGroupClusterTypeInformation(string authorization, int typeID)
		{
			CoreRpc rpc = new CoreRpc(authorization);

			return (IDictionary)JsonConverter.EncodeObject(rpc.GetSmallGroupClusterTypeInformation(typeID));
		}

		public IDictionary GetSmallGroupClusterInformation(string authorization, int clusterID)
		{
			CoreRpc rpc = new CoreRpc(authorization);
			
			return (IDictionary)JsonConverter.EncodeObject(rpc.GetSmallGroupClusterInformation(clusterID));
		}

		public int[] GetSmallGroupClusters(string authorization, int clusterID)
		{
			CoreRpc rpc = new CoreRpc(authorization);

			return rpc.GetSmallGroupClusters(clusterID);
		}

		public int[] GetSmallGroups(string authorization, int clusterID)
		{
			CoreRpc rpc = new CoreRpc(authorization);

			return rpc.GetSmallGroups(clusterID);
		}

		public IDictionary GetSmallGroupInformation(string authorization, int groupID)
		{
			CoreRpc rpc = new CoreRpc(authorization);

			return (IDictionary)JsonConverter.EncodeObject(rpc.GetSmallGroupInformation(groupID));
		}

		public Object[] GetSmallGroupMembers(string authorization, int groupID, int startAtIndex, int numberOfMembers)
		{
			CoreRpc rpc = new CoreRpc(authorization);

			return (Object[])JsonConverter.EncodeObject(rpc.GetSmallGroupMembers(groupID, startAtIndex, numberOfMembers));
		}

		public int[] GetSmallGroupOccurrences(string authorization, int groupID)
		{
			CoreRpc rpc = new CoreRpc(authorization);

			return rpc.GetSmallGroupOccurrences(groupID);
		}

		#endregion
	}

    /// <summary>
    /// This class provides the ability to encode and decode objects
    /// for use by the CoreRpc class. We do this as the current JSON-
    /// RPC library does not perfectly honor capitalization. If a struct
    /// has a member named "UserName" then it gets sent as "userName"
    /// which could confuse case-sensitive clients. The library also
    /// currently does not support nullable primitives, though that is
    /// on their list of things to be fixed for the next version.
    /// </summary>
    public class JsonConverter
    {
        /// <summary>
        /// Encode the given generic object into its encoded format. For
        /// many objects (ints, strings, etc.) the original object is
        /// simply returned. Collections are traversed to make sure all
        /// child objects are properly encoded to preserve case names.
        /// </summary>
        /// <param name="value">The object to be encoded.</param>
        /// <returns>A possibly new object which should contain the same information.</returns>
        public static Object EncodeObject(Object value)
        {
            Type valueType;
            Type[] typeArray = new Type[0];


            //
            // Null is null.
            //
            if (value == null)
                return null;
            valueType = value.GetType();

            //
            // Check for a nullable type.
            //
            if (valueType.GetMethod("GetValueOrDefault", typeArray) != null)
            {
                valueType = Nullable.GetUnderlyingType(valueType);
            }

            //
            // Check for a basic primitive.
            //
            if (valueType.IsPrimitive || valueType == typeof(String))
                return value;

            //
            // Check for an enumeration.
            //
            if (valueType.IsEnum)
            {
                return Enum.GetName(valueType, value);
            }

            //
            // Check for an array of objects.
            //
            if (valueType.IsArray)
            {
                ICollection collection = (ICollection)value;
                Object[] newArray = new Object[collection.Count];
                int i = 0;

                foreach (Object obj in collection)
                {
                    newArray[i++] = EncodeObject(obj);
                }

                return newArray;
            }

            //
            // Check for a dictionary of objects.
            //
            if (value is IDictionary)
            {
                IDictionary dictionary = (IDictionary)value;
                Hashtable hash = new Hashtable();

                foreach (Object key in dictionary.Keys)
                {
                    hash[key] = EncodeObject(dictionary[key]);
                }

                return hash;
            }

            //
            // Check for a DateTime class.
            //
            if (value is DateTime)
            {
                StringBuilder str = new StringBuilder();
                DateTime date = (DateTime)value;

                str.AppendFormat("{0:s}Z", date.ToUniversalTime());

                return str.ToString();
            }

            //
            // Check for a class or struct
            //
            if (valueType.IsClass || valueType.IsValueType)
            {
                Hashtable hash = new Hashtable();
                FieldInfo[] fields;
                Object fieldValue;

                fields = valueType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (FieldInfo field in fields)
                {
                    fieldValue = field.GetValue(value);
                    if (fieldValue != null)
                    {
                        hash[field.Name] = EncodeObject(fieldValue);
                    }
                }

                return hash;
            }

            //
            // Unknown type.
            //
            throw new InvalidCastException("Unknown parameter type: " + valueType.ToString());
        }

        /// <summary>
        /// Decode the object into the value type specified. This method
        /// should be called only when the target value type (such as a
        /// structure or class) is known, otherwise you basically get back
        /// the same object you passed.
        /// </summary>
        /// <param name="value">The object to decode into its native format.</param>
        /// <param name="valueType">The Type of the object, specifies what that native format is.</param>
        /// <returns>Hopefully a new object that is in the native format.</returns>
        public static Object DecodeObject(Object value, Type valueType)
        {
            Type[] typeArray = new Type[0];


            //        Console.WriteLine("Decode " + value.ToString() + "[" + valueType.Name + "]");

            //
            // Null is null.
            //
            if (value == null)
                return null;

            //
            // Check for a nullable type.
            //
            if (valueType.GetMethod("GetValueOrDefault", typeArray) != null)
            {
                valueType = Nullable.GetUnderlyingType(valueType);
            }

            //
            // Check for a datetime.
            //
            if (valueType == typeof(String) && ((string)value).Length == 20)
            {
                Regex expression = new Regex("/([0-9]{4})-([0-9]{2})-([0-9]{2})T([0-9]{2}):([0-9]{2}):([0-9]{2})Z$/");

                if (expression.IsMatch((string)value))
                    return DateTime.Parse("s");
            }

            //
            // Check for a basic primitive.
            //
            if (valueType.IsPrimitive || valueType == typeof(String) || valueType == typeof(IDictionary) ||
                valueType == typeof(Hashtable))
            {
                return value;
            }

            //
            // Check for an enumeration.
            //
            if (valueType.IsEnum)
            {
                return Enum.Parse(valueType, (string)value, false);
            }

            //
            // Check for an array of objects.
            //
            if (valueType.IsArray)
            {
                ICollection collection = (ICollection)value;
                ArrayList newArray = new ArrayList(collection.Count);

                foreach (Object obj in collection)
                {
                    newArray.Add(DecodeObject(obj, valueType.GetElementType()));
                }

                return newArray.ToArray(valueType.GetElementType());
            }

            //
            // Check for a class or struct
            //
            if (valueType.IsClass || valueType.IsValueType)
            {
                IDictionary valueDictionary = (IDictionary)value;
                Object obj;
                Type fieldType;
                FieldInfo field;
                Object fieldValue;

                obj = Activator.CreateInstance(valueType);
                foreach (String key in valueDictionary.Keys)
                {
                    fieldValue = valueDictionary[key];
                    field = valueType.GetField(key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    //
                    // Check for a nullable type.
                    //
                    fieldType = field.FieldType;
                    if (fieldType.GetMethod("GetValueOrDefault", typeArray) != null)
                    {
                        fieldType = Nullable.GetUnderlyingType(fieldType);
                    }

                    field.SetValue(obj, DecodeObject(fieldValue, fieldType));
                }

                return obj;
            }

            //
            // Unknown type.
            //
            throw new InvalidCastException("Unknown parameter type: " + valueType.ToString());
        }
    }
}
