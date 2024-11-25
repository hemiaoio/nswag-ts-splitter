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
    private static readonly List<TsModuleModel> List = new List<TsModuleModel>();

    private readonly FieldInfo _resolverFieldInfo =
        typeof(TypeScriptGenerator).GetField("_resolver", BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly FieldInfo _resolverGeneratedTypeNames =
        typeof(TypeScriptTypeResolver).GetField("_generatedTypeNames", BindingFlags.Instance | BindingFlags.NonPublic);

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

    private readonly Dictionary<JsonSchema, string> _generatedTypeNames = new Dictionary<JsonSchema, string>();

    /// <summary>
    /// 
    /// </summary>
    private Dictionary<JsonSchema, string> ResolverGeneratedTypeNames
    {
        get
        {
            if (Resolver == null)
            {
                return _generatedTypeNames;
            }

            return _resolverGeneratedTypeNames.GetValue(Resolver) as Dictionary<JsonSchema, string>;
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="schema"></param>
    /// <param name="typeNameHint"></param>
    /// <param name="parentPath"></param>
    /// <returns></returns>
    public IEnumerable<TsModuleModel> GenerateDtoClass(JsonSchema schema, string typeNameHint, string parentPath)
    {
        var codeArtifacts = GenerateTypes(schema, typeNameHint);
        foreach (var codeArtifact in codeArtifacts)
        {
            var path = _option.PlainDto ? $"./{codeArtifact.TypeName}.ts" : Path.Combine(parentPath, codeArtifact.TypeName + ".ts");
            var model = List.FirstOrDefault(s => s.ModulePath.Equals(path));
            if (model != null)
            {
                yield return model;
            }

            var generatedSchema = ResolverGeneratedTypeNames.FirstOrDefault(s => s.Value == codeArtifact.TypeName).Key;
            var referenceModules = Resolver.GetReferenceTypes(_option, generatedSchema, codeArtifact.TypeName).ToArray();
            var importCodes = referenceModules.ToImportCode(_option.ServiceFolder, new List<TsModuleModel>());
            var code = base.GenerateFile(new[] { codeArtifact });
            code = code.RemoveBreakLines();
            code = code.AppendImport(importCodes.JoinAsString(_option.NewLineBehavior), _option.NewLineBehavior);
            model = new TsModuleModel
            {
                ModulePath = path,
                ModuleContent = code,
                ModuleName = codeArtifact.TypeName,
                Schema = generatedSchema,
                Artifacts = new List<CodeArtifact>() { codeArtifact }
            };
            yield return model;
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
            var dtos = GenerateDtoClass(definition.Value, definition.Key, _option.DtoPath);
        }

        foreach (var tsModuleModel in List)
        {
            var referenceTypes = Resolver.GetReferenceTypes(_option, tsModuleModel.Schema, tsModuleModel.ModuleName).ToArray();
            tsModuleModel.ModuleContent = tsModuleModel.ModuleContent.AppendImport(
                referenceTypes.ToImportCode(tsModuleModel.ModulePath, List).JoinAsString(_option.NewLineBehavior),
                _option.NewLineBehavior);
            tsModuleModel.ModuleContent = tsModuleModel.ModuleContent.RemoveBreakLines();
        }
        return List;
    }
}