using NSwag.CodeGeneration.TypeScript;
using NSwag;
using NJsonSchema.CodeGeneration.TypeScript;
using System.Collections.Generic;
using System.Threading.Tasks;
using NSwagTsSplitter.Helpers;
using System.IO;
using System.Linq;
using System.Text;
using NJsonSchema.CodeGeneration;
using NSwag.CodeGeneration.TypeScript.Models;

namespace NSwagTsSplitter.Generators;

public class UtilitiesScriptGenerator
{
    private string _utilitiesModuleName = "Utilities";
    private readonly TypeScriptClientGeneratorSettings _clientGeneratorSettings;
    private readonly TypeScriptTypeResolver _resolver;
    private readonly TypeScriptExtensionCode _extensionCode;
    private readonly OpenApiDocument _openApiDocument;

    public string UtilitiesModuleName => _utilitiesModuleName;

    public void SetUtilitiesModuleName(string utilitiesModuleName) => _utilitiesModuleName = utilitiesModuleName;

    public UtilitiesScriptGenerator(TypeScriptClientGeneratorSettings clientGeneratorSettings,
        OpenApiDocument openApiDocument)
    {
        _clientGeneratorSettings = clientGeneratorSettings;
        _resolver = new TypeScriptTypeResolver(clientGeneratorSettings.TypeScriptGeneratorSettings);
        _extensionCode = new TypeScriptExtensionCode(clientGeneratorSettings.TypeScriptGeneratorSettings.ExtensionCode,
            clientGeneratorSettings.TypeScriptGeneratorSettings.ExtendedClasses ?? new[]
            {
                clientGeneratorSettings.ConfigurationClass,
                clientGeneratorSettings.ClientBaseClass
            });
        _openApiDocument = openApiDocument;
    }

    public async Task GenerateUtilitiesFilesAsync(string outputDirectory)
    {
        string utilities = GenerateUtilities();
        string path = Path.Combine(outputDirectory, _utilitiesModuleName + ".ts");
        IoHelper.Delete(path);
        await File.WriteAllTextAsync(path, utilities, Encoding.UTF8);
    }

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
                    public getBaseUrl(defaultUrl: string, fetchBaseUrl?:string) {{
                        return '{_openApiDocument.Servers?.FirstOrDefault()?.Url}' || defaultUrl || fetchBaseUrl;
                    }}
                }}"));
        var model = new TypeScriptFileTemplateModel(tempClientCode, new List<CodeArtifact>(), _openApiDocument,
            _extensionCode, _clientGeneratorSettings, _resolver);
        var template =
            _clientGeneratorSettings.CodeGeneratorSettings.TemplateFactory.CreateTemplate("TypeScript", "File",
                model);
        var utilitiesCode = template.Render();
        utilitiesCode = utilitiesCode.Replace("function ", "export function ")
            .Replace("Placeholder Code For SwaggerException!", "");
        utilitiesCode = utilitiesCode.Replace("\n\n", "\n").Replace("\n\n", "\n").Replace("\n\n", "\n");
        return utilitiesCode;
    }
}