namespace com.jobsite.chat.Domain.Dtos;

// Registration input for the JSON auth surface (spec §2.3).
public sealed record RegisterDto(string Email, string DisplayName, string Password);
