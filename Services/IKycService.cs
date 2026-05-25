using CasinoRoyale.Models;
using Microsoft.AspNetCore.Http;

namespace CasinoRoyale.Services;

public interface IKycService
{
    /// <summary>
    /// Zapisuje przesłany dokument na dysk (poza wwwroot) i dodaje encję KycDocument.
    /// </summary>
    Task<KycDocument> UploadDocumentAsync(int userId, IFormFile file, KycDocumentType documentType);

    /// <summary>
    /// Zwraca wszystkie dokumenty danego użytkownika.
    /// </summary>
    Task<List<KycDocument>> GetUserDocumentsAsync(int userId);

    /// <summary>
    /// Zwraca dokument po ID.
    /// </summary>
    Task<KycDocument?> GetDocumentByIdAsync(int documentId);

    /// <summary>
    /// Zwraca ścieżkę fizyczną do pliku dokumentu.
    /// </summary>
    string GetStoragePath(KycDocument document);

    /// <summary>
    /// Zatwierdza dokument KYC.
    /// </summary>
    Task ApproveDocumentAsync(int documentId, int adminUserId, string? comment);

    /// <summary>
    /// Odrzuca dokument KYC.
    /// </summary>
    Task RejectDocumentAsync(int documentId, int adminUserId, string? comment);

    /// <summary>
    /// Zwraca dokumenty oczekujące na weryfikację (dla admina).
    /// </summary>
    Task<List<KycDocument>> GetPendingDocumentsAsync();

    /// <summary>
    /// Zwraca wszystkie dokumenty (dla admina).
    /// </summary>
    Task<List<KycDocument>> GetAllDocumentsAsync();

    /// <summary>
    /// Aktualizuje User.KycStatus na podstawie stanu dokumentów.
    /// </summary>
    Task RefreshUserKycStatusAsync(int userId);

    /// <summary>
    /// Resetuje status KYC użytkownika do NotSubmitted i unieważnia
    /// wszystkie dotychczasowe dokumenty (np. po zmianie imienia/nazwiska).
    /// </summary>
    Task ResetKycStatusAsync(int userId);
}
