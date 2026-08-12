using BeautyCommerce.Application.Features.Authentication.DTOs;
using BeautyCommerce.Application.Features.Products.DTOs;
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

        if (context.Type == typeof(CreateProductDto))
        {
            schema.Example = new OpenApiObject
            {
                ["name"] = new OpenApiString("Hydrating Face Cream"),
                ["description"] = new OpenApiString("A lightweight, fast absorbing cream."),
                ["brandId"] = new OpenApiString(Guid.NewGuid().ToString()),
                ["categoryId"] = new OpenApiString(Guid.NewGuid().ToString()),
                ["isFeatured"] = new OpenApiBoolean(false),
                ["variants"] = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        ["price"] = new OpenApiDouble(29.99),
                        ["stock"] = new OpenApiInteger(100)
                    }
                },
                ["images"] = new OpenApiArray { new OpenApiString("https://example.com/image1.jpg") }
            };
        }
    }
}
