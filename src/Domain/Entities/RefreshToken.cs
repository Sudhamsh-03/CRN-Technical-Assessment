namespace ProductsApi.Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresOn { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? RevokedOn { get; set; }
    public string? ReplacedByToken { get; set; }

    public User? User { get; set; }

    public bool IsActive => RevokedOn is null && DateTime.UtcNow < ExpiresOn;
}
