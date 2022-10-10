using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NJsonSchema.Infrastructure;
using Serilog;

namespace NSwagTsSplitter.Helpers;

public class GeneratorConfigModel
{
    /// <summary>
    /// 配置文件地址
    /// </summary>
    public string ConfigPath
    {
        get { return _configPath; }
    }

    private string _configPath;

    /// <summary>
    /// 
    /// </summary>
    public string DtoPath { get; set; } = "";

    public string ServicePath { get; set; } = "";

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
            files = DynamicApis.DirectoryGetFiles(currentDirectory, "*.nswag").ToList();
            if (files.Any())
            {
                _configPath = files[0];
            }
        }


        if (string.IsNullOrWhiteSpace(_configPath))
        {
            currentDirectory = AppContext.BaseDirectory;
            Log.Information("CurrentDirectory By [AppContext.BaseDirectory]:{0}", currentDirectory);
            files = DynamicApis.DirectoryGetFiles(currentDirectory, "*.nswag").ToList();
            if (files.Any())
            {
                _configPath = files[0];
            }
        }

        if (string.IsNullOrWhiteSpace(_configPath))
        {
            currentDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            Log.Information(
                "CurrentDirectory By [Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)]:{0}",
                currentDirectory);
            files = DynamicApis.DirectoryGetFiles(currentDirectory, "*.nswag").ToList();
            if (files.Any())
            {
                _configPath = files[0];
            }
        }

        if (string.IsNullOrWhiteSpace(_configPath))
        {
            currentDirectory = Directory.GetCurrentDirectory();
            Log.Information("CurrentDirectory By [Directory.GetCurrentDirectory()]:{0}", currentDirectory);
            files = DynamicApis.DirectoryGetFiles(currentDirectory, "*.nswag").ToList();
            if (files.Any())
            {
                _configPath = files[0];
            }
        }
    }
}