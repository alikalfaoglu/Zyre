/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System;
using System.Runtime.CompilerServices;

namespace NetMQ.Zyre
{
    /// <summary>
    /// Class for "simple" serialization/deserialization of some common types we need to communicate within Zyre
    /// </summary>
    public static class Serialization
    {
        #region ..Fields..

        private static JsonSerializerSettings jss = new JsonSerializerSettings()
        {
            ContractResolver = new PrivateSetter(),
            TypeNameHandling = TypeNameHandling.Objects,
            TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateParseHandling = DateParseHandling.DateTimeOffset,
            DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
        };

        #endregion

        #region ..Classes..

        private class PrivateSetter : DefaultContractResolver
        {
            private const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            private static bool HasSetter(PropertyInfo property)
            {
                return property.DeclaringType.GetTypeInfo().GetMethods(flags).Any(m => m.Name == "set_" + property.Name);
            }

            private static bool HasGetter(PropertyInfo property)
            {
                return property.DeclaringType.GetTypeInfo().GetMethods(flags).Any(m => m.Name == "get_" + property.Name);
            }

            protected override List<MemberInfo> GetSerializableMembers(Type objectType)
            {
                var result = objectType.GetTypeInfo().GetProperties(flags);
                var fields = objectType.GetTypeInfo().GetFields(flags).Cast<MemberInfo>().Where(f => f.GetCustomAttribute<CompilerGeneratedAttribute>() == null);
                //
                return result.Cast<MemberInfo>().Union(fields).ToList();
            }

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var prop = base.CreateProperty(member, memberSerialization);
                if (!prop.Writable)
                {
                    var property = member as PropertyInfo;
                    if (property != null) prop.Writable = HasSetter(property);
                    else
                    {
                        var field = member as FieldInfo;
                        if (field != null) prop.Writable = true;
                    }
                }
                if (!prop.Readable)
                {
                    var field = member as FieldInfo;
                    if (field != null) prop.Readable = true;
                }
                return prop;
            }
        }

        #endregion

        private static byte[] ToEncodedByteArray<T>(this String source)
            where T : System.Text.Encoding, new()
        {
            return (new T()).GetBytes(source);
        }

        private static String FromEncodedByteArrayToStr<T>(this Byte[] source)
            where T : System.Text.Encoding, new()
        {
            return (new T()).GetString(source, 0, source.Length);
        }


        public static byte[] ToUTF8ByteArray(this String source)
        {
            return ToEncodedByteArray<System.Text.UTF8Encoding>(source);
        }

        public static String FromUTF8ByteArrayToStr(this Byte[] source)
        {
            return FromEncodedByteArrayToStr<System.Text.UTF8Encoding>(source);
        }


        /// <summary>
        /// Serialize into serializedBytes that can be deserialized by other members of this class
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectToSerialize"></param>
        /// <returns>the serialized buffer</returns>
        public static byte[] BinarySerialize<T>(T objectToSerialize)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(objectToSerialize, jss).ToUTF8ByteArray();
        }

        /// <summary>
        /// Return deserialized object from serializedBytes serialized by Serializtion.BinarySerialize()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializedBytes">buffer serialized by Serializtion.BinarySerialize()</param>
        /// <returns>the object of type T</returns>
        public static T BinaryDeserialize<T>(byte[] serializedBytes)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(serializedBytes.FromUTF8ByteArrayToStr(), jss);
        }
    }
}
