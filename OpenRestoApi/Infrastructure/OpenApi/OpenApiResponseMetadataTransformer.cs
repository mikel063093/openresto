using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace OpenRestoApi.Infrastructure.OpenApi;

internal sealed class OpenApiResponseMetadataTransformer : IOpenApiOperationTransformer
{
    public async Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (operation.Responses is null)
        {
            return;
        }

        foreach ((string statusCode, IOpenApiResponse response) in operation.Responses)
        {
            if (!int.TryParse(statusCode, out int parsedStatusCode))
            {
                continue;
            }

            List<Type> responseTypes = context.Description.SupportedResponseTypes
                .Where(candidate => candidate.StatusCode == parsedStatusCode && candidate.Type != null && candidate.Type != typeof(void))
                .Select(candidate => candidate.Type!)
                .Concat(GetExplicitResponseTypes(context, parsedStatusCode))
                .Distinct()
                .ToList();

            response.Description = BuildDescription(parsedStatusCode, responseTypes, response.Description);

            if (response.Content is null
                || !response.Content.TryGetValue("application/json", out OpenApiMediaType? mediaType))
            {
                continue;
            }

            if (responseTypes.Count > 1)
            {
                var oneOfSchemas = new List<OpenApiSchema>(responseTypes.Count);
                foreach (Type responseType in responseTypes)
                {
                    oneOfSchemas.Add(await context.GetOrCreateSchemaAsync(responseType, parameterDescription: null, cancellationToken));
                }

                mediaType.Schema = new OpenApiSchema
                {
                    OneOf = [.. oneOfSchemas]
                };
            }

            mediaType.Example ??= OpenApiExampleFactory.CreateForStatus(parsedStatusCode, responseTypes);
        }
    }

    private static IEnumerable<Type> GetExplicitResponseTypes(
        OpenApiOperationTransformerContext context,
        int statusCode)
    {
        if (context.Description.ActionDescriptor is not ControllerActionDescriptor controllerAction)
        {
            return [];
        }

        return controllerAction.ControllerTypeInfo
            .GetCustomAttributes(typeof(ProducesResponseTypeAttribute), inherit: true)
            .Concat(controllerAction.MethodInfo.GetCustomAttributes(typeof(ProducesResponseTypeAttribute), inherit: true))
            .OfType<ProducesResponseTypeAttribute>()
            .Where(attribute => attribute.StatusCode == statusCode
                && attribute.Type is not null
                && attribute.Type != typeof(void))
            .Select(attribute => attribute.Type!);
    }

    private static string BuildDescription(int statusCode, IReadOnlyList<Type> responseTypes, string? existingDescription)
    {
        if (!string.IsNullOrWhiteSpace(existingDescription)
            && !(statusCode == StatusCodes.Status200OK
                && responseTypes.Count == 0
                && string.Equals(existingDescription, "OK", StringComparison.OrdinalIgnoreCase)))
        {
            return existingDescription;
        }

        return statusCode switch
        {
            StatusCodes.Status200OK when responseTypes.Count == 0 => "Successful response with no response body.",
            StatusCodes.Status200OK => "Successful response.",
            StatusCodes.Status201Created => "Resource created successfully.",
            StatusCodes.Status204NoContent => "Request completed successfully with no response body.",
            StatusCodes.Status400BadRequest when responseTypes.Contains(typeof(ValidationProblemDetails))
                && responseTypes.Count > 1 => "Request validation failed or the request was rejected by business rules.",
            StatusCodes.Status400BadRequest when responseTypes.Contains(typeof(ValidationProblemDetails))
                => "Request validation failed.",
            StatusCodes.Status400BadRequest => "Request could not be processed.",
            StatusCodes.Status401Unauthorized => "Authentication is required or the provided credentials were rejected.",
            StatusCodes.Status403Forbidden => "The authenticated user is not allowed to access this resource.",
            StatusCodes.Status404NotFound => "The requested resource was not found.",
            StatusCodes.Status409Conflict => "The request conflicted with the current state of the resource.",
            StatusCodes.Status429TooManyRequests => "The request was rate limited.",
            StatusCodes.Status500InternalServerError => "An unexpected server-side error occurred.",
            _ => $"{statusCode} response."
        };
    }
}
