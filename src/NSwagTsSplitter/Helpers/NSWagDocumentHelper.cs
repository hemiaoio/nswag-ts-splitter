using System.IO;
using System.Threading.Tasks;

using Newtonsoft.Json;

using NSwag.Commands;

using Serilog;

namespace NSwagTsSplitter.Helpers;

public class NsWagDocumentHelper
{
    public static NSwagDocument LoadDocumentFromString(string configString, string configFilePath)
    {
        var nswagDocument = NSwagDocumentBase.FromJson<NSwagDocument>(configFilePath, configString);
        return nswagDocument;
    }

    public static async Task<NSwagDocument> LoadDocumentFromFileAsync(string configFilePath)
    {
        var fileContent = await File.ReadAllTextAsync(configFilePath);
        Log.Information($"NSwag config content:{fileContent}");
        return LoadDocumentFromString(fileContent, configFilePath);
    }
}