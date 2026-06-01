using CasinoRoyale.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CasinoRoyale.ViewComponents;

public class BalanceViewComponent : ViewComponent
{
    private readonly Automaty _db;

    public BalanceViewComponent(Automaty db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userIdClaim = UserClaimsPrincipal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
        {
            return Content("0.00");
        }

        var wallet = await _db.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet == null)
        {
            return Content("0.00");
        }

        var displayValue = wallet.BalanceReal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        return Content(displayValue);
    }
}
