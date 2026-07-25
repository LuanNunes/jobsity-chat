namespace com.jobsite.chat.Domain.Exceptions;

// thrown by Ensure guards and domain factory methods.
public class DomainException(string message) : Exception(message);
