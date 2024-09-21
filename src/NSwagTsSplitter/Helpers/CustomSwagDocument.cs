using System.Collections.Generic;

namespace NSwagTsSplitter.Helpers;

public class CustomSwagDocument
{
    public CustomCodeGenerators CodeGenerators { get; set; }
}


public class CustomCodeGenerators
{
    public CustomOpenApiToTypeScriptClient OpenApiToTypeScriptClient { get; set; }
}

public class CustomOpenApiToTypeScriptClient
{
    /// <summary>
    /// 平铺dto，不按照Client分割DTO
    /// </summary>
    public bool DtoPlain { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public string ServiceFolder { get; set; }

    /// <summary>
    /// 通用模块
    /// </summary>
    public Dictionary<string, string> CommonModule { get; set; }

    /// <summary>
    /// 工具模块
    /// </summary>
    public List<string> UtilitiesModule { get; set; }

    public string DtoFolder { get; set; }
}