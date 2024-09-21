
using System;
using System.Collections.Generic;
using System.Linq;

namespace NSwagTsSplitter.Extensions
{
    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable)
            {
                action(item);
            }
        }

        public static void AddIfNot<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, value);
            }
        }

        public static void AddOrReplace<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key] = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static IReadOnlyDictionary<string, List<string>> ToModulePair(
            this IEnumerable<KeyValuePair<string, string>> enumerable)
        {
            var dictionary = new Dictionary<string, List<string>>();
            foreach (var keyValuePair in enumerable)
            {
                var types = dictionary.GetValueOrDefault(keyValuePair.Value) ?? new List<string>();
                if (types.Contains(keyValuePair.Key))
                {
                    continue;
                }
                types.Add(keyValuePair.Key);
                dictionary[keyValuePair.Value] = types;
            }

            return dictionary;
        }

        public static string ToImportCode(this IEnumerable<KeyValuePair<string, string>> enumerable)
        {
            var modules = enumerable.ToModulePair();
            return string.Join(Environment.NewLine,
                modules.Select(kv => $"import {{ {string.Join(", ", kv.Value)} }} from './{kv.Key}';"));
        }
    }
}