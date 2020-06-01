using System;
using System.IO;

namespace NSwagTsSplitter
{
    public static class IOHelper
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
            var outputPath = outputDirectory;
            if (outputDirectory.StartsWith('.') || outputPath.IndexOf(":", StringComparison.OrdinalIgnoreCase) < 0)
            {
                outputPath = Path.Combine(configFilePath, outputDirectory);
            }

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            if (Directory.Exists(outputDirectory) && isClear)
            {
                Delete(outputPath);
                Directory.CreateDirectory(outputDirectory);
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
    }
}