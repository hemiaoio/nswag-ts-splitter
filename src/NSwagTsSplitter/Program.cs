using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using NJsonSchema.CodeGeneration.TypeScript;

using NSwag;
using NSwag.CodeGeneration.TypeScript;
using NSwag.Commands;

using NSwagTsSplitter.Generators;
using NSwagTsSplitter.Helpers;
using NSwagTsSplitter.IO;

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
            var nSwagDocument = await NsWagDocumentHelper.LoadDocumentFromFileAsync(config.ConfigPath);
            stopwatch.Stop();
            Log.Information("NSwag config file loaded, use time:{0}ms", stopwatch.Elapsed.TotalMilliseconds);
            var outputDirectory = IoHelper.ReadOutputPath(nSwagDocument, config.ConfigPath);
            config.OutputBaseDirectory = outputDirectory;
            switch (nSwagDocument.CodeGenerators.OpenApiToTypeScriptClientCommand.NewLineBehavior)
            {
                case NewLineBehavior.CRLF:
                    config.NewLineBehavior = "\r\n";
                    break;
                case NewLineBehavior.LF:
                    config.NewLineBehavior = "\n";
                    break;
                case NewLineBehavior.Auto:
                default:
                    config.NewLineBehavior = Environment.NewLine;
                    break;
            }

            Log.Information("Output directory is :[{0}]", outputDirectory);
            stopwatch.Restart();

            OpenApiDocument swaggerDocument =
                await OpenApiDocumentHelper.FromDocumentCommandAsync(
                    nSwagDocument.SwaggerGenerators.FromDocumentCommand);
            Log.Information("Swagger content loaded, use time:{0}ms", stopwatch.Elapsed.TotalMilliseconds);
            stopwatch.Restart();

            var settings = nSwagDocument.CodeGenerators.OpenApiToTypeScriptClientCommand.Settings;
            config.AddTsBaseTypes(settings.ExcludedParameterNames);

            var fileWriter = new FileWriter(config);
            var resolver = new TypeScriptTypeResolver(settings.TypeScriptGeneratorSettings);

            // Utilities
            var utilitiesGenerator = new UtilitiesGenerator(settings, swaggerDocument, config, resolver);
            var utilitiesModules = utilitiesGenerator.Generate();
            fileWriter.AddModules(utilitiesModules);
            stopwatch.Stop();
            Log.Information("Generate Utilities.ts complete, use time:{0}ms",
                stopwatch.Elapsed.TotalMilliseconds);


            // DtoClass
            stopwatch.Restart();
            var modelsScriptGenerator = new CustomTypeScriptGenerator(swaggerDocument,
                settings.TypeScriptGeneratorSettings, resolver, config);
            var modelModules = modelsScriptGenerator.GenerateFiles();
            fileWriter.AddModules(modelModules);
            stopwatch.Stop();
            Log.Information("Generate dto files over, use time:{0}ms", stopwatch.Elapsed.TotalMilliseconds);


            stopwatch.Restart();
            var clientsScriptGenerator =
                new CustomTypeScriptClientGenerator(swaggerDocument, settings, config, resolver);
            var clientModules = clientsScriptGenerator.GenerateFiles();
            fileWriter.AddModules(clientModules);
            stopwatch.Stop();
            Log.Information("Generate client files over, use time:{0}ms", stopwatch.Elapsed.TotalMilliseconds);

            stopwatch.Restart();
            await fileWriter.WriteAsync();
            stopwatch.Stop();
            Log.Information("Generate index file over, use time:{0}ms", stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}