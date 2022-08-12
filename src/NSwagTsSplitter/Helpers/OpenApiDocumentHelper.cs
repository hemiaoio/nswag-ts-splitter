using NSwag;

using System.Net.Http;
using System;
using System.Threading.Tasks;

namespace NSwagTsSplitter.Helpers;

public class OpenApiDocumentHelper
{
    public static async Task<OpenApiDocument> FromUrlAsync(string url)
    {
        using HttpClient httpClient = new HttpClient();
        OpenApiDocument openApiDocument = await OpenApiDocument.FromJsonAsync(await httpClient.GetStringAsync(url));
        if (string.IsNullOrWhiteSpace(openApiDocument.BaseUrl) || openApiDocument.BaseUrl.StartsWith("http"))
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
}