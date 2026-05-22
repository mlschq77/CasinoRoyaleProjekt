using CasinoRoyale.Models;
using CasinoRoyale.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace CasinoRoyale.Controllers;

[Authorize]
public class KycController : Controller
{
    private readonly IKycService _kycService;

    public KycController(IKycService kycService)
    {
        _kycService = kycService;
    }

    /// <summary>
    /// Lista dokumentów użytkownika + formularz do przesłania nowego.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var documents = await _kycService.GetUserDocumentsAsync(userId.Value);
        return View(documents);
    }

    /// <summary>
    /// Formularz przesyłania dokumentu.
    /// </summary>
    public IActionResult Upload()
    {
        ViewBag.DocumentTypes = GetDocumentTypeSelectList();
        return View();
    }

    /// <summary>
    /// Zapisuje przesłany dokument.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file, KycDocumentType documentType)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("file", "Proszę wybrać plik.");
            ViewBag.DocumentTypes = GetDocumentTypeSelectList();
            return View();
        }

        try
        {
            await _kycService.UploadDocumentAsync(userId.Value, file, documentType);
            TempData["KycMessage"] = "Dokument został przesłany. Oczekuje na weryfikację.";
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.DocumentTypes = GetDocumentTypeSelectList();
            return View();
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Podgląd pliku dokumentu (tylko dla właściciela).
    /// </summary>
    public async Task<IActionResult> File(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var document = await _kycService.GetDocumentByIdAsync(id);
        if (document == null || document.UserId != userId.Value)
            return NotFound();

        var path = _kycService.GetStoragePath(document);
        if (!System.IO.File.Exists(path))
            return NotFound();

        return PhysicalFile(path, document.ContentType, document.FileName);
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }

    private static SelectList GetDocumentTypeSelectList()
    {
        var types = Enum.GetValues<KycDocumentType>()
            .Select(t => new
            {
                Value = t,
                Text = t switch
                {
                    KycDocumentType.DowodOsobisty => "Dowód osobisty",
                    KycDocumentType.Paszport => "Paszport",
                    KycDocumentType.PrawoJazdy => "Prawo jazdy",
                    KycDocumentType.Inny => "Inny dokument",
                    _ => t.ToString()
                }
            })
            .ToList();

        return new SelectList(types, "Value", "Text");
    }
}
