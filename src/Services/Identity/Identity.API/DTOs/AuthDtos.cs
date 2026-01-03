namespace Identity.API.DTOs;

public record RegisterRequest(
    string Email,
    string Password,
    string FullName
);

public record LoginRequest(
    string Email,
    string Password
);

public record LoginResponse(
    string Token,
    string Email,
    string FullName
);

public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    DateTime CreatedAt
);
