using BeautyCommerce.Application.Common.Behaviors;
using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace BeautyCommerce.Tests.Behaviors;

public class TransactionBehaviorLoggingTests
{
    public record TestCommand : IRequest<int>;

    private static void VerifyLog(
        Mock<ILogger<TransactionBehavior<TestCommand, int>>> logger,
        LogLevel level,
        Times times)
    {
        logger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    [Fact]
    public async Task Expected_Business_Exception_Logs_Warning_Not_Error()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var _ = connection;

        var logger = new Mock<ILogger<TransactionBehavior<TestCommand, int>>>();
        var behavior = new TransactionBehavior<TestCommand, int>(context, logger.Object);

        var act = async () => await behavior.Handle(
            new TestCommand(),
            _ => throw new BadRequestException("Rechazo de negocio esperado."),
            CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();

        VerifyLog(logger, LogLevel.Warning, Times.Once());
        VerifyLog(logger, LogLevel.Error, Times.Never());
    }

    [Fact]
    public async Task Unrecognized_Exception_Still_Logs_Error()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var _ = connection;

        var logger = new Mock<ILogger<TransactionBehavior<TestCommand, int>>>();
        var behavior = new TransactionBehavior<TestCommand, int>(context, logger.Object);

        var act = async () => await behavior.Handle(
            new TestCommand(),
            _ => throw new InvalidCastException("Fallo inesperado, no es una excepción de negocio conocida."),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCastException>();

        VerifyLog(logger, LogLevel.Error, Times.Once());
        VerifyLog(logger, LogLevel.Warning, Times.Never());
    }
}
