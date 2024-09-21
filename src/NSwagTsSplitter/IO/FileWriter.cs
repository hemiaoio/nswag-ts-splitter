using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NSwag.Commands;

using NSwagTsSplitter.Configuration;
using NSwagTsSplitter.Extensions;
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
            await File.WriteAllTextAsync(tsModuleModel.ModulePath, tsModuleModel.ModuleContent, encode);
        }
    }
}