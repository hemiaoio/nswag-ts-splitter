using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Newtonsoft.Json;

using NSwagTsSplitter.Extensions;
using NSwagTsSplitter.Helpers;

using Serilog;

namespace NSwagTsSplitter.Configuration;

public class GeneratorOption
{
    /// <summary>
    /// 配置文件地址
    /// </summary>
    public string ConfigPath => _configPath;

    private string _configPath;

    public string DisabledLint { get; set; } = "/* tslint:disable */";

    public static GeneratorOption FromConfigFile(string configFilePath)
    {
        var fileContent = File.ReadAllText(configFilePath);
        var document = JsonConvert.DeserializeObject<CustomSwagDocument>(fileContent);
        var model = new GeneratorOption();
        if (document.CodeGenerators.OpenApiToTypeScriptClient != null)
        {
            model.PlainDto = document.CodeGenerators.OpenApiToTypeScriptClient.DtoPlain;
            if (!string.IsNullOrWhiteSpace(document.CodeGenerators.OpenApiToTypeScriptClient.ServiceFolder))
            {
                model.ServiceFolder = document.CodeGenerators.OpenApiToTypeScriptClient.ServiceFolder;
            }
            if (!string.IsNullOrWhiteSpace(document.CodeGenerators.OpenApiToTypeScriptClient.DtoFolder))
            {
                model.DtoFolder = document.CodeGenerators.OpenApiToTypeScriptClient.DtoFolder;
            }
            if (!string.IsNullOrWhiteSpace(document.CodeGenerators.OpenApiToTypeScriptClient.UtilitiesFileName))
            {
                model.UtilitiesFileName = document.CodeGenerators.OpenApiToTypeScriptClient.UtilitiesFileName;
            }
            if (document.CodeGenerators.OpenApiToTypeScriptClient.CommonModule != null)
            {
                foreach (var kv in document.CodeGenerators.OpenApiToTypeScriptClient.CommonModule)
                {
                    model.CommonModule.AddOrReplace(kv.Key, kv.Value);
                }
            }

            if (document.CodeGenerators.OpenApiToTypeScriptClient.UtilitiesModule != null)
            {
                model.UtilitiesModule = model.UtilitiesModule
                    .Concat(document.CodeGenerators.OpenApiToTypeScriptClient.UtilitiesModule).Distinct().ToList();
            }
        }

        return model;
    }

    /// <summary>
    /// 将所有DTO文件放在同一文件夹，根目录为 <see cref="DtoFolder"/>
    /// </summary>
    public bool PlainDto { get; set; }

    /// <summary>
    /// 指定DTO输出到文件夹
    /// </summary>
    public string DtoFolder { get; set; } = "";

    /// <summary>
    /// DTO文件全路径
    /// </summary>
    public string DtoPath => string.IsNullOrWhiteSpace(DtoFolder) ? "./" : DtoFolder.EnsureStartsWith("./");

    /// <summary>
    /// 指定Service文件的根目录
    /// </summary>
    public string ServiceFolder { get; set; } = "";

    /// <summary>
    /// 通用Module，在这个列表中的Module，会被放在指定Common文件夹中
    /// </summary>
    public Dictionary<string, string> CommonModule { get; set; } = new();

    /// <summary>
    /// TS 的基础类型
    /// </summary>
    public List<string> TsBaseTypes { get; private set; } = new()
    {
        "string", "number", "Date", "undefined", "any", "boolean", "void"
    };

    /// <summary>
    /// 全局模块，（不做导入）
    /// </summary>
    public List<string> GlobalModules { get; set; } = new List<string>()
    {
        "Blob"
    };

    /// <summary>
    /// 指定Utilities文件包含的Module
    /// </summary>
    public List<string> UtilitiesModule { get; set; } = new()
    {
        "throwException",
        "FileParameter",
        "FileResponse",
        "SwaggerException",
        "ServiceBase",
        "blobToText",
        "jsonParse",
        "createInstance",
        "parseDateOnly"
    };

    /// <summary>
    /// 通用模块名称
    /// </summary>
    public string UtilitiesFileName { get; set; } = "Utilities";

    private string _outputBaseDirectory;

    /// <summary>
    /// 最终输出目录
    /// </summary>
    public string OutputBaseDirectory
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_outputBaseDirectory))
            {
                return _outputBaseDirectory;
            }

            var configPath = Path.GetFullPath(_configPath);
            if (Directory.Exists(configPath))
            {
                _outputBaseDirectory = configPath;
            }
            else
            {
                var parent = Directory.GetParent(configPath);
                _outputBaseDirectory = parent == null ? Directory.GetCurrentDirectory() : parent.FullName;
            }

            return _outputBaseDirectory;
        }
        set => _outputBaseDirectory = value;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="arg"></param>
    /// <param name="currentDirectory"></param>
    public void SetConfigPath(string arg, string currentDirectory)
    {
        var tmpPath = arg;
        tmpPath = tmpPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(tmpPath))
        {
            _configPath = tmpPath;
            return;
        }

        if (arg.StartsWith("." + Path.DirectorySeparatorChar))
        {
            tmpPath = Path.GetFullPath(Path.Combine(currentDirectory, arg));
            _configPath = tmpPath;
            return;
        }

        tmpPath = Path.GetFullPath(Path.Combine(currentDirectory, arg));
        _configPath = tmpPath;
        var files = new List<string>();
        if (string.IsNullOrWhiteSpace(_configPath))
        {
            files = Directory.GetFiles(currentDirectory, "*.nswag").ToList();
            if (files.Any())
            {
                _configPath = files[0];
            }
        }


        if (string.IsNullOrWhiteSpace(_configPath))
        {
            currentDirectory = AppContext.BaseDirectory;
            Log.Information("CurrentDirectory By [AppContext.BaseDirectory]:{0}", currentDirectory);
            files = Directory.GetFiles(currentDirectory, "*.nswag").ToList();
            if (files.Any())
            {
                _configPath = files[0];
            }
        }

        if (string.IsNullOrWhiteSpace(_configPath))
        {
            currentDirectory = Path.GetDirectoryName(Environment.ProcessPath);
            Log.Information(
                "CurrentDirectory By [Path.GetDirectoryName(Environment.ProcessPath)]:{0}",
                currentDirectory);
            files = Directory.GetFiles(currentDirectory, "*.nswag").ToList();
            if (files.Any())
            {
                _configPath = files[0];
            }
        }

        if (string.IsNullOrWhiteSpace(_configPath))
        {
            currentDirectory = Directory.GetCurrentDirectory();
            Log.Information("CurrentDirectory By [Directory.GetCurrentDirectory()]:{0}", currentDirectory);
            files = Directory.GetFiles(currentDirectory, "*.nswag").ToList();
            if (files.Any())
            {
                _configPath = files[0];
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="settingsExcludedParameterNames"></param>
    public void AddTsBaseTypes(ICollection<string> settingsExcludedParameterNames)
    {
        if (settingsExcludedParameterNames == null || !settingsExcludedParameterNames.Any())
        {
            return;
        }
        TsBaseTypes.AddRange(settingsExcludedParameterNames);
        TsBaseTypes = TsBaseTypes.Distinct().ToList();
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="moduleName"></param>
    /// <returns></returns>
    public bool IsCommonModule(string moduleName)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
        {
            return false;
        }
        return CommonModule.ContainsKey(moduleName.Trim());
    }

    /// <summary>
    /// 是否是工具类Module
    /// </summary>
    /// <param name="moduleName"></param>
    /// <returns></returns>
    public bool IsUtilitiesModule(string moduleName)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
        {
            return false;
        }
        return UtilitiesModule.Contains(moduleName.Trim());
    }

    /// <summary>
    /// 是否是TS基本类型
    /// </summary>
    /// <param name="inputType"></param>
    /// <returns></returns>
    public bool IsBaseType(string inputType)
    {
        if (string.IsNullOrWhiteSpace(inputType))
        {
            return false;
        }

        return TsBaseTypes.Any(s => s.Replace(" ", "").Equals(inputType.Replace(" ", "")));
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="inputType"></param>
    /// <returns></returns>
    public bool IsKeyValueType(string inputType)
    {
        if (string.IsNullOrWhiteSpace(inputType))
        {
            return false;
        }

        inputType = inputType.Replace(" ", "");
        if (inputType.StartsWith("{[key:"))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 是否是不需要导入的类型
    /// </summary>
    /// <param name="inputType"></param>
    /// <returns></returns>
    public bool IsExcludeType(string inputType)
    {
        return IsBaseType(inputType) || IsGlobalModule(inputType);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="moduleName"></param>
    /// <returns></returns>
    public bool IsGlobalModule(string moduleName)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
        {
            return false;
        }

        return GlobalModules.Contains(moduleName.Trim());
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public IEnumerable<KeyValuePair<string, string>> CheckAndReturnTypeKeyValuePairs(string type)
    {
        if (type.StartsWith("Anonymous"))
        {
            yield break;
        }

        if (IsExcludeType(type))
        {
            yield break;
        }

        if (IsKeyValueType(type))
        {
            var kvTypes = GetKeyValueType(type);
            foreach (var keyValuePair in kvTypes)
            {
                yield return keyValuePair;
            }
        }
        else if (IsUtilitiesModule(type))
        {
            yield return new KeyValuePair<string, string>(type, UtilitiesFileName);
        }
        else
        {
            yield return new KeyValuePair<string, string>(type, type);
        }
    }

    public IEnumerable<KeyValuePair<string, string>> GetKeyValueType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            yield break;
        }
        type = type.Replace(" ", "");
        var keyType = type.Substring(type.IndexOf("[key:", StringComparison.Ordinal) + 5, type.IndexOf("]", StringComparison.Ordinal) - type.IndexOf("[key:", StringComparison.Ordinal) - 5);
        var keyTypePairs = CheckAndReturnTypeKeyValuePairs(keyType);
        foreach (var keyValuePair in keyTypePairs)
        {
            yield return keyValuePair;
        }

        var valueType = type.Substring(type.IndexOf("]:", StringComparison.Ordinal) + 2, type.IndexOf(";}", StringComparison.Ordinal) - type.IndexOf("]:", StringComparison.Ordinal) - 2);
        var valueTypePairs = CheckAndReturnTypeKeyValuePairs(valueType);
        foreach (var keyValuePair in valueTypePairs)
        {
            yield return keyValuePair;
        }
    }

    public string NewLineBehavior { get; set; }
}