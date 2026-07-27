namespace com.jobsite.chat.Domain.Dtos;

public sealed record RegisterDto(string Email, string DisplayName, string Password);
