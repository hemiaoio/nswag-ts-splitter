using System.Linq;
using NJsonSchema;
using NSwag;

namespace NSwagTsSplitter.Extensions;

public static class OpenApiDocumentExtensions
{
    private static string GetOperationNameFromPath(OpenApiOperationDescription operation)
    {
        var pathSegments = operation.Path.Trim('/').Split('/');
        var lastPathSegment = pathSegments.LastOrDefault(s => !s.Contains("{"));
        return string.IsNullOrEmpty(lastPathSegment) ? "Anonymous" : lastPathSegment;
    }
    public static void GenerateOperationIds(this OpenApiDocument apiDocument, bool groupByTag)
    {
        if (!groupByTag)
        {
            apiDocument.GenerateOperationIds();
            return;
        }
        // Generate missing IDs

        var groupByTagOperations = apiDocument.Operations.GroupBy(c => c.Operation.Tags.FirstOrDefault());

        foreach (var operationsList in groupByTagOperations)
        {

            foreach (var operation in operationsList.Where(o => string.IsNullOrEmpty(o.Operation.OperationId)))
            {
                operation.Operation.OperationId = GetOperationNameFromPath(operation);
            }

            // Find non-unique operation IDs

            // 1: Append all to methods returning collections
            foreach (var group in operationsList.GroupBy(o => o.Operation.OperationId))
            {
                if (group.Count() > 1)
                {
                    var collections = group.Where(o => o.Operation.ActualResponses.Any(r =>
                              HttpUtilities.IsSuccessStatusCode(r.Key) &&
                              r.Value.Schema?.ActualSchema.Type == JsonObjectType.Array));
                    // if we have just collections, adding All will not help in discrimination
                    if (collections.Count() == group.Count()) continue;

                    foreach (var o in group)
                    {
                        var isCollection = o.Operation.ActualResponses.Any(r =>
                            HttpUtilities.IsSuccessStatusCode(r.Key) &&
                            r.Value.Schema?.ActualSchema.Type == JsonObjectType.Array);

                        if (isCollection)
                        {
                            o.Operation.OperationId += "All";
                        }
                    }
                }
            }

            // 2: Append the Method type
            foreach (var group in operationsList.GroupBy(o => o.Operation.OperationId))
            {
                if (group.Count() > 1)
                {
                    var methods = group.Select(o => o.Method.ToUpper()).Distinct();
                    if (methods.Count() == 1) continue;

                    foreach (var o in group)
                    {
                        o.Operation.OperationId += o.Method.ToUpper();
                    }
                }
            }

            // 3: Append numbers as last resort
            foreach (var group in operationsList.GroupBy(o => o.Operation.OperationId))
            {
                var operations = group.ToList();
                if (group.Count() > 1)
                {
                    // Add numbers
                    var i = 2;
                    foreach (var operation in operations.Skip(1))
                    {
                        operation.Operation.OperationId += i++;
                    }

                    GenerateOperationIds(apiDocument, true);
                    return;
                }
            }

        }
    }
}