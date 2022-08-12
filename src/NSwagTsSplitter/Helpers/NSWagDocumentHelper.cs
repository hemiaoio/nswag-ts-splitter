using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using NSwag.Commands;

using Serilog;

namespace NSwagTsSplitter.Helpers;

public class NsWagDocumentHelper
{
    public static NSwagDocument LoadDocumentFromString(string configString, string configFilePath)
    {
        var nswagDocument = NSwagDocumentBase.FromJson<NSwagDocument>(string.Empty, configString);
        return nswagDocument;
    }

    public static async Task<NSwagDocument> LoadDocumentFromFileAsync(string configFilePath)
    {
        var fileContent = await File.ReadAllTextAsync(configFilePath);
        var nswagDocument = NSwagDocumentBase.FromJson<NSwagDocument>(configFilePath, fileContent);
        return nswagDocument;
    }
}