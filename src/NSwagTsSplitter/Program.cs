using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NJsonSchema.CodeGeneration;
using NJsonSchema.CodeGeneration.TypeScript;
using NJsonSchema.Infrastructure;
using NSwag;
using NSwag.CodeGeneration;
using NSwag.CodeGeneration.TypeScript;
using NSwag.CodeGeneration.TypeScript.Models;
using NSwag.Commands;

namespace NSwagTsSplitter
{
    internal static class Program
    {
        private static string _tsDirectory;
        private static string GetParentPath(string path, int parentLevel)
        {
            if (parentLevel > 0)
            {
                return GetParentPath(Directory.GetParent(path).FullName, --parentLevel);
            }
            else
            {
                return path;
            }
        }

        private static async System.Threading.Tasks.Task Main(string[] args)
        {
            DateTime startTime = DateTime.Now;
            DateTime levelTime = DateTime.Now;

            var currentDirectory = await DynamicApis.DirectoryGetCurrentDirectoryAsync();
            var configFiles = await DynamicApis.DirectoryGetFilesAsync(currentDirectory, "*.nswag");
            var document = await NSwagDocument.LoadWithTransformationsAsync(configFiles.FirstOrDefault(), string.Empty);
            var swaggerDocument = await SwaggerDocument.FromUrlAsync(document.SwaggerGenerators.FromSwaggerCommand.Url);
            var json = swaggerDocument.ToJson();
            var settings = document.CodeGenerators.SwaggerToTypeScriptClientCommand.Settings;
            _tsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, document.CodeGenerators.SwaggerToTypeScriptClientCommand.OutputFilePath);

            if (!Directory.Exists(_tsDirectory))
            {
                Directory.CreateDirectory(_tsDirectory);
            }
            else
            {
                DirectoryInfo di = new DirectoryInfo(_tsDirectory);
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }
            }
            var _resolver = new TypeScriptTypeResolver(settings.TypeScriptGeneratorSettings);
            _resolver.RegisterSchemaDefinitions(swaggerDocument.Definitions);

            var _extensionCode = new TypeScriptExtensionCode(
                settings.TypeScriptGeneratorSettings.ExtensionCode,
                (settings.TypeScriptGeneratorSettings.ExtendedClasses ?? new string[] { }).Concat(new[] { settings.ConfigurationClass }).ToArray(),
                new[] { settings.ClientBaseClass });

            var tempClientCode = "Placeholder Code For SwaggerException!";
            var clientClasses = new List<string>();
            settings.ImportRequiredTypes = false;
            settings.GenerateDtoTypes = false;
            var model = new TypeScriptFileTemplateModel(tempClientCode, clientClasses, swaggerDocument, _extensionCode, settings, _resolver);
            var template = settings.CodeGeneratorSettings.TemplateFactory.CreateTemplate("TypeScript", "File", model);
            var utilitiesCode = template.Render();
            utilitiesCode = utilitiesCode.Replace("function ", "export function ").Replace(tempClientCode, "");
            utilitiesCode = utilitiesCode.Replace("\n\n", "\n").Replace("\n\n", "\n").Replace("\n\n", "\n");
            File.WriteAllText(Path.Combine(_tsDirectory, "Utilities.ts"), utilitiesCode + "\n");

            
            var codeGen = new SwaggerToTypeScriptClientGenerator(swaggerDocument, settings);
            var operations = codeGen.GetOperations(_resolver,swaggerDocument);
            foreach (var controllerOperations in operations.GroupBy(o => o.ControllerName))
            {
                var controllerName = controllerOperations.Key;
                var controllerClassName = settings.GenerateControllerName(controllerOperations.Key);
                var clientCode = codeGen.GenerateClientClass(_resolver,controllerName, controllerClassName, controllerOperations.ToList(), ClientGeneratorOutputType.Full);
                var tsPath = Path.Combine(_tsDirectory, controllerClassName + ".ts");
                await File.WriteAllTextAsync(tsPath, clientCode + "\n");
            }

            var generator = new TypeScriptGenerator(swaggerDocument, settings.TypeScriptGeneratorSettings, _resolver);
            var typeDef = TypeScriptGeneratorExtension.GenerateTypes(_resolver, _extensionCode);

            foreach (CodeArtifact codeArtifact in typeDef.Artifacts)
            {
                var tsPath = Path.Combine(_tsDirectory, codeArtifact.TypeName + ".ts");
                await File.WriteAllTextAsync(tsPath, codeArtifact.Code + "\n");
            }
            Console.WriteLine("Generate dto files over, use time：{0}", (DateTime.Now - levelTime).TotalSeconds);
            levelTime = DateTime.Now;
            Console.WriteLine("Generate client files over, use time：{0}", (DateTime.Now - levelTime).TotalSeconds);
        }
    }
}
