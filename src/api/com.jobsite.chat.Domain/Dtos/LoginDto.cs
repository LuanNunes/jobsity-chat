namespace com.jobsite.chat.Domain.Dtos;

public sealed record LoginDto(string Email, string Password, bool RememberMe);
