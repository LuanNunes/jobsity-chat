namespace com.jobsite.chat.Domain.Abstractions;

// Empty MARKER extending IEntity<TKey>. Invariants enforced at construction
// (private ctor + factory + inline FluentValidation throwing DomainException),
// so any live instance is valid by construction. Adding Validate()/IsValid would
// duplicate factory validation or expose the private InlineValidator — rejected.
public interface IEntityValidator<TKey> : IEntity<TKey> where TKey : notnull;
