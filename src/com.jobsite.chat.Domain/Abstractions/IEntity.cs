namespace com.jobsite.chat.Domain.Abstractions;

public interface IEntity<TKey> where TKey : notnull
{
    TKey Id { get; }
}
