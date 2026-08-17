using BeautyCommerce.Application.Common.Behaviors;
using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BeautyCommerce.Tests.Behaviors;

// Regression coverage for the Punto 5 / Escenario I finding: a failed
// LoginCommand needs its side effect (ASP.NET Identity's own
// AccessFailedCount/LockoutEnd bookkeeping) to survive even though
// LoginAsync throws UnauthorizedException — which TransactionBehavior
// otherwise rolls back for every "Command"-suffixed request. The fix is an
// opt-out marker (INotTransactional), applied only to LoginCommand. These
// tests prove both halves: the opt-out actually skips the rollback, and
// ordinary commands (not marked) keep rolling back exactly as before.
public class TransactionBehaviorOptOutTests
{
    public record TransactionalTestCommand : IRequest<int>;

    public record NotTransactionalTestCommand : IRequest<int>, INotTransactional;

    [Fact]
    public async Task Ordinary_Command_Write_Does_Not_Survive_A_Thrown_UnauthorizedException()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var _ = connection;

        var behavior = new TransactionBehavior<TransactionalTestCommand, int>(
            context, NullLogger<TransactionBehavior<TransactionalTestCommand, int>>.Instance);

        var brandId = Guid.NewGuid();

        var act = async () => await behavior.Handle(
            new TransactionalTestCommand(),
            async _ =>
            {
                context.Brands.Add(new Brand { Id = brandId, Name = "QA Should Roll Back", Description = "QA" });
                await context.SaveChangesAsync();
                throw new UnauthorizedException("Simulated authorization failure mid-operation.");
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();

        var exists = await context.Brands.AsNoTracking().AnyAsync(b => b.Id == brandId);
        exists.Should().BeFalse(
            "an ordinary command's write must still roll back on an expected business exception — the opt-out must not weaken this default");
    }

    [Fact]
    public async Task NotTransactional_Command_Write_Survives_A_Thrown_UnauthorizedException()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var _ = connection;

        var behavior = new TransactionBehavior<NotTransactionalTestCommand, int>(
            context, NullLogger<TransactionBehavior<NotTransactionalTestCommand, int>>.Instance);

        var brandId = Guid.NewGuid();

        var act = async () => await behavior.Handle(
            new NotTransactionalTestCommand(),
            async _ =>
            {
                context.Brands.Add(new Brand { Id = brandId, Name = "QA Should Survive", Description = "QA" });
                await context.SaveChangesAsync();
                throw new UnauthorizedException("Simulated login failure — the failed-attempt bookkeeping must survive this.");
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();

        var exists = await context.Brands.AsNoTracking().AnyAsync(b => b.Id == brandId);
        exists.Should().BeTrue(
            "a command opted out of the ambient transaction must keep its write even when it throws — this is exactly what makes login lockout persist");
    }
}
