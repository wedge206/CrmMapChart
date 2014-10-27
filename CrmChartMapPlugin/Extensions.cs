using System;
using System.IO;
//using System.Xml;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

using Microsoft.Xrm.Sdk;

namespace CrmChartMap.CrmChartMapPlugin
{
    public static class Extensions
    {
        public static bool TryGetValue<T>(this ParameterCollection paramCollection, string key, out T value)
        {
            object valueObj;
            if (paramCollection.TryGetValue(key, out valueObj))
            {
                try
                {
                    value = (T)valueObj;
                    return true;
                }
                catch (InvalidCastException)
                {
                }
            }

            value = default(T);
            return false;
        }

        public static string ToBase64String(this string s)
        {
            if (String.IsNullOrWhiteSpace(s))
                return "";

            byte[] cleanArray = Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding(Encoding.ASCII.EncodingName, new EncoderReplacementFallback(string.Empty), new DecoderExceptionFallback()), Encoding.UTF8.GetBytes(s)); // mini hack to remove invalid character that gets inserted for unknown reason
            return Convert.ToBase64String(cleanArray);
        }

        public static Guid ToGuid(this string s)
        {
            return Guid.Parse(s);
        }

        public static T ParseJSON<T>(this string jsonString)
        {
            if (String.IsNullOrWhiteSpace(jsonString))
                return default(T);

            DataContractJsonSerializer JsonDeserializer = new DataContractJsonSerializer(typeof(T));
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString)))
            {
                return (T)JsonDeserializer.ReadObject(stream);
            }
        }

        public static bool TryParseJSON<T>(this string jsonString, out T obj)
        {
            try
            {
                obj = jsonString.ParseJSON<T>();
                return true;
            }
            catch (InvalidCastException)
            {
                obj = default(T);
                return false;
            }
        }

        public static string ToJSON(this object obj)
        {
            DataContractJsonSerializer JsonSerializer = new DataContractJsonSerializer(obj.GetType());
            using (MemoryStream stream = new MemoryStream())
            {
                JsonSerializer.WriteObject(stream, obj);

                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public static Entity Retrieve(this IOrganizationService service, string entityName, Guid id, bool allColumns)
        {
            return service.Retrieve(entityName, id, new Microsoft.Xrm.Sdk.Query.ColumnSet(allColumns));
        }
    }
}
