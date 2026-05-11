using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CasinoRoyale.Controllers
{
    [Authorize]
    public class BlackjackViewController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Games/Blackjack.cshtml");
        }
    }
}
