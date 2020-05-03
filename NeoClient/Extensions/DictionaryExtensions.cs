using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoClient.Extensions
{
    public static class DictionaryExtensions
    {
        public static void Map<T>(this IReadOnlyDictionary<string, object> dict, T obj)
        {
            var type = typeof(T);
            var properties = type.GetProperties().Where(p => p.CanRead && p.CanWrite && dict.ContainsKey(p.Name));

            foreach (var property in properties)
            {
                var value = Convert.ChangeType(dict[property.Name], property.PropertyType);
                property.SetValue(obj, value);
            }
        }

        public static T Map<T>(this IReadOnlyDictionary<string, object> dict) where T : new()
        {
            var result = new T();

            Map(dict, result);

            return result;
        }
    }
}