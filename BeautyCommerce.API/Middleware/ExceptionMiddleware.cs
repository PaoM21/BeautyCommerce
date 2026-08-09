using System.Net;
using System.Text.Json;
using BeautyCommerce.Application.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace BeautyCommerce.API.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            await WriteResponse(
                context,
                HttpStatusCode.NotFound,
                ex.Message);
        }
        catch (BadRequestException ex)
        {
            await WriteResponse(
                context,
                HttpStatusCode.BadRequest,
                ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado");

            await WriteResponse(
                context,
                HttpStatusCode.InternalServerError,
                "Ha ocurrido un error interno.");
        }
    }

    private static async Task WriteResponse(
        HttpContext context,
        HttpStatusCode status,
        string message)
    {
        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json";

        var response = new
        {
            success = false,
            message
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response));
    }
}
