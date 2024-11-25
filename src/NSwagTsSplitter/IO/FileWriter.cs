using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using NSwagTsSplitter.Configuration;
using NSwagTsSplitter.Models;

namespace NSwagTsSplitter.IO;

public class FileWriter
{
    private readonly GeneratorOption _option;
    private readonly List<TsModuleModel> _modules;

    /// <summary>
    /// 
    /// </summary>
    public FileWriter(GeneratorOption option)
    {
        _option = option;
        _modules = new List<TsModuleModel>();
    }

    public void AddModules(ICollection<TsModuleModel> modules)
    {
        _modules.AddRange(modules);
    }
    public async Task WriteAsync()
    {
        foreach (var tsModuleModel in _modules)
        {
            var encode = new UTF8Encoding(false);
            var fullPath = Path.Combine(_option.OutputBaseDirectory, tsModuleModel.ModulePath);
            var folderPath = Path.GetDirectoryName(fullPath);
            if (folderPath == null)
            {
                continue;
            }
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            if (string.IsNullOrWhiteSpace(tsModuleModel.ModuleContent))
            {
                continue;
            }
            await File.WriteAllTextAsync(fullPath, tsModuleModel.ModuleContent, encode);
        }
    }
}