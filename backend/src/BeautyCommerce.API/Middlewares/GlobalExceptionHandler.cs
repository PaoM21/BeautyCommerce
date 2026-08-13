using BeautyCommerce.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BeautyCommerce.API.Middlewares;

// Without this, every business-rule failure (invalid order status
// transition, insufficient stock, "cart not found", ...) bubbles up as an
// unhandled exception and the client sees a bare 500 with (in Development)
// a full stack trace, instead of the 400/401/404 the situation actually
// calls for. This maps the exception types already thrown across the
// Application layer to the right status code; it does not change which
// exception any handler throws.
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = Map(exception);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled exception on {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning(
                "{ExceptionType} on {Method} {Path} mapped to {StatusCode}: {Message}",
                exception.GetType().Name,
                httpContext.Request.Method,
                httpContext.Request.Path,
                statusCode,
                exception.Message);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            // The message on business exceptions is already the
            // user-facing Spanish copy the handlers wrote (e.g. "No hay
            // suficiente stock para realizar la salida."). For an
            // unmapped/unexpected exception we do not forward
            // exception.Message, since that could leak internal details
            // (SQL, file paths) — the full exception is logged above
            // instead.
            Detail = statusCode == StatusCodes.Status500InternalServerError
                ? "Ocurrió un error inesperado. Intenta de nuevo más tarde."
                : exception.Message,
            Instance = httpContext.Request.Path
        };

        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.ErrorMessage).ToArray());
        }

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);

        return true;
    }

    public static (int StatusCode, string Title) Map(Exception exception) =>
        exception switch
        {
            BadRequestException => (StatusCodes.Status400BadRequest, "Solicitud inválida."),
            ValidationException => (StatusCodes.Status400BadRequest, "Uno o más valores no son válidos."),
            ArgumentException => (StatusCodes.Status400BadRequest, "Solicitud inválida."),
            InvalidOperationException => (StatusCodes.Status400BadRequest, "Operación no permitida."),

            NotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado."),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado."),

            UnauthorizedException => (StatusCodes.Status401Unauthorized, "No autorizado."),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "No autorizado."),

            ConflictException => (StatusCodes.Status409Conflict, "Conflicto con el estado actual del recurso."),

            _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor.")
        };
}
