using NJsonSchema.Infrastructure;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Serilog;

namespace NSwagTsSplitter.Helpers;

public class ArgumentsHelper
{
    public static GeneratorConfigModel ReadArgs(string[] args)
    {
        string currentDirectory = DynamicApis.DirectoryGetCurrentDirectory();
        var model = new GeneratorConfigModel();
        Queue<string> queue = new Queue<string>(args);
        while (queue.Any())
        {
            var arg = queue.Dequeue();
            if (string.IsNullOrWhiteSpace(arg))
            {
                continue;
            }
            if (arg.Equals("-c", StringComparison.OrdinalIgnoreCase) || arg.Equals("--config", StringComparison.OrdinalIgnoreCase))
            {
                model.SetConfigPath(queue.Dequeue(), currentDirectory);
            }

            if (arg.Equals("-dp", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("--dto-path", StringComparison.OrdinalIgnoreCase))
            {
                model.DtoPath = queue.Dequeue();
            }
            if (arg.Equals("-sp", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("--service-path", StringComparison.OrdinalIgnoreCase))
            {
                model.ServicePath = queue.Dequeue();
            }
        }
        return model;
    }
    public static string[] GetNSwagPath(string[] args)
    {
        var files = new List<string>();
        string currentDirectory = DynamicApis.DirectoryGetCurrentDirectory();
        Log.Information("CurrentDirectory By [DynamicApis.DirectoryGetCurrentDirectory()]:{0}", currentDirectory);
        Queue<string> queue = new Queue<string>(args);
        while (queue.Any())
        {
            var arg = queue.Dequeue();
            if (string.IsNullOrWhiteSpace(arg))
            {
                continue;
            }
            if (arg.Equals("-c", StringComparison.OrdinalIgnoreCase) || arg.Equals("--config", StringComparison.OrdinalIgnoreCase))
            {
                while (true)
                {
                    if (!queue.Any())
                    {
                        break;
                    }

                    arg = queue.Dequeue();
                    if (arg.StartsWith("-"))
                    {
                        break;
                    }

                    var tmpPath = arg;
                    tmpPath = tmpPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                    if (Path.IsPathRooted(tmpPath))
                    {
                        files.Add(tmpPath);
                        continue;
                    }

                    if (arg.StartsWith("." + Path.DirectorySeparatorChar))
                    {
                        tmpPath = Path.GetFullPath(Path.Combine(currentDirectory, arg));
                        files.Add(tmpPath);
                        continue;
                    }

                    tmpPath = Path.GetFullPath(Path.Combine(currentDirectory, arg));
                    files.Add(tmpPath);
                }
            }

        }

        if (files.Any())
        {
            return files.ToArray();
        }

        files = DynamicApis.DirectoryGetFiles(currentDirectory, "*.nswag").ToList();
        if (files.Any())
        {
            return files.ToArray();
        }

        currentDirectory = AppContext.BaseDirectory;
        Log.Information<string>("CurrentDirectory By [AppContext.BaseDirectory]:{0}", currentDirectory);
        files = DynamicApis.DirectoryGetFiles(currentDirectory, "*.nswag").ToList();
        if (files.Any())
        {
            return files.ToArray();
        }

        currentDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        Log.Information<string>("CurrentDirectory By [Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)]:{0}", currentDirectory);
        files = DynamicApis.DirectoryGetFiles(currentDirectory, "*.nswag").ToList();
        if (files.Any())
        {
            return files.ToArray();
        }
        currentDirectory = Directory.GetCurrentDirectory();
        Log.Information<string>("CurrentDirectory By [Directory.GetCurrentDirectory()]:{0}", currentDirectory);
        files = DynamicApis.DirectoryGetFiles(currentDirectory, "*.nswag").ToList();
        if (files.Any())
        {
            return files.ToArray();
        }
        return files.ToArray();
    }

    public static string GetFolder(string[] args)
    {
        throw new NotImplementedException();
    }
}