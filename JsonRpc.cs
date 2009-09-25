
using System;
using System.Collections;
using System.Web;
using System.Web.SessionState;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Jayrock.Json;
using Jayrock.JsonRpc;
using Jayrock.JsonRpc.Web;


namespace Arena.Custom.HDC.WebService
{
    [JsonRpcHelp("This service provides an interface into Arena via a JSON-RPC web query system.")]
    public class JsonRpc : JsonRpcHandler, IRequiresSessionState
    {
        #region Anonymous (non-authenticated) methods.

        [JsonRpcMethod("Version", Idempotent = false)]
        [JsonRpcHelp("Return the version of the API in use by the server.")]
        public IDictionary Version()
        {
            return (IDictionary)JsonConverter.EncodeObject(CoreRpc.Version());
        }


        [JsonRpcMethod("IsClientVersionSupported", Idempotent = false)]
        [JsonRpcHelp("Returns a true/false indication of wether or not the given client version is safe to use.")]
        public bool IsClientVersionSupported(int major, int minor)
        {
            return CoreRpc.IsClientVersionSupported(major, minor);
        }


        [JsonRpcMethod("Login", Idempotent = true)]
        [JsonRpcHelp("Performs a login for the user's session and returns a new authorization key to be used throughout the session.")]
        public string Login(string loginID, string password)
        {
            return CoreRpc.Login(loginID, password);
        }

        #endregion


        #region Methods for working with people records.

        [JsonRpcMethod("FindPeopleByName", Idempotent = true)]
        [JsonRpcHelp("Retrieves an array of all person IDs that match the names.")]
        public int[] FindPeople(string authorization, string firstName, string lastName)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return rpc.FindPeopleByName(firstName, lastName);
        }


        [JsonRpcMethod("FindPeopleByPhone", Idempotent = true)]
        [JsonRpcHelp("Retrieves an array of all person IDs that match the phone number.")]
        public int[] FindPeople(string authorization, string phone)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return rpc.FindPeopleByPhone(phone);
        }


        [JsonRpcMethod("FindPeopleByEmail", Idempotent = true)]
        [JsonRpcHelp("Retrieves an array of all person IDs that match the email address.")]
        public int[] FindPeopleByEmail(string authorization, string email)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return rpc.FindPeopleByEmail(email);
        }


        [JsonRpcMethod("FindPeople", Idempotent = true)]
        [JsonRpcHelp("Retrieves an array of all person IDs that match the search criterea.")]
        public int[] FindPeople(string authorization, IDictionary query)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return rpc.FindPeople((RpcPeopleQuery)JsonConverter.DecodeObject(query, typeof(RpcPeopleQuery)));
        }


        [JsonRpcMethod("GetPersonInformation", Idempotent = true)]
        [JsonRpcHelp("Get the basic person information for the given person ID.")]
        public IDictionary GetPersonInformation(string authorization, int personID)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return (IDictionary)JsonConverter.EncodeObject(rpc.GetPersonInformation(personID));
        }


        [JsonRpcMethod("GetPersonContactInformation", Idempotent = true)]
        [JsonRpcHelp("Get the contact information for the given person ID.")]
        public IDictionary GetPersonContactInformation(string authorization, int personID)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return (IDictionary)JsonConverter.EncodeObject(rpc.GetPersonContactInformation(personID));
        }


        [JsonRpcMethod("GetPersonPeers", Idempotent = true)]
        [JsonRpcHelp("Get all or some of the known peers for the given person.")]
        public IDictionary GetPersonPeers(string authorization, int personID, int start, int count)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return (IDictionary)JsonConverter.EncodeObject(rpc.GetPersonPeers(personID, start, count));
        }
        

        [JsonRpcMethod("GetPersonProfiles", Idempotent = true)]
        [JsonRpcHelp("Get the profile IDs that the member is a part of.")]
        public IDictionary GetPersonProfiles(string authorization, int personID)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return (IDictionary)JsonConverter.EncodeObject(rpc.GetPersonProfiles(personID));
        }


        [JsonRpcMethod("GetPersonNotes", Idempotent = true)]
        [JsonRpcHelp("Retrieve the notes that exist for a given person ID")]
        public Object[] GetPersonNotes(string authorization, int personID)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return (Object[])JsonConverter.EncodeObject(rpc.GetPersonNotes(personID));
        }

        #endregion


        #region Methods for working with profile records.

        [JsonRpcMethod("GetProfileInformation", Idempotent = true)]
        [JsonRpcHelp("Get the information about the given profileID.")]
        public IDictionary GetProfileInformation(string authorization, int profileID)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return (IDictionary)JsonConverter.EncodeObject(rpc.GetProfileInformation(profileID));
        }


        [JsonRpcMethod("GetProfileChildren", Idempotent = true)]
        [JsonRpcHelp("Retrieve the child profile IDs of the given profile.")]
        public int[] GetProfileChildren(string authorization, int profileID)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return rpc.GetProfileChildren(profileID);
        }


        [JsonRpcMethod("GetProfileRoots", Idempotent = true)]
        [JsonRpcHelp("Retrieve all the root profile ID numbers for the given profile type.")]
        public int[] GetProfileRoots(string authorization, int profileType)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return rpc.GetProfileRoots(profileType);
        }


        [JsonRpcMethod("GetProfileMembers", Idempotent = true)]
        [JsonRpcHelp("Get all members of the profile.")]
        public int[] GetProfileMembers(string authorization, int profileID)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return rpc.GetProfileMembers(profileID);
        }


        [JsonRpcMethod("GetProfileOccurrences", Idempotent = true)]
        [JsonRpcHelp("Get all occurrences in this profile.")]
        public int[] GetProfileOccurrences(string authorization, int profileID)
        {
            CoreRpc rpc = new CoreRpc(authorization);

            return rpc.GetProfileOccurrences(profileID);
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
