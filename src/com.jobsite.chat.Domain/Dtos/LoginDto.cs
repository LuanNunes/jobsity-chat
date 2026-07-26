namespace com.jobsite.chat.Domain.Dtos;

// Login input for the JSON auth surface (spec §2.3).
public sealed record LoginDto(string Email, string Password, bool RememberMe);
