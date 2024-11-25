using NJsonSchema;
using System.Collections.Generic;
using NJsonSchema.CodeGeneration.TypeScript;
using System.Reflection;
using NSwagTsSplitter.Configuration;

namespace NSwagTsSplitter.Extensions;

public static class TypeScriptTypeResolverExtensions
{
    private static readonly MethodInfo ResolveDictionaryKeyTypeMethodInfo = typeof(TypeScriptTypeResolver).GetMethod(
        "ResolveDictionaryKeyType",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>
    /// 获得参数和属性依赖的类型
    /// </summary>
    /// <param name="resolver"></param>
    /// <param name="schema"></param>
    /// <param name="typeNameHint"></param>
    /// <param name="ignoreProperties"></param>
    /// <returns></returns>
    public static IEnumerable<KeyValuePair<JsonSchema, string>> GetReferenceTypes(this TypeScriptTypeResolver resolver, JsonSchema schema, string typeNameHint, bool ignoreProperties = false)
    {
        // parent types
        foreach (var parent in schema.AllOf)
        {
            var type = resolver.GetOrGenerateTypeName(parent, typeNameHint);
            yield return new KeyValuePair<JsonSchema, string>(parent, type);
        }

        // schema types
        foreach (var parent in schema.OneOf)
        {
            var type = resolver.GetOrGenerateTypeName(parent, typeNameHint);
            yield return new KeyValuePair<JsonSchema, string>(parent, type);
        }
        if (!ignoreProperties)
        {
            // properties
            foreach (var actualProperty in schema.ActualProperties)
            {
                var propertyResult = GetReferenceTypes(resolver, actualProperty.Value, typeNameHint);
                foreach (var result in propertyResult)
                {
                    yield return result;
                }
            }
        }

        if (schema.IsDictionary)
        {
            if (schema.AdditionalPropertiesSchema == null)
            {
                yield break;
            }

            object[] resolveDictionaryKeyTypeMethodParams = { schema.AdditionalPropertiesSchema, "string" };
            var keyType = ResolveDictionaryKeyTypeMethodInfo.Invoke(resolver, resolveDictionaryKeyTypeMethodParams)
                ?.ToString();
            yield return new KeyValuePair<JsonSchema, string>(schema.AdditionalPropertiesSchema, keyType);
            var valueResult = GetReferenceTypes(resolver, schema.AdditionalPropertiesSchema, typeNameHint);
            foreach (var result in valueResult)
            {
                yield return result;
            }
        }
        else if (schema.IsArray || schema.IsTuple)
        {
            if (schema.Item != null)
            {
                var itemType = resolver.Resolve(schema.Item, true, typeNameHint);
                yield return new KeyValuePair<JsonSchema, string>(schema.Item, itemType);
            }
        }
        else if (schema.IsEnumeration)
        {
            var itemType = resolver.GetOrGenerateTypeName(schema, typeNameHint);
            if (!string.IsNullOrWhiteSpace(itemType))
            {
                yield return new KeyValuePair<JsonSchema, string>(schema, itemType);
            }
            else
            {
                if (schema is JsonSchemaProperty schemaProperty)
                {
                    itemType = resolver.GetOrGenerateTypeName(schemaProperty, schemaProperty.Name);
                    yield return new KeyValuePair<JsonSchema, string>(schemaProperty, itemType);
                }
                else if (schema.Item != null)
                {
                    itemType = resolver.Resolve(schema.Item, true, typeNameHint);
                    yield return new KeyValuePair<JsonSchema, string>(schema.Item, itemType);
                }
            }
        }
        else
        {
            var itemType = resolver.Resolve(schema, true, typeNameHint);
            yield return new KeyValuePair<JsonSchema, string>(schema, itemType);
        }
    }

    /// <summary>
    /// 获得引用类型列表
    /// </summary>
    /// <param name="resolver"></param>
    /// <param name="option"></param>
    /// <param name="schema"></param>
    /// <param name="typeNameHint"></param>
    /// <param name="ignoreProperties">忽略属性</param>
    /// <returns>
    /// TKey: type name
    /// TValue: type in module name
    /// </returns>
    public static IEnumerable<KeyValuePair<string, string>> GetReferenceTypes(this TypeScriptTypeResolver resolver,
        GeneratorOption option, JsonSchema schema, string typeNameHint, bool ignoreProperties = false)

    {
        // parent types
        foreach (var parent in schema.AllOf)
        {
            var type = resolver.GetOrGenerateTypeName(parent, typeNameHint);
            foreach (var keyValuePair in option.CheckAndReturnTypeKeyValuePairs(type))
            {
                yield return keyValuePair;
                if (resolver.Settings.GenerateConstructorInterface)
                {
                    var interfaceName = resolver.ResolveConstructorInterfaceName(parent, true, typeNameHint);
                    yield return new KeyValuePair<string, string>(interfaceName, type);
                }
            }
        }

        // schema types
        foreach (var parent in schema.OneOf)
        {
            var type = resolver.GetOrGenerateTypeName(parent, typeNameHint);
            foreach (var keyValuePair in option.CheckAndReturnTypeKeyValuePairs(type))
            {
                yield return keyValuePair;
                if (resolver.Settings.GenerateConstructorInterface)
                {
                    var interfaceName = resolver.ResolveConstructorInterfaceName(parent, true, typeNameHint);
                    yield return new KeyValuePair<string, string>(interfaceName, type);
                }
            }
        }
        if (!ignoreProperties)
        {
            // properties
            foreach (var actualProperty in schema.ActualProperties)
            {
                var propertyResult = GetReferenceTypes(resolver, option, actualProperty.Value, typeNameHint + actualProperty.Key.ToPascalCase());
                foreach (var result in propertyResult)
                {
                    yield return result;
                }
            }
        }

        if (schema.IsDictionary)
        {
            if (schema.AdditionalPropertiesSchema == null)
            {
                yield break;
            }

            object[] resolveDictionaryKeyTypeMethodParams = { schema.AdditionalPropertiesSchema, "string" };
            var keyType = ResolveDictionaryKeyTypeMethodInfo.Invoke(resolver, resolveDictionaryKeyTypeMethodParams)
                ?.ToString();

            foreach (var keyValuePair in option.CheckAndReturnTypeKeyValuePairs(keyType))
            {
                yield return keyValuePair;
                if (resolver.Settings.GenerateConstructorInterface)
                {
                    if (schema.AdditionalPropertiesSchema.DictionaryKey != null)
                    {
                        var interfaceName = resolver.ResolveConstructorInterfaceName(
                            schema.AdditionalPropertiesSchema.DictionaryKey, true, typeNameHint);
                        yield return new KeyValuePair<string, string>(interfaceName, keyType);
                    }
                }
            }

            var valueResult = GetReferenceTypes(resolver, option, schema.AdditionalPropertiesSchema, typeNameHint);

            foreach (var result in valueResult)
            {
                yield return result;
            }
        }
        else if (schema.IsArray || schema.IsTuple)
        {
            if (schema.Item != null)
            {
                var itemType = resolver.Resolve(schema.Item, true, typeNameHint);
                foreach (var keyValuePair in option.CheckAndReturnTypeKeyValuePairs(itemType))
                {
                    yield return keyValuePair;
                    if (resolver.Settings.GenerateConstructorInterface)
                    {
                        var interfaceName = resolver.ResolveConstructorInterfaceName(
                            schema.Item, true, typeNameHint);
                        yield return new KeyValuePair<string, string>(interfaceName, itemType);
                    }
                }
            }
        }
        else if (schema.IsEnumeration)
        {
            var itemType = resolver.GetOrGenerateTypeName(schema, typeNameHint);
            if (!string.IsNullOrWhiteSpace(itemType))
            {
                foreach (var keyValuePair in option.CheckAndReturnTypeKeyValuePairs(itemType))
                {
                    yield return keyValuePair;
                }
            }
            else
            {

                if (schema is JsonSchemaProperty schemaProperty)
                {
                    itemType = resolver.GetOrGenerateTypeName(schemaProperty, schemaProperty.Name);

                    foreach (var keyValuePair in option.CheckAndReturnTypeKeyValuePairs(itemType))
                    {
                        yield return keyValuePair;
                    }
                }
                else if (schema.Item != null)
                {
                    itemType = resolver.Resolve(schema.Item, true, typeNameHint);
                    foreach (var keyValuePair in option.CheckAndReturnTypeKeyValuePairs(itemType))
                    {
                        yield return keyValuePair;
                    }
                }
            }
        }
        else
        {
            var itemType = resolver.Resolve(schema, true, typeNameHint);
            if (itemType == typeNameHint)
            {
                yield break;
            }
            foreach (var keyValuePair in option.CheckAndReturnTypeKeyValuePairs(itemType))
            {
                yield return keyValuePair;
                if (resolver.Settings.GenerateConstructorInterface)
                {
                    var interfaceName = resolver.ResolveConstructorInterfaceName(
                        schema, true, typeNameHint);
                    yield return new KeyValuePair<string, string>(interfaceName, itemType);
                }
            }
        }
    }
}