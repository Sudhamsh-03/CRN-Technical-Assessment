namespace ProductsApi.Application.DTOs.Auth;

public class AuthResponse
{
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresOn { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
}
