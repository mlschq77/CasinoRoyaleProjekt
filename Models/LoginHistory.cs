using System.ComponentModel.DataAnnotations;

namespace CasinoRoyale.Models;

public class LoginHistory
{
    [Required]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [MaxLength(45)]
    public string IpAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string UserAgent { get; set; } = string.Empty;

    public bool Successful { get; set; }

    [MaxLength(20)]
    public string EventType { get; set; } = "login";

    [Required]
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
}
