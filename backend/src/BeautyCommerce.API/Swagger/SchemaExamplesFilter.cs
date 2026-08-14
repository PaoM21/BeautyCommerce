using BeautyCommerce.Application.Features.Authentication.DTOs;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BeautyCommerce.API.Swagger;

public class SchemaExamplesFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(RegisterRequestDto))
        {
            schema.Example = new OpenApiObject
            {
                ["firstName"] = new OpenApiString("Jane"),
                ["lastName"] = new OpenApiString("Doe"),
                ["email"] = new OpenApiString("jane.doe@example.com"),
                ["password"] = new OpenApiString("P@ssw0rd123"),
                ["confirmPassword"] = new OpenApiString("P@ssw0rd123")
            };
        }

        if (context.Type == typeof(LoginRequestDto))
        {
            schema.Example = new OpenApiObject
            {
                ["email"] = new OpenApiString("jane.doe@example.com"),
                ["password"] = new OpenApiString("P@ssw0rd123")
            };
        }
    }
}
