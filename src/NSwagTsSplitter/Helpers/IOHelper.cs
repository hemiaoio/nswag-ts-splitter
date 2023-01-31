
using System;
using System.IO;

using NSwag.Commands;
using Serilog;

namespace NSwagTsSplitter.Helpers
{
    public static class IoHelper
    {
        /// <summary>
        /// create or update target directory
        /// </summary>
        /// <param name="configFilePath"></param>
        /// <param name="outputDirectory"></param>
        /// <param name="isClear"></param>
        /// <returns></returns>
        public static string CreateOrUpdatePath(string configFilePath, string outputDirectory, bool isClear = false)
        {
            var outputPath = outputDirectory.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            configFilePath = Path.GetDirectoryName(configFilePath);
            if (outputDirectory.StartsWith('.') || outputPath.IndexOf(":", StringComparison.OrdinalIgnoreCase) < 0)
            {
                outputPath = Path.GetFullPath(Path.Combine(configFilePath, outputPath));
            }

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            if (Directory.Exists(outputPath) && isClear)
            {
                Delete(outputPath);
                Directory.CreateDirectory(outputPath);
            }

            return outputPath;
        }

        public static bool ExistsFile(string configFilePath, string outputDirectory, string fileName)
        {
            var outputPath = outputDirectory;
            if (outputDirectory.StartsWith('.') || outputPath.IndexOf(":", StringComparison.OrdinalIgnoreCase) < 0)
            {
                outputPath = Path.Combine(configFilePath, outputDirectory);
            }

            string filePath = Path.Combine(outputPath, fileName);
            return File.Exists(filePath);
        }

        public static void Delete(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        public static string ReadOutputPath(NSwagDocument nSwagDocument, string configFilePath)
        {
            return CreateOrUpdatePath(configFilePath, nSwagDocument.CodeGenerators
                .OpenApiToTypeScriptClientCommand.OutputFilePath);
        }
    }
}