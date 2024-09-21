using NSwag;

using System.Net.Http;
using System;
using System.Threading.Tasks;
using NSwag.Commands.Generation;
using NSwagTsSplitter.Extensions;

namespace NSwagTsSplitter.Helpers;

public class OpenApiDocumentHelper
{
    public static async Task<OpenApiDocument> FromUrlAsync(string url)
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        using HttpClient httpClient = new HttpClient(handler);
        var openApiDocumentContent = await httpClient.GetStringAsync(url);
        OpenApiDocument openApiDocument;
        if (openApiDocumentContent.IsJson())
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

    public static async Task<OpenApiDocument> FromDocumentCommandAsync(FromDocumentCommand fromDocumentCommand)
    {
        if (string.IsNullOrEmpty(fromDocumentCommand.Json))
        {
            // fetch swagger
            return await FromUrlAsync(fromDocumentCommand.Url);
        }

        return await FromJsonAsync(fromDocumentCommand.Json);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fromDocumentCommand"></param>
    /// <returns></returns>
    public static OpenApiDocument FromDocumentCommand(FromDocumentCommand fromDocumentCommand)
    {
        if (string.IsNullOrEmpty(fromDocumentCommand.Json))
        {
            // fetch swagger
            return FromUrlAsync(fromDocumentCommand.Url).Result;
        }

        return FromJsonAsync(fromDocumentCommand.Json).Result;
    }
}