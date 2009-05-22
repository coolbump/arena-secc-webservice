
using System;
using System.Collections;
using System.Web;
using System.Web.SessionState;
using System.Reflection;
using Jayrock.Json;
using Jayrock.JsonRpc;
using Jayrock.JsonRpc.Web;


namespace Arena.Custom.HDC.WebService
{
    [JsonRpcHelp("This service provides an interface into Arena via a JSON-RPC web query system.")]
    public class JsonRpc : JsonRpcHandler, IRequiresSessionState
    {
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

        [JsonRpcMethod("FindPeople", Idempotent = true)]
        [JsonRpcHelp("Retrieves an array of all person IDs that match the search criterea.")]
        public int[] FindPeople(IDictionary credentials, IDictionary query)
        {
            CoreRpc rpc = new CoreRpc((RpcCredentials)JsonConverter.DecodeObject(credentials, typeof(RpcCredentials)));

            return rpc.FindPeople((RpcPeopleQuery)JsonConverter.EncodeObject(query));
        }


        [JsonRpcMethod("GetPersonInformation", Idempotent = true)]
        [JsonRpcHelp("Get the basic person information for the given person ID.")]
        public IDictionary GetPersonInformation(IDictionary credentials, int personID)
        {
            CoreRpc rpc = new CoreRpc((RpcCredentials)JsonConverter.DecodeObject(credentials, typeof(RpcCredentials)));
            
            return (IDictionary)JsonConverter.EncodeObject(rpc.GetPersonInformation(personID));
        }


        [JsonRpcMethod("GetPersonContactInformation", Idempotent = true)]
        [JsonRpcHelp("Get the contact information for the given person ID.")]
        public IDictionary GetPersonContactInformation(IDictionary credentials, int personID)
        {
            CoreRpc rpc = new CoreRpc((RpcCredentials)JsonConverter.DecodeObject(credentials, typeof(RpcCredentials)));

            return (IDictionary)JsonConverter.EncodeObject(rpc.GetPersonContactInformation(personID));
        }

        
        [JsonRpcMethod("GetPersonProfiles", Idempotent = true)]
        [JsonRpcHelp("Get the profile IDs that the member is a part of.")]
        public IDictionary GetPersonProfiles(IDictionary credentials, int personID)
        {
            CoreRpc rpc = new CoreRpc((RpcCredentials)JsonConverter.DecodeObject(credentials, typeof(RpcCredentials)));

            return (IDictionary)JsonConverter.EncodeObject(rpc.GetPersonProfiles(personID));
        }
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
