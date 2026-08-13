using BeautyCommerce.API.Middlewares;
using BeautyCommerce.Application.Common.Exceptions;
using FluentAssertions;
using FluentValidation.Results;

namespace BeautyCommerce.Tests.Middlewares;

public class GlobalExceptionHandlerTests
{
    [Theory]
    [InlineData(typeof(BadRequestException), 400)]
    [InlineData(typeof(ArgumentException), 400)]
    [InlineData(typeof(InvalidOperationException), 400)]
    [InlineData(typeof(NotFoundException), 404)]
    [InlineData(typeof(KeyNotFoundException), 404)]
    [InlineData(typeof(UnauthorizedException), 401)]
    [InlineData(typeof(UnauthorizedAccessException), 401)]
    [InlineData(typeof(ConflictException), 409)]
    [InlineData(typeof(TimeoutException), 500)]
    public void Maps_Exception_Type_To_Expected_Status_Code(Type exceptionType, int expectedStatus)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "test message")!;

        var (statusCode, _) = GlobalExceptionHandler.Map(exception);

        statusCode.Should().Be(expectedStatus);
    }

    [Fact]
    public void Maps_FluentValidation_ValidationException_To_400()
    {
        var exception = new FluentValidation.ValidationException(
            new List<ValidationFailure>
            {
                new("Name", "El nombre es obligatorio.")
            });

        var (statusCode, _) = GlobalExceptionHandler.Map(exception);

        statusCode.Should().Be(400);
    }

    [Fact]
    public void Unmapped_Exception_Falls_Back_To_500()
    {
        // Regression guard for the exact bug this middleware fixes: before
        // it existed, every business-rule violation (invalid order status
        // transition, insufficient stock, ...) bubbled up as a raw 500 with
        // a stack trace instead of the 400/404/401 the situation called
        // for. Unknown/unexpected exceptions should still be a safe 500.
        var (statusCode, title) = GlobalExceptionHandler.Map(new Exception("boom"));

        statusCode.Should().Be(500);
        title.Should().NotBeNullOrWhiteSpace();
    }
}
