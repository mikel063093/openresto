using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace OpenRestoApi.Infrastructure.OpenApi;

internal sealed class OpenApiSchemaExampleTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.ParameterDescription != null || schema.Example != null)
        {
            return Task.CompletedTask;
        }

        JsonNode? example = OpenApiExampleFactory.CreateForType(context.JsonTypeInfo.Type);
        if (example != null)
        {
            schema.Example = example;
        }

        return Task.CompletedTask;
    }
}
