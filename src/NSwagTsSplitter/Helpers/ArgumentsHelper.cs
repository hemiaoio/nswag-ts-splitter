using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using NSwagTsSplitter.Configuration;

using Serilog;

using static System.String;

namespace NSwagTsSplitter.Helpers;

public class ArgumentsHelper
{
    /// <summary>
    /// Gets or sets the root binary directory where the command line executables loaded from.
    /// </summary>
    /// 
    public static string RootBinaryDirectory { get; set; } = Directory.GetCurrentDirectory();

    public static GeneratorOption ReadArgs(string[] args, GeneratorOption model = null)
    {
        bool fromArgs = false;
        if (model == null)
        {
            fromArgs = true;
            model = new GeneratorOption();
        }

        Queue<string> queue = new Queue<string>(args);
        while (queue.Any())
        {
            var arg = queue.Dequeue();
            if (IsNullOrWhiteSpace(arg))
            {
                continue;
            }

            if (arg.Equals("-c", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("--config", StringComparison.OrdinalIgnoreCase))
            {
                model.SetConfigPath(queue.Dequeue(), RootBinaryDirectory);
            }

            if (arg.Equals("-pd", StringComparison.OrdinalIgnoreCase) || arg.Equals("--plain-dto"))
            {
                model.PlainDto = true;
            }

            if (arg.Equals("-df", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("--dto-folder", StringComparison.OrdinalIgnoreCase))
            {
                model.DtoFolder = queue.Dequeue();
            }

            if (arg.Equals("-sp", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("--service-path", StringComparison.OrdinalIgnoreCase))
            {
                model.ServiceFolder = queue.Dequeue();
            }
        }

        if (IsNullOrEmpty(model.ConfigPath) || !fromArgs)
        {
            return model;
        }

        model = GeneratorOption.FromConfigFile(model.ConfigPath);
        ReadArgs(args, model);
        return model;
    }

    public static string[] GetNSwagPath(string[] args)
    {
        var files = new List<string>();
        string currentDirectory = RootBinaryDirectory;
        Log.Information("CurrentDirectory By [Directory.GetCurrentDirectory()]:{0}", currentDirectory);
        Queue<string> queue = new Queue<string>(args);
        while (queue.Any())
        {
            var arg = queue.Dequeue();
            if (IsNullOrWhiteSpace(arg))
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

        files = Directory.GetFiles(currentDirectory, "*.nswag").ToList();
        if (files.Any())
        {
            return files.ToArray();
        }

        currentDirectory = Path.GetDirectoryName(Environment.ProcessPath);
        Log.Information("CurrentDirectory By [Path.GetDirectoryName(Environment.ProcessPath)]:{0}", currentDirectory);
        files = Directory.GetFiles(currentDirectory, "*.nswag").ToList();
        if (files.Any())
        {
            return files.ToArray();
        }
        currentDirectory = Directory.GetCurrentDirectory();
        Log.Information<string>("CurrentDirectory By [Directory.GetCurrentDirectory()]:{0}", currentDirectory);
        files = Directory.GetFiles(currentDirectory, "*.nswag").ToList();
        if (files.Any())
        {
            return files.ToArray();
        }
        return files.ToArray();
    }
}