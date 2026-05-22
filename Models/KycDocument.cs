using System.ComponentModel.DataAnnotations;

namespace CasinoRoyale.Models;

public class KycDocument
{
    [Required]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public KycDocumentType DocumentType { get; set; }

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Ścieżka względna do pliku w storage (np. "kyc/5/abc123_front.jpg").
    /// Fizycznie plik jest przechowywany poza wwwroot, w App_Data/kyc/.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string StoragePath { get; set; } = string.Empty;

    [Required]
    public KycDocumentStatus Status { get; set; } = KycDocumentStatus.Pending;

    [Required]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Komentarz administratora (przy odrzuceniu lub zatwierdzeniu).
    /// </summary>
    [MaxLength(1000)]
    public string? AdminComment { get; set; }

    /// <summary>
    /// ID administratora, który podjął decyzję.
    /// </summary>
    public int? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAt { get; set; }
}
