using System.IO;
using System.Threading.Tasks;

using NSwag.Commands;

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
        return LoadDocumentFromString(fileContent, configFilePath);
    }
}