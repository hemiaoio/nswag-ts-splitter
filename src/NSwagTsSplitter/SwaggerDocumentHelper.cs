using System.Threading.Tasks;
using NJsonSchema;
using NSwag;

namespace NSwagTsSplitter
{
    public class SwaggerDocumentHelper
    {
        public async Task<OpenApiDocument> FromUrlAsync(string url)
        {
            return await OpenApiDocument.FromUrlAsync(url);
        }

        public async Task<OpenApiDocument> FromJsonAsync(string json, SchemaType schemaType = SchemaType.Swagger2)
        {
            return await OpenApiDocument.FromJsonAsync(json, null, schemaType);
        }

        public async Task<OpenApiDocument> FromPathAsync(string swaggerPath)
        {
            return await OpenApiDocument.FromFileAsync(swaggerPath);
        }
    }
}