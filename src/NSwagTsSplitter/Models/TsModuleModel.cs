using System.Collections.Generic;
using NJsonSchema;
using NJsonSchema.CodeGeneration;

namespace NSwagTsSplitter.Models;

public class TsModuleModel
{
    public string ModuleName { get; set; }
    public string ModuleContent { get; set; }
    public string ModulePath { get; set; }
    public List<CodeArtifact> Artifacts { get; set; }
    public JsonSchema Schema { get; set; }
}