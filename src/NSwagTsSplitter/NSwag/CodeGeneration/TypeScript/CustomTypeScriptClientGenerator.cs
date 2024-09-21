using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using NJsonSchema.CodeGeneration;
using NJsonSchema.CodeGeneration.TypeScript;

using NSwag.CodeGeneration.TypeScript.Models;

using NSwagTsSplitter.Configuration;
using NSwagTsSplitter.Extensions;
using NSwagTsSplitter.Models;

// ReSharper disable once CheckNamespace
namespace NSwag.CodeGeneration.TypeScript;

public class CustomTypeScriptClientGenerator : TypeScriptClientGenerator
{
    private readonly GeneratorOption _option;
    private readonly TypeScriptTypeResolver _resolver;
    private readonly OpenApiDocument _document;
    private readonly CustomTypeScriptGenerator _modelGenerator;
    private readonly List<TsModuleModel> _list = new List<TsModuleModel>();
    /// <summary>Initializes a new instance of the <see cref="T:NSwag.CodeGeneration.TypeScript.TypeScriptClientGenerator" /> class.</summary>
    /// <param name="document">The Swagger document.</param>
    /// <param name="settings">The settings.</param>
    /// <param name="option"></param>
    /// <param name="resolver"></param>
    /// <param name="modelGenerator"></param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="document" /> is <see langword="null" />.</exception>
    public CustomTypeScriptClientGenerator(OpenApiDocument document, TypeScriptClientGeneratorSettings settings,
        GeneratorOption option, TypeScriptTypeResolver resolver, CustomTypeScriptGenerator modelGenerator = null) : this(document, settings, resolver, option, modelGenerator)
    {
        _option = option;
    }

    /// <summary>Initializes a new instance of the <see cref="T:NSwag.CodeGeneration.TypeScript.TypeScriptClientGenerator" /> class.</summary>
    /// <param name="document">The Swagger document.</param>
    /// <param name="settings">The settings.</param>
    /// <param name="resolver">The resolver.</param>
    /// <param name="option"></param>
    /// <param name="modelGenerator"></param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="document" /> is <see langword="null" />.</exception>
    public CustomTypeScriptClientGenerator(OpenApiDocument document, TypeScriptClientGeneratorSettings settings,
        TypeScriptTypeResolver resolver, GeneratorOption option,
        CustomTypeScriptGenerator modelGenerator = null) : base(document, settings, resolver)
    {
        _document = document;
        _option = option;
        _modelGenerator = modelGenerator ??
                          new CustomTypeScriptGenerator(document, settings.TypeScriptGeneratorSettings, resolver,
                              option);
        _resolver = resolver;
    }

    /// <summary>Generates the client class.</summary>
    /// <param name="controllerName">Name of the controller.</param>
    /// <param name="controllerClassName">Name of the controller class.</param>
    /// <param name="operations">The operations.</param>
    /// <returns>The code.</returns>
    protected override IEnumerable<CodeArtifact> GenerateClientTypes(string controllerName, string controllerClassName, IEnumerable<TypeScriptOperationModel> operations)
    {
        var operationArray = operations.ToArray();
        GenerateDtoTypes(controllerClassName, operationArray);
        var referenceTypes = GetReferenceTypes(operationArray).ToList();
        if (!string.IsNullOrWhiteSpace(Settings.ClientBaseClass))
        {
            referenceTypes.Add(new KeyValuePair<string, string>(Settings.ClientBaseClass, _option.UtilitiesFileName));
        }
        var importCodes = referenceTypes.ToImportCode();
        var codeArtifacts = base.GenerateClientTypes(controllerName, controllerClassName, operationArray);
        foreach (var codeArtifact in codeArtifacts)
        {
            var code = codeArtifact.Code;
            code = code.AppendImport(importCodes);
            code = code.RemoveBreakLines();
            yield return new CodeArtifact(codeArtifact.TypeName, codeArtifact.Type, codeArtifact.Language, codeArtifact.Category,
                code);
        }
    }

    /// <summary>
    /// 生成DtoTypes
    /// </summary>
    /// <param name="controllerClassName"></param>
    /// <param name="operations"></param>
    protected virtual void GenerateDtoTypes(string controllerClassName, IEnumerable<TypeScriptOperationModel> operations)
    {
        foreach (var typeScriptOperationModel in operations)
        {
            var operationOpenApiSchemaFieldInfo = typeof(TypeScriptOperationModel).GetField("_operation", BindingFlags.NonPublic | BindingFlags.Instance);
            var operationOpenApiSchema = operationOpenApiSchemaFieldInfo?.GetValue(typeScriptOperationModel) as OpenApiOperation;
            if (operationOpenApiSchema == null)
            {
                continue;
            }
            // parameters types:
            foreach (var parameter in operationOpenApiSchema.Parameters)
            {
                //使用此处生成的Model
                foreach (var keyValuePair in _modelGenerator.GenerateDtoClass(parameter.Schema, controllerClassName))
                {
                    var path = _option.DtoPath;
                    if (!_option.PlainDto)
                    {
                        path = Path.Combine(path, controllerClassName);
                    }
                    _list.Add(new TsModuleModel
                    {
                        ModuleName = keyValuePair.Key,
                        ModuleContent = keyValuePair.Value,
                        ModulePath = Path.Combine(path, keyValuePair.Key + ".ts"),
                    });
                }
            }

            // response types:
            foreach (var responseType in operationOpenApiSchema.Responses)
            {
                var resultType = responseType.Value.Schema;
                if (resultType == null)
                {
                    continue;
                }
                //使用此处生成的Model
                foreach (var keyValuePair in _modelGenerator.GenerateDtoClass(resultType, controllerClassName))
                {
                    var path = _option.DtoPath;
                    if (!_option.PlainDto)
                    {
                        path = Path.Combine(path, controllerClassName);
                    }
                    _list.Add(new TsModuleModel
                    {
                        ModuleName = keyValuePair.Key,
                        ModuleContent = keyValuePair.Value,
                        ModulePath = Path.Combine(path, keyValuePair.Key + ".ts"),
                    });
                }
            }
        }


    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="operation"></param>
    /// <returns></returns>
    public IEnumerable<KeyValuePair<string, string>> GetReferenceTypes(OpenApiOperation operation)
    {
        if (operation.ActualParameters.Any(p => p.ActualTypeSchema.IsBinary) ||
            operation.RequestBody?.Content?.Any(c =>
                c.Value.Schema?.IsBinary == true ||
                c.Value.Schema?.ActualProperties.Any(p => p.Value.IsBinary ||
                                                          p.Value.Item?.IsBinary == true ||
                                                          p.Value.Items.Any(i => i.IsBinary)
                ) == true) == true)
        {
            yield return new KeyValuePair<string, string>("FileParameter", _option.UtilitiesFileName);
        }

        if (operation.ActualResponses.Any(r => r.Value.IsBinary(operation)))
        {
            yield return new KeyValuePair<string, string>("FileResponse", _option.UtilitiesFileName);
        }
        // parameters types:
        foreach (var parameter in operation.Parameters)
        {

            var parameterTypes = _resolver.GetReferenceTypes(_option, parameter.ActualSchema, parameter.Name, true);
            foreach (var parameterType in parameterTypes)
            {
                yield return parameterType;
            }
        }

        // response types:
        foreach (var responseType in operation.Responses)
        {
            var resultType = responseType.Value.Schema;
            if (resultType == null)
            {
                continue;
            }
            var resultTypes = _resolver.GetReferenceTypes(_option, resultType, responseType.Key, true);
            foreach (var result in resultTypes)
            {
                yield return result;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="operations"></param>
    /// <returns>
    /// key: type name
    /// value: module name
    /// </returns>
    public virtual IEnumerable<KeyValuePair<string, string>> GetReferenceTypes(IEnumerable<TypeScriptOperationModel> operations)
    {
        if (Settings.Template == TypeScriptTemplate.Axios)
        {
            yield return new KeyValuePair<string, string>("isAxiosError", _option.UtilitiesFileName);
        }
        yield return new KeyValuePair<string, string>("throwException", _option.UtilitiesFileName);

        foreach (var typeScriptOperationModel in operations)
        {
            var operationOpenApiSchemaFieldInfo = typeof(TypeScriptOperationModel).GetField("_operation", BindingFlags.NonPublic | BindingFlags.Instance);
            var operationOpenApiSchema = operationOpenApiSchemaFieldInfo?.GetValue(typeScriptOperationModel) as OpenApiOperation;
            var referenceTypes = GetReferenceTypes(operationOpenApiSchema);
            foreach (var referenceType in referenceTypes)
            {
                yield return referenceType;
            }
        }
    }

    /// <summary>Generates the file.</summary>
    /// <param name="clientTypes">The client types.</param>
    /// <param name="dtoTypes">The DTO types.</param>
    /// <param name="outputType">Type of the output.</param>
    /// <returns>The code.</returns>
    protected override string GenerateFile(IEnumerable<CodeArtifact> clientTypes, IEnumerable<CodeArtifact> dtoTypes, ClientGeneratorOutputType outputType)
    {
        var extensionCodeFieldInfo = typeof(TypeScriptClientGenerator).GetField("_extensionCode", BindingFlags.NonPublic | BindingFlags.Instance);
        var extensionCode = extensionCodeFieldInfo?.GetValue(this) as TypeScriptExtensionCode;
        var settings = Settings;
        var model = new CustomTypeScriptFileTemplateModel(clientTypes, dtoTypes, _document, extensionCode, settings, _resolver);
        var template = BaseSettings.CodeGeneratorSettings.TemplateFactory.CreateTemplate("TypeScript", "File", model);
        return template.Render();
    }
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public List<TsModuleModel> GenerateFiles()
    {
        var codeArtifacts = GenerateAllClientTypes();
        foreach (var codeArtifact in codeArtifacts)
        {
            var code = GenerateFile(new[] { codeArtifact }, Array.Empty<CodeArtifact>(), ClientGeneratorOutputType.Full);
            _list.Add(new TsModuleModel()
            {
                ModuleContent = code,
                ModuleName = codeArtifact.TypeName,
                ModulePath = Path.Combine(_option.OutputBaseDirectory, codeArtifact.TypeName + ".ts")
            });
        }

        return _list;
    }
}