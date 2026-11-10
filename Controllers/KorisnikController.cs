using ESportskiCentar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ESportskiCentar.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class KorisnikController : Controller
    {
        private readonly UserManager<Korisnik> _userManager;

        public KorisnikController(UserManager<Korisnik> userManager)
        {
            _userManager = userManager;
        }

        // GET: Korisnik
        // Samo administrator vidi listu korisnika.
        public IActionResult Index(string pretraga)
        {
            if (!string.IsNullOrWhiteSpace(pretraga))
            {
                var provjera = new System.Text.RegularExpressions.Regex(@"^[A-Za-zČčĆćŽžŠšĐđ ]+$");
                if (!provjera.IsMatch(pretraga))
                {
                    ModelState.AddModelError("pretraga", "Pretraga može sadržavati samo slova.");
                    ViewBag.Pretraga = pretraga;
                    return View(new List<Korisnik>());
                }
            }
            var korisnici = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(pretraga))
            {
                var p = pretraga.ToLower();
                korisnici = korisnici.Where(k =>
                    (k.ime + " " + k.prezime).ToLower().Contains(p) ||
                    k.ime.ToLower().Contains(p) ||
                    k.prezime.ToLower().Contains(p));
            }

            ViewBag.Pretraga = pretraga;
            return View(korisnici.ToList());
        }

        // GET: Korisnik/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
                return NotFound();

            var korisnik = await _userManager.FindByIdAsync(id);

            if (korisnik == null)
                return NotFound();

            ViewBag.Role = await _userManager.GetRolesAsync(korisnik);

            return View(korisnik);
        }

        // GET: Korisnik/PromijeniRolu/5
        public async Task<IActionResult> PromijeniRolu(string id)
        {
            if (id == null)
                return NotFound();

            var korisnik = await _userManager.FindByIdAsync(id);

            if (korisnik == null)
                return NotFound();

            var trenutneRole = await _userManager.GetRolesAsync(korisnik);

            ViewBag.TrenutnaRola = trenutneRole.FirstOrDefault() ?? "Bez role";

            return View(korisnik);
        }

        // POST: Korisnik/PromijeniRolu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromijeniRolu(string id, string novaRola)
        {
            if (id == null || string.IsNullOrEmpty(novaRola))
                return NotFound();

            var korisnik = await _userManager.FindByIdAsync(id);

            if (korisnik == null)
                return NotFound();

            var trenutneRole = await _userManager.GetRolesAsync(korisnik);

            if (trenutneRole.Any())
            {
                await _userManager.RemoveFromRolesAsync(korisnik, trenutneRole);
            }

            await _userManager.AddToRoleAsync(korisnik, novaRola);

            return RedirectToAction(nameof(Index));
        }
    }
}