using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using NSwagTsSplitter.Contants;
using NSwagTsSplitter.Generators;
using NSwagTsSplitter.Helpers;

using Serilog;
namespace NSwagTsSplitter
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            // resolve the settings file

            var configFiles = ConfigHelper.GetNSwagPath(args);
            if (!configFiles.Any() || !configFiles.All(File.Exists))
            {
                throw new FileNotFoundException("Please specify *.nswag file.");
            }
            foreach (var file in configFiles)
            {
                Log.Information("Read config files:[{0}]", file);
            }


            Stopwatch stopwatch = Stopwatch.StartNew();
            var configFile = configFiles.First();
            var configFilePath = Path.GetFullPath(configFile);
            Log.Information("Use config file:[{0}]", configFilePath);
            var nSwagDocument = await NSWagDocumentHelper.LoadDocumentFromString(configFilePath);
            stopwatch.Stop();
            Log.Information("NSwag config file loaded, use time:{0}ms", stopwatch.Elapsed.TotalMilliseconds);
            var outputDirectory = IoHelper.ReadOutputPath(nSwagDocument, configFilePath);
            Log.Information("Output directory is :[{0}]", outputDirectory);
            stopwatch.Restart();
            // fetch swagger
            var swaggerDocument =
                await SwaggerDocumentHelper.FromUrlAsync(nSwagDocument.SwaggerGenerators.FromDocumentCommand.Url);
            stopwatch.Stop();
            Log.Information("Swagger content loaded, use time:{0}ms", stopwatch.Elapsed.TotalMilliseconds);
            stopwatch.Restart();
            var settings = nSwagDocument.CodeGenerators.OpenApiToTypeScriptClientCommand.Settings;
            settings.ExcludedParameterNames ??= Array.Empty<string>();
            Constant.TsBaseType.AddRange(settings.ExcludedParameterNames);
            // Utilities
            var utilitiesScriptGenerator = new UtilitiesScriptGenerator(settings, swaggerDocument);
            await utilitiesScriptGenerator.GenerateUtilitiesFilesAsync(outputDirectory);
            stopwatch.Stop();
            Log.Information("Generate Utilities.ts complate, use time:{0}ms",
                stopwatch.Elapsed.TotalMilliseconds);
            stopwatch.Restart();
            // DtoClass
            var modelsScriptGenerator = new ModelsScriptGenerator(settings, swaggerDocument);
            await modelsScriptGenerator.GenerateDtoFilesAsync(outputDirectory);
            stopwatch.Stop();
            Log.Information("Generate dto files over, use time:{0}ms", stopwatch.Elapsed.TotalMilliseconds);
            stopwatch.Restart();
            var clientsScriptGenerator = new ClientsScriptGenerator(settings, swaggerDocument);
            clientsScriptGenerator.SetDtoPath(modelsScriptGenerator.DirName);
            clientsScriptGenerator.SetUtilitiesModuleName(utilitiesScriptGenerator.UtilitiesModuleName);
            await clientsScriptGenerator.GenerateClientClassFilesAsync(outputDirectory);
            stopwatch.Stop();
            Log.Information("Generate client files over, use time:{0}ms", stopwatch.Elapsed.TotalMilliseconds);
        }



    }
}