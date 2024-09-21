using System.Collections.Generic;
using System.Threading.Tasks;

using NSwagTsSplitter.Models;

namespace NSwagTsSplitter.Generators;

public interface IGenerator
{
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    List<TsModuleModel> Generate();
}