using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NJsonSchema.Infrastructure;

namespace NSwagTsSplitter
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            // resolve the settings file

            var currentDirectory = DynamicApis.DirectoryGetCurrentDirectory();
            var configFiles = DynamicApis.DirectoryGetFiles(currentDirectory, "*.nswag");
            if (!configFiles.Any())
            {
                throw new FileNotFoundException("The runtime directory must be include *.nswag file.");
            }

            DateTime levelTime = DateTime.Now;
            var nswagDocumentHelper = new NSWagDocumentHelper();
            var swaggerDocumentHelper = new SwaggerDocumentHelper();
            string tsPath = string.Empty;

            // load config
            var nSwagDocument = await nswagDocumentHelper.LoadDocumentFromFileAsync(configFiles.FirstOrDefault());
            Console.WriteLine("NSwag config file loaded, use time：{0}ms", (DateTime.Now - levelTime).TotalSeconds);
            var outputDirectory = IOHelper.CreateOrUpdatePath(nSwagDocument.CodeGenerators
                .OpenApiToTypeScriptClientCommand.OutputFilePath);
            IOHelper.CreateOrUpdatePath(outputDirectory);
            levelTime = DateTime.Now;

            // fetch swagger
            var swaggerDocument =
                await swaggerDocumentHelper.FromUrlAsync(nSwagDocument.SwaggerGenerators.FromDocumentCommand.Url);
            Console.WriteLine("Swagger content loaded, use time：{0}ms", (DateTime.Now - levelTime).TotalSeconds);
            levelTime = DateTime.Now;

            var selfTypeScriptGenerator = new SelfTypeScriptGenerator(
                nSwagDocument.CodeGenerators.OpenApiToTypeScriptClientCommand.Settings,
                swaggerDocument);

            // Utilities
            var utilitiesCode = selfTypeScriptGenerator.GenerateUtilities();
            tsPath = Path.Combine(outputDirectory, "Utilities.ts");
            IOHelper.Delete(tsPath);
            await File.WriteAllTextAsync(tsPath, utilitiesCode, Encoding.UTF8);
            Console.WriteLine("Generate Utilities.ts complate, use time：{0}ms",
                (DateTime.Now - levelTime).TotalSeconds);
            levelTime = DateTime.Now;
            // DtoClass
            var dtos = selfTypeScriptGenerator.GenerateDtoClasses();
            foreach (var dto in dtos)
            {
                tsPath = Path.Combine(outputDirectory, dto.Key + ".ts");
                IOHelper.Delete(tsPath);
                await File.WriteAllTextAsync(tsPath, dto.Value + "\n", Encoding.UTF8);
            }

            Console.WriteLine("Generate dto files over, use time：{0}", (DateTime.Now - levelTime).TotalSeconds);
            levelTime = DateTime.Now;

            // ClientClass
            var classCodes = selfTypeScriptGenerator.GenerateClientClasses();
            foreach (var @class in classCodes)
            {
                tsPath = Path.Combine(outputDirectory, @class.Key + ".ts");
                IOHelper.Delete(tsPath);
                await File.WriteAllTextAsync(tsPath, @class.Value + "\n", Encoding.UTF8);
            }

            Console.WriteLine("Generate client files over, use time：{0}", (DateTime.Now - levelTime).TotalSeconds);
        }
    }
}