using System.Globalization;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace NSwagTsSplitter.Extensions;

public static class StringExtensions
{
    public static bool IsAllUpperCase(this string input)
    {
        return input.All(t => !char.IsLetter(t) || char.IsUpper(t));
    }

    /// <summary>
    /// Converts PascalCase string to camelCase string.
    /// </summary>
    /// <param name="str">String to convert</param>
    /// <param name="useCurrentCulture">set true to use current culture. Otherwise, invariant culture will be used.</param>
    /// <param name="handleAbbreviations">set true to if you want to convert 'XYZ' to 'xyz'.</param>
    /// <returns>camelCase of the string</returns>
    public static string ToCamelCase(this string str, bool useCurrentCulture = false, bool handleAbbreviations = false)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return str;
        }

        if (str.Length == 1)
        {
            return useCurrentCulture ? str.ToLower() : str.ToLowerInvariant();
        }

        if (handleAbbreviations && IsAllUpperCase(str))
        {
            return useCurrentCulture ? str.ToLower() : str.ToLowerInvariant();
        }

        return (useCurrentCulture ? char.ToLower(str[0]) : char.ToLowerInvariant(str[0])) + str.Substring(1);
    }

    /// <summary>
    /// Converts given PascalCase/camelCase string to sentence (by splitting words by space).
    /// Example: "ThisIsSampleSentence" is converted to "This is a sample sentence".
    /// </summary>
    /// <param name="str">String to convert.</param>
    /// <param name="useCurrentCulture">set true to use current culture. Otherwise, invariant culture will be used.</param>
    public static string ToSentenceCase(this string str, bool useCurrentCulture = false)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return str;
        }

        return useCurrentCulture
            ? Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + " " + char.ToLower(m.Value[1]))
            : Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + " " + char.ToLowerInvariant(m.Value[1]));
    }

    /// <summary>
    /// Converts given PascalCase/camelCase string to kebab-case.
    /// </summary>
    /// <param name="str">String to convert.</param>
    /// <param name="useCurrentCulture">set true to use current culture. Otherwise, invariant culture will be used.</param>
    public static string ToKebabCase(this string str, bool useCurrentCulture = false)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return str;
        }

        str = str.ToCamelCase();

        return useCurrentCulture
            ? Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + "-" + char.ToLower(m.Value[1]))
            : Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + "-" + char.ToLowerInvariant(m.Value[1]));
    }

    /// <summary>
    /// Converts given PascalCase/camelCase string to snake case.
    /// Example: "ThisIsSampleSentence" is converted to "this_is_a_sample_sentence".
    /// https://github.com/npgsql/npgsql/blob/dev/src/Npgsql/NameTranslation/NpgsqlSnakeCaseNameTranslator.cs#L51
    /// </summary>
    /// <param name="str">String to convert.</param>
    /// <returns></returns>
    public static string ToSnakeCase(this string str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return str;
        }

        var builder = new StringBuilder(str.Length + Math.Min(2, str.Length / 5));
        var previousCategory = default(UnicodeCategory?);

        for (var currentIndex = 0; currentIndex < str.Length; currentIndex++)
        {
            var currentChar = str[currentIndex];
            if (currentChar == '_')
            {
                builder.Append('_');
                previousCategory = null;
                continue;
            }

            var currentCategory = char.GetUnicodeCategory(currentChar);
            switch (currentCategory)
            {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                    if (previousCategory == UnicodeCategory.SpaceSeparator ||
                        previousCategory == UnicodeCategory.LowercaseLetter ||
                        previousCategory != UnicodeCategory.DecimalDigitNumber &&
                        previousCategory != null &&
                        currentIndex > 0 &&
                        currentIndex + 1 < str.Length &&
                        char.IsLower(str[currentIndex + 1]))
                    {
                        builder.Append('_');
                    }

                    currentChar = char.ToLower(currentChar);
                    break;

                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.DecimalDigitNumber:
                    if (previousCategory == UnicodeCategory.SpaceSeparator)
                    {
                        builder.Append('_');
                    }

                    break;

                default:
                    if (previousCategory != null)
                    {
                        previousCategory = UnicodeCategory.SpaceSeparator;
                    }

                    continue;
            }

            builder.Append(currentChar);
            previousCategory = currentCategory;
        }

        return builder.ToString();
    }

    /// <summary>
    /// Converts camelCase string to PascalCase string.
    /// </summary>
    /// <param name="str">String to convert</param>
    /// <param name="useCurrentCulture">set true to use current culture. Otherwise, invariant culture will be used.</param>
    /// <returns>PascalCase of the string</returns>
    public static string ToPascalCase(this string str, bool useCurrentCulture = false)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return str;
        }

        if (str.Length == 1)
        {
            return useCurrentCulture ? str.ToUpper() : str.ToUpperInvariant();
        }

        return (useCurrentCulture ? char.ToUpper(str[0]) : char.ToUpperInvariant(str[0])) + str.Substring(1);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="source"></param>
    /// <param name="start"></param>
    /// <param name="allowNullable"></param>
    /// <returns></returns>
    public static string EnsureStartsWith(this string source, string start, bool allowNullable = true)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            if (allowNullable)
            {
                return start;
            }

            return null;
        }

        if (source.StartsWith(start))
        {
            return source;
        }

        return start + source;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="source"></param>
    /// <param name="end"></param>
    /// <param name="allowNullable"></param>
    /// <returns></returns>
    public static string EnsureEndsWith(this string source, string end, bool allowNullable = true)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return allowNullable ? end : null;
        }

        if (source.EndsWith(end))
        {
            return source;
        }

        return source + end;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static bool IsJson(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        input = input.Trim();
        if ((!input.StartsWith("{") || !input.EndsWith("}")) && // 对象
            (!input.StartsWith("[") || !input.EndsWith("]")))
        {
            return false; // 数组
        }

        try
        {
            var obj = JToken.Parse(input);
            return true;
        }
        catch (JsonReaderException)
        {
            // 不是JSON格式
            return false;
        }
    }

    /// <summary>
    /// 移除空行
    /// </summary>
    /// <param name="input"></param>
    /// <param name="allowBreakLines">允许空行数量</param>
    /// <returns></returns>
    public static string RemoveBreakLines(this string input, int allowBreakLines = 1)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        input = input.Trim();
        var newCrlf = string.Join("", Enumerable.Repeat("\n\r", allowBreakLines + 1));
        var newLf = string.Join("", Enumerable.Repeat("\n", allowBreakLines + 1));
        var newCr = string.Join("", Enumerable.Repeat("\r", allowBreakLines + 1));
        var oldCrlf = string.Join("", Enumerable.Repeat("\n\r", allowBreakLines + 2));
        var oldLf = string.Join("", Enumerable.Repeat("\n", allowBreakLines + 2));
        var oldCr = string.Join("", Enumerable.Repeat("\r", allowBreakLines + 2));

        input = input.Replace(oldCrlf, newCrlf);
        input = input.Replace(oldLf, newLf);
        input = input.Replace(oldCr, newCr);
        if (input.Contains(oldCrlf) || input.Contains(oldLf) || input.Contains(oldCr))
        {
            return RemoveBreakLines(input, allowBreakLines);
        }

        return input;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="code"></param>
    /// <param name="newLineBehavior"></param>
    /// <returns></returns>
    public static string NormalizeNewLine(this string code, string newLineBehavior)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return code;
        }

        return code.Replace("\r\n", newLineBehavior).Replace("\n", newLineBehavior).Replace("\r", newLineBehavior);
    }

    /// <summary>
    /// 追加import
    /// </summary>
    /// <param name="code"></param>
    /// <param name="importCodeLines"></param>
    /// <param name="newLineBehavior"></param>
    /// <returns></returns>
    public static string AppendImport(this string code, string importCodeLines, string newLineBehavior)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return code;
        }

        code = code.NormalizeNewLine(newLineBehavior);
        var lines = code.Split(newLineBehavior);
        var importLines = importCodeLines.Split(newLineBehavior);
        var stringBuilder = new StringBuilder();
        bool isImported = false;
        foreach (var line in lines)
        {
            if (!isImported && line.StartsWith("export"))
            {
                foreach (var importLine in importLines)
                {
                    stringBuilder.Append(importLine);
                    stringBuilder.Append(newLineBehavior);
                }

                stringBuilder.Append(newLineBehavior);
                isImported = true;
            }

            stringBuilder.Append(line);
            stringBuilder.Append(newLineBehavior);
        }

        return stringBuilder.ToString();
    }
}