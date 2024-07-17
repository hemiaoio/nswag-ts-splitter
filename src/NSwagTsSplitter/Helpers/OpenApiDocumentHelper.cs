using NSwag;

using System.Net.Http;
using System;
using System.Threading.Tasks;
using NSwagTsSplitter.Utils;

namespace NSwagTsSplitter.Helpers;

public class OpenApiDocumentHelper
{
    public static async Task<OpenApiDocument> FromUrlAsync(string url)
    {
        using HttpClient httpClient = new HttpClient();
        var openApiDocumentContent = await httpClient.GetStringAsync(url);
        OpenApiDocument openApiDocument = null;
        if (StringHelper.IsJson(openApiDocumentContent))
        {
            openApiDocument = await OpenApiDocument.FromJsonAsync(openApiDocumentContent);
        }
        else
        {
            openApiDocument = await OpenApiYamlDocument.FromYamlAsync(openApiDocumentContent);
        }

        if (string.IsNullOrWhiteSpace(openApiDocument.BaseUrl))
        {
            return openApiDocument;
        }

        if (openApiDocument.BaseUrl.StartsWith("http"))
            return openApiDocument;
        string str = openApiDocument.BaseUrl;
        if (str.EndsWith("/"))
            str = str.Remove(str.Length - 1);
        Uri uri = new Uri(url);
        openApiDocument.Servers.Clear();
        openApiDocument.Servers.Add(new OpenApiServer()
        {
            Url = uri.Scheme + "://" + str
        });
        return openApiDocument;
    }

    public async Task<OpenApiDocument> FromPathAsync(string swaggerFilePath)
    {
        return await OpenApiDocument.FromFileAsync(swaggerFilePath);
    }

    public static async Task<OpenApiDocument> FromJsonAsync(string json)
    {
        return await OpenApiDocument.FromJsonAsync(json);
    }
}