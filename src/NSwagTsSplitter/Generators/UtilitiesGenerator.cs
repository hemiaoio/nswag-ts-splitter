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

public class UtilitiesGenerator : IGenerator
{
    private readonly TypeScriptClientGeneratorSettings _clientGeneratorSettings;
    private readonly TypeScriptGenerator _typeScriptGenerator;
    private readonly TypeScriptTypeResolver _resolver;
    private readonly TypeScriptExtensionCode _extensionCode;
    private readonly OpenApiDocument _openApiDocument;
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
    public string GenerateUtilities()
    {
        ////var tempClientCode = "Placeholder Code For SwaggerException!";
        var tempClientCode = new List<CodeArtifact>();
        tempClientCode.Add(new CodeArtifact("tsException", CodeArtifactType.Undefined,
            CodeArtifactLanguage.TypeScript, CodeArtifactCategory.Undefined,
            "Placeholder Code For SwaggerException!"));
        tempClientCode.Add(new CodeArtifact("clientBaseClass", CodeArtifactType.Class,
            CodeArtifactLanguage.TypeScript, CodeArtifactCategory.Utility,
            $@"export class {_clientGeneratorSettings.ClientBaseClass} {{
    public getBaseUrl(defaultUrl: string) {{
        return defaultUrl  || '';
    }}
}}"));
        var model = new TypeScriptFileTemplateModel(tempClientCode, new List<CodeArtifact>(), _openApiDocument,
            _extensionCode, _clientGeneratorSettings, _resolver);
        var template =
            _clientGeneratorSettings.CodeGeneratorSettings.TemplateFactory.CreateTemplate("TypeScript", "File",
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
        return utilitiesCode;
    }


    public List<TsModuleModel> Generate()
    {
        var content = GenerateUtilities();
        var path = Path.Combine(_generatorOption.OutputBaseDirectory, _generatorOption.UtilitiesFileName + ".ts");
        var result = new List<TsModuleModel>();
        result.Add(new TsModuleModel()
        {
            ModuleContent = content,
            ModulePath = path,
            ModuleName = _generatorOption.UtilitiesFileName
        });
        return result;
    }
}