using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace BizSrt.Foundation.StringEnum
{
    public class StringValueAttribute : Attribute
    {
        private string value;

        public StringValueAttribute(string value)
        {
            this.value = value;
        }

        public StringValueAttribute(Type resourceType, string resourceName)
        {
            if (!(resourceType == null || String.IsNullOrWhiteSpace(resourceName)))
            {
                PropertyInfo property = resourceType.GetProperty(resourceName, BindingFlags.Public | BindingFlags.Static);
                if (property != null && property.PropertyType == typeof(string))
                    this.value = (string)property.GetValue(null, null);
                else
                    throw new InvalidOperationException(String.Format("Resource {0} does not have property {1}", resourceType.ToString(), resourceName));
            }
            else
                throw new ArgumentNullException();
        }

        public string Value
        {
            get { return this.value; }
        }
    }

    public static partial class Extensions
    {
        private static ConcurrentDictionary<Enum, StringValueAttribute> _cache = new ConcurrentDictionary<Enum, StringValueAttribute>();

        public static string GetStringValue(this Enum value)
        {
            return value.GetStringValue(value.ToString());
        }

        public static string GetStringValue(this Enum value, string defaultValue)
        {
            StringValueAttribute output;
            Type type = value.GetType();

            if (!_cache.TryGetValue(value, out output))
            {
                FieldInfo fi = type.GetField(value.ToString());
                StringValueAttribute[] attrs =
                   fi.GetCustomAttributes(typeof(StringValueAttribute),
                                           false) as StringValueAttribute[];

                if (attrs != null && attrs.Length > 0)
                {
                    output = attrs[0];
                    if (!_cache.ContainsKey(value))
                        _cache.TryAdd(value, output);
                }
                else
                    return defaultValue;
            }

            return output.Value;
        }

        public static object[] GetValues(this Type type)
        {
            return (from fieldInfo in type.GetFields(BindingFlags.Public | BindingFlags.Static)
                    where fieldInfo.IsLiteral
                    select fieldInfo.GetValue(type)).ToArray();
        }
    }
}
