namespace com.jobsite.chat.Domain.Abstractions;

public interface IEntityValidator<TKey> : IEntity<TKey> where TKey : notnull;
