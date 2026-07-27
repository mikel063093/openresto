using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace OpenRestoApi.Infrastructure.OpenApi;

internal sealed class OperatorMcpOpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info ??= new OpenApiInfo();

        string existingDescription = document.Info.Description?.Trim() ?? string.Empty;
        document.Info.Description = string.IsNullOrWhiteSpace(existingDescription)
            ? OperatorMcpDocumentation.OpenApiDescription
            : $"{existingDescription}\n\n{OperatorMcpDocumentation.OpenApiDescription}";

        return Task.CompletedTask;
    }
}
