using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NSwagTsSplitter.Generators;

public class CommonCodeGenerator
{
    public static string AppendDisabledLint(string sourceCode)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("/* eslint-disable */");
        stringBuilder.AppendLine("/* tslint:disable */");
        stringBuilder.Append(sourceCode);
        stringBuilder.AppendLine();
        return stringBuilder.ToString();
    }

    public static string AppendImport(string sourceCode, string importCode)
    {
        var sourceCodeLines = sourceCode.Split("\n");
        var stringBuilder = new StringBuilder();
        bool flag = false;
        var hasImport = false;
        foreach (string str in sourceCodeLines)
        {
            if (str.StartsWith("import"))
            {
                hasImport = true;
                flag = true;
            }
            else
            {
                if (flag)
                {
                    stringBuilder.Append(importCode);
                    flag = false;
                }
            }
            stringBuilder.AppendLine(str);
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
}