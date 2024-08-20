using System.IO;
using System.Text;
using System.Threading.Tasks;

using Serilog;

namespace NSwagTsSplitter.Generators;

public class CommonCodeGenerator
{
    public static string AppendDisabledLint(string sourceCode)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(sourceCode);
        stringBuilder.AppendLine();
        AppendDisabledLint(stringBuilder);
        return stringBuilder.ToString();
    }

    private static void AppendDisabledLint(StringBuilder builder)
    {
        builder.Insert(0, "/* tslint:disable */\n");
        builder.Insert(0, "/* eslint-disable */\n");
        builder.Insert(0, "/* eslint-disable unicorn/no-abusive-eslint-disable */\n");
        builder.Insert(0, "/* eslint-disable eslint-comments/no-unlimited-disable */\n");
    }

    public static string AppendImport(string sourceCode, string importCode)
    {
        var sourceCodeLines = sourceCode.Split("\n");
        var stringBuilder = new StringBuilder();
        bool isImportRow = false;
        var hasImport = false;
        foreach (string str in sourceCodeLines)
        {
            if (str.StartsWith("import"))
            {
                hasImport = true;
                isImportRow = true;
            }
            else
            {
                if (isImportRow)
                {
                    stringBuilder.Append(importCode);
                    isImportRow = false;
                }
            }

            stringBuilder.AppendLine(str.Replace("\r", ""));
        }

        if (!hasImport)
        {
            stringBuilder.Insert(0, importCode);
        }
        return stringBuilder.ToString();
    }

    public static async Task<string> GetCommonImportFromUtilitiesAsync(string outputDir, string utilitiesModuleName)
    {
        var utilitiesCodeLines =
            await File.ReadAllLinesAsync(Path.Combine(outputDir, utilitiesModuleName + ".ts"), Encoding.UTF8);
        var builder = new StringBuilder();
        foreach (var line in utilitiesCodeLines)
        {
            if (line.Trim().StartsWith("import"))
            {
                builder.AppendLine(line);
            }
        }
        return builder.ToString();
    }

    public static async Task GenerateIndexAsync(string outputDirectory, bool includeDirectory)
    {
        var indexFilePath = Path.Combine(outputDirectory, "index.ts");
        if (File.Exists(indexFilePath))
        {
            File.Delete(indexFilePath);
        }

        Log.Information("Remove index from [{0}]:", outputDirectory);
        var builder = new StringBuilder();
        if (includeDirectory)
        {
            var dirs = Directory.GetDirectories(outputDirectory);
            foreach (var dir in dirs)
            {
                if (File.Exists(Path.Combine(dir, "index.ts")))
                {
                    builder.AppendLine($"export * from './{Path.GetFileNameWithoutExtension(dir)}'");
                }
            }
        }

        var files = Directory.GetFiles(outputDirectory);
        foreach (var file in files)
        {
            builder.AppendLine($"export * from './{Path.GetFileNameWithoutExtension(file)}'");
        }

        AppendDisabledLint(builder);
        await File.WriteAllTextAsync(indexFilePath, builder.ToString(), Encoding.UTF8);
    }
}