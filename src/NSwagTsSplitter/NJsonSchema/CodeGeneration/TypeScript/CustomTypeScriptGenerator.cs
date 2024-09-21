using NSwagTsSplitter.Configuration;

using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using NSwagTsSplitter.Extensions;
using NSwag;
using NSwagTsSplitter.Models;
using System.IO;

// ReSharper disable once CheckNamespace
namespace NJsonSchema.CodeGeneration.TypeScript;

public class CustomTypeScriptGenerator : TypeScriptGenerator
{
    private readonly GeneratorOption _option;
    private readonly OpenApiDocument _document;
    private readonly List<TsModuleModel> _list = new List<TsModuleModel>();

    private readonly FieldInfo _resolverFieldInfo =
        typeof(TypeScriptGenerator).GetField("_resolver", BindingFlags.Instance | BindingFlags.NonPublic);

    private TypeScriptTypeResolver Resolver
    {
        get
        {
            if (_resolverFieldInfo == null)
            {
                return null;
            }

            return _resolverFieldInfo.GetValue(this) as TypeScriptTypeResolver;
        }
    }

    /// <summary>Initializes a new instance of the <see cref="T:NJsonSchema.CodeGeneration.TypeScript.TypeScriptGenerator" /> class.</summary>
    /// <param name="rootObject">The root object to search for all JSON Schemas.</param>
    /// <param name="settings">The generator settings.</param>
    /// <param name="resolver">The resolver.</param>
    /// <param name="option"></param>
    public CustomTypeScriptGenerator(object rootObject, TypeScriptGeneratorSettings settings,
        TypeScriptTypeResolver resolver, GeneratorOption option) : base(rootObject, settings, resolver)
    {
        _document = rootObject as OpenApiDocument;
        _option = option;
    }

    /// <summary>Generates the type.</summary>
    /// <param name="schema">The schema.</param>
    /// <param name="typeNameHint">The fallback type name.</param>
    /// <returns>The code.</returns>
    protected override CodeArtifact GenerateType(JsonSchema schema, string typeNameHint)
    {
        var codeArtifact = base.GenerateType(schema, typeNameHint);
        var referenceTypes = Resolver.GetReferenceTypes(_option, schema, typeNameHint)
            .Where(s => s.Value != codeArtifact.TypeName).ToList();
        var code = codeArtifact.Code;
        code = code.AppendImport(referenceTypes.ToImportCode());
        return new CodeArtifact(codeArtifact.TypeName, codeArtifact.Type, codeArtifact.Language, codeArtifact.Category,
            code);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="schema"></param>
    /// <param name="typeNameHint"></param>
    /// <returns></returns>
    public IEnumerable<KeyValuePair<string, string>> GenerateDtoClass(JsonSchema schema, string typeNameHint)
    {
        var codeArtifacts = GenerateTypes(schema, typeNameHint);
        foreach (var codeArtifact in codeArtifacts)
        {
            var code = base.GenerateFile(new[] { codeArtifact });
            code = code.RemoveBreakLines();
            yield return new KeyValuePair<string, string>(codeArtifact.TypeName, code);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public List<TsModuleModel> GenerateFiles()
    {
        foreach (var definition in _document.Definitions)
        {
            foreach (var keyValuePair in GenerateDtoClass(definition.Value, definition.Key))
            {
                _list.Add(new TsModuleModel
                {
                    ModuleName = keyValuePair.Key,
                    ModuleContent = keyValuePair.Value,
                    ModulePath = Path.Combine(_option.DtoPath, keyValuePair.Key + ".ts"),
                });
            }
        }

        return _list;
    }
}