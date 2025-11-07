using NSwag.CodeGeneration.TypeScript;
using NSwag;
using NJsonSchema.CodeGeneration.TypeScript;
using System.Collections.Generic;
using System.IO;
using NJsonSchema;
using NJsonSchema.CodeGeneration;
using NSwag.CodeGeneration.TypeScript.Models;
using NSwagTsSplitter.Configuration;
using NSwagTsSplitter.Extensions;
using NSwagTsSplitter.Models;

namespace NSwagTsSplitter.Generators;

public class UtilitiesGenerator
{
    private readonly TypeScriptClientGeneratorSettings _clientGeneratorSettings;
    private readonly TypeScriptGenerator _typeScriptGenerator;
    private readonly TypeScriptTypeResolver _resolver;
    private readonly TypeScriptExtensionCode _extensionCode;
    private readonly OpenApiDocument _openApiDocument;
    private readonly List<TsModuleModel> _list = new List<TsModuleModel>();
    private readonly GeneratorOption _generatorOption;

    public UtilitiesGenerator(TypeScriptClientGeneratorSettings clientGeneratorSettings,
        OpenApiDocument openApiDocument, GeneratorOption generatorOption, TypeScriptTypeResolver resolver)
    {
        _clientGeneratorSettings = clientGeneratorSettings;
        _resolver = resolver;
        _extensionCode = new TypeScriptExtensionCode(clientGeneratorSettings.TypeScriptGeneratorSettings.ExtensionCode,
            clientGeneratorSettings.TypeScriptGeneratorSettings.ExtendedClasses);
        _typeScriptGenerator =
            new TypeScriptGenerator(null, _clientGeneratorSettings.TypeScriptGeneratorSettings, _resolver);
        _openApiDocument = openApiDocument;
        _generatorOption = generatorOption;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public IEnumerable<TsModuleModel> GenerateUtilities()
    {
        ////var tempClientCode = "Placeholder Code For SwaggerException!";
        var tempClientCode = new List<CodeArtifact>();
        var model = new TypeScriptFileTemplateModel(tempClientCode, new List<CodeArtifact>(), _openApiDocument,
            _extensionCode, _clientGeneratorSettings, _resolver);
        var template =
            _clientGeneratorSettings.CodeGeneratorSettings.TemplateFactory.CreateTemplate("TypeScript", "File.Utilities",
                model);
        var utilitiesCode = template.Render();

        var dtoGlobal = _typeScriptGenerator.GenerateFile(new JsonSchema(), "Util").Split('\n');
        for (int i = 0; i < dtoGlobal.Length; i++)
        {
            if (i < 13)
            {
                continue;
            }

            utilitiesCode += "\n" + dtoGlobal[i];
        }

        utilitiesCode = utilitiesCode.Replace("function ", "export function ")
            .Replace("Placeholder Code For SwaggerException!", "");
        utilitiesCode = utilitiesCode.RemoveBreakLines();
        var codeBaseModule = new TsModuleModel()
        {
            Artifacts = tempClientCode,
            ModuleContent = utilitiesCode,
            ModuleName = _generatorOption.UtilitiesFileName,
            ModulePath = _generatorOption.UtilitiesFileName.EnsureStartsWith("./").EnsureEndsWith(".ts")
        };
        var list = new List<TsModuleModel>()
        {
            codeBaseModule,
            new TsModuleModel()
            {
                Artifacts = new List<CodeArtifact>(),
                ModuleContent = "",
                ModuleName = _clientGeneratorSettings.ClientBaseClass,
                ModulePath = codeBaseModule.ModulePath
            },

        };
        if (_clientGeneratorSettings.Template == TypeScriptTemplate.Axios)
        {
            list.Add(new TsModuleModel()
            {
                Artifacts = new List<CodeArtifact>(),
                ModuleContent = "",
                ModuleName = "isAxiosError",
                ModulePath = codeBaseModule.ModulePath
            });
            list.Add(new TsModuleModel()
            {
                Artifacts = new List<CodeArtifact>(),
                ModuleContent = "",
                ModuleName = "throwException",
                ModulePath = codeBaseModule.ModulePath
            });
        }
        return list;
    }

    public List<TsModuleModel> Generate()
    {
        var modules = GenerateUtilities();
        _list.AddRange(modules);
        return _list;
    }
}