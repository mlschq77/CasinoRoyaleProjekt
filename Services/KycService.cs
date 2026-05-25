using CasinoRoyale.Data;
using CasinoRoyale.Models;
using Microsoft.EntityFrameworkCore;

namespace CasinoRoyale.Services;

public class KycService : IKycService
{
    private readonly Automaty _db;
    private readonly IWebHostEnvironment _env;
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/pdf"
    };

    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public KycService(Automaty db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<KycDocument> UploadDocumentAsync(int userId, IFormFile file, KycDocumentType documentType)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Plik jest pusty.");

        if (file.Length > MaxFileSize)
            throw new ArgumentException("Plik jest zbyt duży. Maksymalny rozmiar to 10 MB.");

        if (!AllowedContentTypes.Contains(file.ContentType))
            throw new ArgumentException("Niedozwolony typ pliku. Akceptujemy tylko JPEG, PNG, WebP i PDF.");

        // Przygotowanie ścieżki storage (poza wwwroot)
        var storageDir = Path.Combine(_env.ContentRootPath, "App_Data", "kyc", userId.ToString());
        Directory.CreateDirectory(storageDir);

        var uniqueName = $"{Guid.NewGuid():N}_{file.FileName}";
        var relativePath = Path.Combine("kyc", userId.ToString(), uniqueName);
        var fullPath = Path.Combine(_env.ContentRootPath, "App_Data", relativePath);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var document = new KycDocument
        {
            UserId = userId,
            DocumentType = documentType,
            FileName = file.FileName,
            ContentType = file.ContentType,
            StoragePath = relativePath,
            Status = KycDocumentStatus.Pending,
            UploadedAt = DateTime.UtcNow
        };

        _db.KycDocuments.Add(document);
        await _db.SaveChangesAsync();

        await RefreshUserKycStatusAsync(userId);

        return document;
    }

    public async Task<List<KycDocument>> GetUserDocumentsAsync(int userId)
    {
        return await _db.KycDocuments
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
    }

    public async Task<KycDocument?> GetDocumentByIdAsync(int documentId)
    {
        return await _db.KycDocuments.FindAsync(documentId);
    }

    public string GetStoragePath(KycDocument document)
    {
        return Path.Combine(_env.ContentRootPath, "App_Data", document.StoragePath);
    }

    public async Task ApproveDocumentAsync(int documentId, int adminUserId, string? comment)
    {
        var document = await _db.KycDocuments.FindAsync(documentId)
            ?? throw new InvalidOperationException("Dokument nie istnieje.");

        document.Status = KycDocumentStatus.Approved;
        document.ReviewedByUserId = adminUserId;
        document.ReviewedAt = DateTime.UtcNow;
        document.AdminComment = comment;

        await _db.SaveChangesAsync();
        await RefreshUserKycStatusAsync(document.UserId);
    }

    public async Task RejectDocumentAsync(int documentId, int adminUserId, string? comment)
    {
        var document = await _db.KycDocuments.FindAsync(documentId)
            ?? throw new InvalidOperationException("Dokument nie istnieje.");

        document.Status = KycDocumentStatus.Rejected;
        document.ReviewedByUserId = adminUserId;
        document.ReviewedAt = DateTime.UtcNow;
        document.AdminComment = comment;

        await _db.SaveChangesAsync();
        await RefreshUserKycStatusAsync(document.UserId);
    }

    public async Task<List<KycDocument>> GetPendingDocumentsAsync()
    {
        return await _db.KycDocuments
            .AsNoTracking()
            .Where(d => d.Status == KycDocumentStatus.Pending)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
    }

    public async Task<List<KycDocument>> GetAllDocumentsAsync()
    {
        return await _db.KycDocuments
            .AsNoTracking()
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
    }

    public async Task ResetKycStatusAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return;

        var documents = await _db.KycDocuments
            .Where(d => d.UserId == userId && d.Status != KycDocumentStatus.Rejected)
            .ToListAsync();

        foreach (var doc in documents)
        {
            doc.Status = KycDocumentStatus.Rejected;
            doc.AdminComment = "Anulowano — zmiana danych osobowych (imię/nazwisko).";
            doc.ReviewedAt = DateTime.UtcNow;
        }

        user.KycStatus = UserKycStatus.NotSubmitted;

        await _db.SaveChangesAsync();
    }

    public async Task RefreshUserKycStatusAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return;

        var documents = await _db.KycDocuments
            .Where(d => d.UserId == userId)
            .ToListAsync();

        if (documents.Count == 0)
        {
            user.KycStatus = UserKycStatus.NotSubmitted;
        }
        else if (documents.Any(d => d.Status == KycDocumentStatus.Approved))
        {
            user.KycStatus = UserKycStatus.Approved;
        }
        else if (documents.Any(d => d.Status == KycDocumentStatus.Pending))
        {
            user.KycStatus = UserKycStatus.Pending;
        }
        else
        {
            user.KycStatus = UserKycStatus.Rejected;
        }

        await _db.SaveChangesAsync();
    }
}
