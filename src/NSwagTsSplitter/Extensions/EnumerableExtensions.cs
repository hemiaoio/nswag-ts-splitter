
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NSwag.Commands;

using NSwagTsSplitter.Models;

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
            dictionary.TryAdd(key, value);
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
        /// <param name="tsModules"></param>
        /// <returns></returns>
        public static IReadOnlyDictionary<string, List<string>> ToModulePair(
            this IEnumerable<KeyValuePair<string, string>> enumerable, IReadOnlyCollection<TsModuleModel> tsModules)
        {
            var dictionary = new Dictionary<string, List<string>>();
            foreach (var keyValuePair in enumerable)
            {
                if (keyValuePair.Value == null)
                {
                    continue;
                }

                var moduleFullName =
                    tsModules.FirstOrDefault(s => s.ModuleName.Equals(keyValuePair.Value))?.ModulePath ??
                    keyValuePair.Value;
                moduleFullName = moduleFullName.Replace("\\", "/");
                var types = dictionary.GetValueOrDefault(moduleFullName) ?? new List<string>();
                if (types.Contains(keyValuePair.Key))
                {
                    continue;
                }

                types.Add(keyValuePair.Key);
                dictionary[moduleFullName] = types;
            }

            return dictionary;
        }

        public static IEnumerable<string> ToImportCode(this IEnumerable<KeyValuePair<string, string>> enumerable,
            string path,
            IReadOnlyCollection<TsModuleModel> referenceModules)
        {
            var modules = enumerable.ToModulePair(referenceModules);
            foreach (var module in modules)
            {
                var relativePath = PathUtilities.MakeRelativePath(module.Key, path);
                if (!relativePath.StartsWith("."))
                {
                    relativePath = "./" + relativePath;
                }

                if (relativePath.EndsWith(".ts"))
                {
                    relativePath = relativePath.Replace(".ts", string.Empty);
                }
                var line =
                    $"import {{ {string.Join(", ", module.Value)} }} from '{relativePath}';";
                yield return line;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="enumerable"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string JoinAsString(this IEnumerable<string> enumerable, string separator)
        {
            if (enumerable == null)
            {
                return string.Empty;
            }

            return string.Join(separator, enumerable);
        }
    }
}