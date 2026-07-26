namespace com.jobsite.chat.Domain.Exceptions;

// thrown by domain factory validation.
public class DomainException(string message) : Exception(message);
