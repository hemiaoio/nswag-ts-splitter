using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NJsonSchema;
using NJsonSchema.CodeGeneration;
using NJsonSchema.CodeGeneration.TypeScript;
using NJsonSchema.CodeGeneration.TypeScript.Models;

using NSwag;
using NSwag.CodeGeneration.TypeScript;

using NSwagTsSplitter.Contants;
using NSwagTsSplitter.Extensions;
using NSwagTsSplitter.Helpers;

namespace NSwagTsSplitter.Generators;

public class ModelsScriptGenerator
{
    private readonly CustomTypeScriptTypeResolver _resolver;
    private readonly OpenApiDocument _openApiDocument;
    private string _dtoDirName = "";

    public ModelsScriptGenerator(TypeScriptClientGeneratorSettings settings, OpenApiDocument openApiDocument)
    {
        _openApiDocument = openApiDocument;
        _resolver = new CustomTypeScriptTypeResolver(settings.TypeScriptGeneratorSettings);
        _resolver.RegisterSchemaDefinitions(_openApiDocument.Definitions);
    }

    public string DirName => _dtoDirName;
    public void SetDirName(string dirName) => _dtoDirName = dirName;


    public async Task GenerateDtoFilesAsync(string outputDirectory)
    {
        var targetFolder = Path.Combine(outputDirectory, _dtoDirName);
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        var fileNames = new List<string>();
        foreach (var dtoClass in GenerateDtoClasses())
        {
            string path = Path.Combine(targetFolder, dtoClass.Key + ".ts");
            IoHelper.Delete(path);
            await File.WriteAllTextAsync(path, dtoClass.Value, Encoding.UTF8);
            fileNames.Add(dtoClass.Key);
        }
        string indexFile = Path.Combine(targetFolder, "index.ts");
        if (!string.IsNullOrWhiteSpace(_dtoDirName))
        {
            IoHelper.Delete(indexFile);
            await File.AppendAllLinesAsync(indexFile, fileNames.Select(c => $"export * from './{c}';"), Encoding.UTF8);
        }
    }

    public IEnumerable<KeyValuePair<string, string>> GenerateDtoClasses()
    {
        var defs = _openApiDocument.Definitions;
        foreach (var definition in defs)
        {
            var code = GenerateDtoClass(definition.Value, definition.Key, out string className);
            yield return new KeyValuePair<string, string>(className, code);
        }
    }

    /// <summary>
    /// Generate Dto Class
    /// </summary>
    /// <param name="schema"></param>
    /// <param name="typeNameHint"></param>
    /// <param name="className"></param>
    /// <returns></returns>
    public string GenerateDtoClass(JsonSchema schema, string typeNameHint, out string className)
    {
        string appendCode = string.Empty;
        var typeName = _resolver.GetOrGenerateTypeName(schema, typeNameHint);
        if (schema.IsEnumeration)
        {
            var model = new EnumTemplateModel(string.IsNullOrWhiteSpace(typeName) ? typeNameHint : typeName, schema,
                _resolver.Settings);
            var template = _resolver.Settings.TemplateFactory.CreateTemplate("TypeScript", "Enum", model);
            var codeArtifact = new CodeArtifact(string.IsNullOrWhiteSpace(typeName) ? typeNameHint : typeName,
                CodeArtifactType.Enum, CodeArtifactLanguage.TypeScript, CodeArtifactCategory.Undefined, template);
            className = codeArtifact.TypeName;
            return CommonCodeGenerator.AppendDisabledLint(codeArtifact.Code);
        }
        else
        {
            var model = new CustomClassTemplateModel(typeName, typeNameHint, _resolver.Settings, _resolver, schema,
                schema);
            List<string> nswagTypes;
            StringBuilder builder = new StringBuilder();
            var typeNames = GetReferenceTypes(schema, out nswagTypes);
            foreach (var actualProperty in schema.ActualProperties)
            {
                if (actualProperty.Value.IsEnumeration && actualProperty.Value.Reference == null)
                {
                    appendCode = GenerateDtoClass(actualProperty.Value, actualProperty.Key, out _);
                }
                if (_resolver.Settings.HandleReferences)
                {
                    nswagTypes.Add("createInstance");
                    nswagTypes.Add("jsonParse");
                }
            }

            typeNames.Where(c => c.Key != typeName).ToList()
                .ForEach(c => builder.AppendLine($"import {{ {string.Join(",", c.Value)} }} from './{c.Key}';"));
            if (nswagTypes.Any())
            {
                builder.AppendLine(
                    $"import {{ {string.Join(",", nswagTypes.Distinct())} }} from '{(string.IsNullOrWhiteSpace(_dtoDirName) ? "./" : "../")}Utilities';");
            }
            builder.AppendLine();
            var template = _resolver.Settings.TemplateFactory.CreateTemplate("TypeScript", "Class", model);
            className = model.ClassName;
            var code = string.Join("\n", builder.ToString(),
                template.Render(), appendCode);
            return CommonCodeGenerator.AppendDisabledLint(code);
        }
    }




    public Dictionary<string, IEnumerable<string>> GetReferenceTypes(JsonSchema schema, out List<string> nswagTypes)
    {
        var result = new Dictionary<string, IEnumerable<string>>();
        nswagTypes = new List<string>();
        // parent types
        foreach (var parent in schema.AllOf)
        {
            var list = new List<string>();
            var type = _resolver.GetOrGenerateTypeName(parent, string.Empty);
            if (Constant.UtilitiesModules.Contains(type))
            {
                nswagTypes.Add(type);
                continue;
            }
            list.Add(type);
            if (_resolver.Settings.GenerateConstructorInterface)
            {
                var interfaceName = _resolver.ResolveConstructorInterfaceName(parent, true, string.Empty);
                list.Insert(0, interfaceName);
            }
            result.TryAdd(type, list);
        }

        // properties
        foreach (var actualProperty in schema.ActualProperties)
        {
            var propertyResult = GetReferenceTypes(actualProperty.Value, out nswagTypes);
            if (propertyResult != null)
            {
                propertyResult.ForEach(keyValue =>
                {
                    result.TryAdd(keyValue.Key, keyValue.Value);
                });
            }
        }
        if (schema.IsDictionary)
        {
            var keyType = _resolver.ResolveDictionaryKeyType(schema.AdditionalPropertiesSchema, "string", false);
            if (keyType != null && !Constant.TsBaseType.Contains(keyType))
            {
                if (Constant.UtilitiesModules.Contains(keyType))
                {
                    nswagTypes.Add(keyType);
                }
                else
                {
                    var list = new List<string>();
                    list.Add(keyType);
                    if (_resolver.Settings.GenerateConstructorInterface)
                    {
                        var interfaceName = _resolver.ResolveConstructorInterfaceName(
                            schema.AdditionalPropertiesSchema.DictionaryKey, true, string.Empty);
                        list.Insert(0, interfaceName);
                    }
                    result.TryAdd(keyType, list.Distinct());
                }
            }

            var valueResult = GetReferenceTypes(schema.AdditionalPropertiesSchema, out nswagTypes);
            if (valueResult != null)
            {
                valueResult.ForEach(keyValue =>
                {
                    result.TryAdd(keyValue.Key, keyValue.Value);
                });
            }

        }
        else if (schema.IsArray || schema.IsTuple)
        {
            var itemType = _resolver.Resolve(schema.Item, true, "");
            if (itemType == null || Constant.TsBaseType.Contains(itemType))
            {
                return result;
            }

            if (Constant.UtilitiesModules.Contains(itemType))
            {
                nswagTypes.Add(itemType);
                return result;
            }
            var list = new List<string>();
            list.Add(itemType);
            if (_resolver.Settings.GenerateConstructorInterface)
            {
                var interfaceName = _resolver.ResolveConstructorInterfaceName(
                    schema.Item, true, string.Empty);
                list.Insert(0, interfaceName);
            }

            result.TryAdd(itemType, list.Distinct());

        }
        else if (schema.IsEnumeration)
        {
            var itemType = _resolver.Resolve(schema.Item, true, "");
            if (itemType == null || Constant.TsBaseType.Contains(itemType))
            {
                return result;
            }
            if (Constant.UtilitiesModules.Contains(itemType))
            {
                nswagTypes.Add(itemType);
                return result;
            }
            var list = new List<string>();
            list.Add(itemType);
            result.TryAdd(itemType, list.Distinct());
        }
        else
        {
            var itemType = _resolver.Resolve(schema, true, "");
            if (itemType == null || Constant.TsBaseType.Contains(itemType))
            {
                return result;
            }
            if (Constant.UtilitiesModules.Contains(itemType))
            {
                nswagTypes.Add(itemType);
                return result;
            }
            var list = new List<string>();
            list.Add(itemType);
            if (_resolver.Settings.GenerateConstructorInterface)
            {
                var interfaceName = _resolver.ResolveConstructorInterfaceName(
                    schema, true, string.Empty);
                list.Insert(0, interfaceName);
            }

            result.TryAdd(itemType, list.Distinct());
        }
        return result;
    }
}