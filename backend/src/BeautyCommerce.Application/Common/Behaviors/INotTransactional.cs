namespace BeautyCommerce.Application.Common.Behaviors;

// Opt-out marker for TransactionBehavior. A command implementing this
// interface is executed without an ambient database transaction, so any
// side effect it produces on its failure path (e.g. ASP.NET Identity's own
// AccessFailedCount/LockoutEnd bookkeeping on a failed login) is persisted
// even when the handler ultimately throws.
public interface INotTransactional
{
}
