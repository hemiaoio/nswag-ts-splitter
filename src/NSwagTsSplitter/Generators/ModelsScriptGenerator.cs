using System;
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
    private readonly TypeScriptTypeResolver _resolver;
    private readonly OpenApiDocument _openApiDocument;
    private string _dtoDirName = "";

    public ModelsScriptGenerator(TypeScriptClientGeneratorSettings settings, OpenApiDocument openApiDocument)
    {
        _resolver = new TypeScriptTypeResolver(settings.TypeScriptGeneratorSettings);
        _openApiDocument = openApiDocument;
    }

    public string DirName => _dtoDirName;
    public void SetDirName(string dirName) => _dtoDirName = dirName;


    public async Task GenerateDtoFilesAsync(string outputDirectory)
    {
        foreach (var dtoClass in GenerateDtoClasses())
        {
            string path = Path.Combine(outputDirectory, _dtoDirName, dtoClass.Key + ".ts");
            IoHelper.Delete(path);
            await File.WriteAllTextAsync(path, dtoClass.Value, Encoding.UTF8);
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
            var model = new ClassTemplateModel(typeName, typeNameHint, _resolver.Settings, _resolver, schema,
                schema);
            Dictionary<string, List<string>> typeNames = new Dictionary<string, List<string>>();
            List<string> enumNames = new List<string>();
            List<string> nswagTypes = new List<string>();
            StringBuilder builder = new StringBuilder();
            foreach (var parent in schema.AllOf)
            {
                var type = _resolver.GetOrGenerateTypeName(parent, string.Empty);
                var list = new List<string>();
                if (_resolver.Settings.GenerateConstructorInterface)
                {
                    list.Add(_resolver.ResolveConstructorInterfaceName(parent, true, string.Empty));
                }
                list.Add(type);
                typeNames.AddIfNot(type, list);
            }

            foreach (var actualProperty in schema.ActualProperties)
            {
                if (actualProperty.Value.IsEnumeration && actualProperty.Value.Reference == null)
                {
                    appendCode = GenerateDtoClass(actualProperty.Value, actualProperty.Key, out _);
                }

                var property = new PropertyModel(model, actualProperty.Value, actualProperty.Key, _resolver,
                    _resolver.Settings);

                var propertyType = property.Type.IndexOf("[", StringComparison.Ordinal) > 0
                    ? property.Type.Replace("[]", "")
                    : property.Type;
                if (!Constant.TsBaseType.Contains(propertyType) && !property.IsDictionary &&
                    !actualProperty.Value.IsEnumeration)
                {
                    typeNames.AddIfNot(propertyType, new List<string> { propertyType });
                }

                if (Constant.UtilitiesModules.Contains(propertyType))
                {
                    nswagTypes.Add(propertyType);
                }

                var propertyDictionaryItemType =
                    property.DictionaryItemType.IndexOf("[", StringComparison.Ordinal) > 0
                        ? property.DictionaryItemType.Replace("[]", "")
                        : property.DictionaryItemType;
                if (!Constant.TsBaseType.Contains(propertyDictionaryItemType))
                {
                    typeNames.AddIfNot(propertyDictionaryItemType, new List<string>() { propertyDictionaryItemType });
                }

                if (Constant.UtilitiesModules.Contains(propertyDictionaryItemType))
                {
                    nswagTypes.Add(propertyDictionaryItemType);
                }

                var propertyArrayItemType = property.ArrayItemType.IndexOf("[", StringComparison.Ordinal) > 0
                    ? property.ArrayItemType.Replace("[]", "")
                    : property.ArrayItemType;
                if (!Constant.TsBaseType.Contains(propertyArrayItemType))
                {
                    typeNames.AddIfNot(propertyArrayItemType, new List<string>() { propertyArrayItemType });
                }

                if (Constant.UtilitiesModules.Contains(propertyArrayItemType))
                {
                    nswagTypes.Add(propertyArrayItemType);
                }
            }

            typeNames.Where(c => !nswagTypes.Contains(c.Key)).Where(c => c.Key != typeName).ToList()
                .ForEach(c => builder.AppendLine($"import {{ {string.Join(",", c.Value)} }} from './{c.Key}';"));
            enumNames.Distinct().Where(c => !nswagTypes.Contains(c)).Where(c => c != typeName).ToList()
                .ForEach(c => builder.AppendLine($"import {{ {c} }} from './{c}';"));
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
}