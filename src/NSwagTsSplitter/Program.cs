using System;
using System.Diagnostics;
using System.IO;
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

            var config = ArgumentsHelper.ReadArgs(args);
            if (string.IsNullOrWhiteSpace(config.ConfigPath))
            {
                throw new FileNotFoundException("Please specify *.nswag file.");
            }
            if (!File.Exists(config.ConfigPath))
            {
                throw new FileNotFoundException($"Not found config file from :{config.ConfigPath}");
            }
            Log.Information("Read config files:[{0}]", config.ConfigPath);
            Stopwatch stopwatch = Stopwatch.StartNew();
            var configFilePath = Path.GetFullPath(config.ConfigPath);
            Log.Information("Use config file:[{0}]", configFilePath);
            //Log.Information($"{await File.ReadAllTextAsync(configFilePath)}");
            var nSwagDocument = await NsWagDocumentHelper.LoadDocumentFromFileAsync(configFilePath);
            stopwatch.Stop();
            Log.Information("NSwag config file loaded, use time:{0}ms", stopwatch.Elapsed.TotalMilliseconds);
            var outputDirectory = IoHelper.ReadOutputPath(nSwagDocument, configFilePath);
            Log.Information("Output directory is :[{0}]", outputDirectory);
            stopwatch.Restart();
            // fetch swagger
            var swaggerDocument = await OpenApiDocumentHelper.FromUrlAsync(nSwagDocument.SwaggerGenerators.FromDocumentCommand.Url);
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
            modelsScriptGenerator.SetDirName(config.DtoPath);
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
            stopwatch.Restart();
            await CommonCodeGenerator.GenerateIndexAsync(outputDirectory);
            stopwatch.Stop();
            Log.Information("Generate index file over, use time:{0}ms", stopwatch.Elapsed.TotalMilliseconds);

        }



    }
}