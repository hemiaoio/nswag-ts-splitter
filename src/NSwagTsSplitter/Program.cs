using System;
using System.Collections.Generic;
using System.Diagnostics;
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


            var configFiles = GetNSwagPath(args);
            if (!configFiles.Any())
            {
                throw new FileNotFoundException("Please specify *.nswag file.");
            }

            DateTime levelTime = DateTime.Now;
            var nswagDocumentHelper = new NSWagDocumentHelper();
            var swaggerDocumentHelper = new SwaggerDocumentHelper();
            var configFile = configFiles.First();
            var configFilePath = Path.GetFullPath(configFile);
            // load config
            var nSwagDocument = await nswagDocumentHelper.LoadDocumentFromFileAsync(configFile);
            Console.WriteLine("NSwag config file loaded, use time：{0}ms", (DateTime.Now - levelTime).TotalSeconds);
            var outputDirectory = IOHelper.CreateOrUpdatePath(configFilePath, nSwagDocument.CodeGenerators
                .OpenApiToTypeScriptClientCommand.OutputFilePath);
            IOHelper.CreateOrUpdatePath(configFilePath, outputDirectory);
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
            var tsPath = Path.Combine(outputDirectory, "Utilities.ts");
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
            foreach (var @class in selfTypeScriptGenerator.GenerateClientClasses())
            {
                tsPath = Path.Combine(outputDirectory, @class.Key + ".ts");
                IOHelper.Delete(tsPath);
                await File.WriteAllTextAsync(tsPath, @class.Value + "\n", Encoding.UTF8);
            }

            Console.WriteLine("Generate client files over, use time：{0}", (DateTime.Now - levelTime).TotalSeconds);
        }


        private static string[] GetNSwagPath(string[] args)
        {
            var files = new List<string>();

            Queue<string> queue = new Queue<string>(args);
            while (queue.Any())
            {
                var arg = queue.Dequeue();
                if (arg.StartsWith("-"))
                {
                    if (arg.Equals("-c", StringComparison.OrdinalIgnoreCase))
                    {
                        while (true)
                        {
                            if (queue.Any())
                            {
                                break;
                            }

                            arg = queue.Dequeue();
                            if (arg.StartsWith("-"))
                            {
                                break;
                            }

                            var tmpPath = arg;
                            if (Path.IsPathRooted(tmpPath))
                            {
                                files.Add(tmpPath);
                                continue;
                            }

                            if (arg.StartsWith('.'))
                            {
                                tmpPath = Path.Combine(Directory.GetCurrentDirectory(), arg);
                                files.Add(tmpPath);
                                continue;
                            }

                            tmpPath = Path.Combine(Directory.GetCurrentDirectory(), arg);
                            files.Add(tmpPath);
                        }
                    }
                }
            }

            if (files.Any())
            {
                return files.ToArray();
            }

            var currentDirectory = DynamicApis.DirectoryGetCurrentDirectory();
            Console.WriteLine(currentDirectory);
            files = DynamicApis.DirectoryGetFiles(currentDirectory, "*.nswag").ToList();
            if (files.Any())
            {
                return files.ToArray();
            }

            currentDirectory = AppContext.BaseDirectory;
            Console.WriteLine(currentDirectory);
            files = DynamicApis.DirectoryGetFiles(currentDirectory, "*.nswag").ToList();
            if (files.Any())
            {
                return files.ToArray();
            }

            currentDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            Console.WriteLine(currentDirectory);
            files = DynamicApis.DirectoryGetFiles(currentDirectory, "*.nswag").ToList();
            if (files.Any())
            {
                return files.ToArray();
            }

            return files.ToArray();
        }
    }
}