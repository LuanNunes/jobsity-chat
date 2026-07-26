namespace com.jobsite.chat.Api.Endpoints;

// Api presentation records for the JSON surface (spec §2.2). Not Domain DTOs — constructed inline
// at the endpoint handlers (no mapper). Serialized/deserialized as camelCase by ASP.NET defaults.
public sealed record RegisterRequest(string Email, string DisplayName, string Password);

public sealed record LoginRequest(string Email, string Password, bool RememberMe);

// Response shape the SPA depends on for register/login/me. Built inline from ApplicationUser or the
// cookie ClaimsPrincipal — never leaks the password hash or the full IdentityUser.
public sealed record AuthUserResponse(string Id, string Email, string DisplayName);

public sealed record CreateRoomRequest(string Name);
