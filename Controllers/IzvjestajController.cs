using ESportskiCentar.Data;
using ESportskiCentar.Helpers;
using ESportskiCentar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ESportskiCentar.Controllers
{
    [Authorize(Roles = "Administrator,Radnik")]
    public class IzvjestajController : Controller
    {
        private readonly ApplicationDbContext _context;

        public IzvjestajController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Izvjestaj/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Izvjestaj/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DateTime datumOd, DateTime datumDo)
        {
            if (datumDo < datumOd)
            {
                ModelState.AddModelError("", "Datum do ne može biti prije datuma od.");
                return View();
            }

            var prijavljeniKorisnikId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var rezervacije = await _context.Rezervacije
                .Include(r => r.Termin)
                .ThenInclude(t => t.Teren)
                .Where(r => r.vrijemeRezervacije.Date >= datumOd.Date &&
                            r.vrijemeRezervacije.Date <= datumDo.Date)
                .ToListAsync();

            // POPRAVKA: Sve se automatski popunjava.
            var izvjestaj = new Izvjestaj
            {
                datumOd = datumOd,
                datumDo = datumDo,
                datumGenerisanja = VrijemeHelper.SadaLokalno(),
                ukupnoRezervacija = rezervacije.Count,
                ukupnaZarada = rezervacije.Sum(r => r.konacnaCijena),
                korisnikID = prijavljeniKorisnikId
            };

            _context.Izvjestaji.Add(izvjestaj);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = izvjestaj.id });
        }

        // GET: Izvjestaj/Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var izvjestaj = await _context.Izvjestaji
                .Include(i => i.Korisnik)
                .FirstOrDefaultAsync(i => i.id == id);

            if (izvjestaj == null)
                return NotFound();

            return View(izvjestaj);
        }

        // GET: Izvjestaj/Index
        public async Task<IActionResult> Index()
        {
            var izvjestaji = await _context.Izvjestaji
                .Include(i => i.Korisnik)
                .OrderByDescending(i => i.datumGenerisanja)
                .ToListAsync();

            return View(izvjestaji);
        }
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var izvjestaj = await _context.Izvjestaji
                .Include(i => i.Korisnik)
                .FirstOrDefaultAsync(i => i.id == id);

            if (izvjestaj == null)
                return NotFound();

            return View(izvjestaj);
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var izvjestaj = await _context.Izvjestaji.FindAsync(id);

            if (izvjestaj != null)
            {
                _context.Izvjestaji.Remove(izvjestaj);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}