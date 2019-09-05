using System.Linq;
using System.Threading.Tasks;
using NSwag.Commands;

namespace NSwagTsSplitter
{
    public class NSWagDocumentHelper
    {
        public async Task<NSwagDocument> LoadDocumentFromFileAsync(string path)
        {
            return await NSwagDocument.LoadWithTransformationsAsync(path, string.Empty);
        }

        public NSwagDocument LoadDocumentFromString(string configJson)
        {
            return NSwagDocument.FromJson<NSwagDocument>(null, configJson);
        }
    }
}