using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BeautyCommerce.API.Swagger;

public class AddStandardResponsesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Responses == null)
            operation.Responses = new OpenApiResponses();

        AddIfMissing(operation, "400", "Bad Request", new OpenApiObject
        {
            ["success"] = new OpenApiBoolean(false),
            ["message"] = new OpenApiString("Validation failed or bad request."),
        });

        AddIfMissing(operation, "401", "Unauthorized", new OpenApiObject
        {
            ["success"] = new OpenApiBoolean(false),
            ["message"] = new OpenApiString("Authentication required or invalid token."),
        });

        AddIfMissing(operation, "404", "Not Found", new OpenApiObject
        {
            ["success"] = new OpenApiBoolean(false),
            ["message"] = new OpenApiString("Resource not found."),
        });

        AddIfMissing(operation, "500", "Internal Server Error", new OpenApiObject
        {
            ["success"] = new OpenApiBoolean(false),
            ["message"] = new OpenApiString("An unexpected error occurred."),
        });
    }

    private static void AddIfMissing(OpenApiOperation operation, string statusCode, string description, IOpenApiAny example)
    {
        if (!operation.Responses.ContainsKey(statusCode))
        {
            operation.Responses[statusCode] = new OpenApiResponse
            {
                Description = description,
                Content =
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema { Type = "object" },
                        Example = example
                    }
                }
            };
        }
    }
}
