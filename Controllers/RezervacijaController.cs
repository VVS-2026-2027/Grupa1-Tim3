using ESportskiCentar.Data;
using ESportskiCentar.Helpers;
using ESportskiCentar.Models;
using ESportskiCentar.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace ESportskiCentar.Controllers
{
    [Authorize]
    public class RezervacijaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
            
        public RezervacijaController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index(DateTime? datumOd, DateTime? datumDo, Sport? sport, Status? status)
        {
            if (datumOd.HasValue && datumDo.HasValue && datumDo < datumOd)
            {
                ModelState.AddModelError(string.Empty, "Datum kraja ne može biti prije datuma početka!");
                return View(new List<Rezervacija>());
            }

            // Automatsko ažuriranje statusa u IZVRSENA za termine koji su prošli
            var prosleRezervacije = await _context.Rezervacije
                .Include(r => r.Termin)
                .Where(r => r.status == Status.CEKANJE && r.Termin.datum < VrijemeHelper.SadaLokalno())
                .ToListAsync();

            foreach (var r in prosleRezervacije)
            {
                r.status = Status.IZVRSENA;
            }
            await _context.SaveChangesAsync();

            var korisnikId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var rezervacije = _context.Rezervacije
                .Include(r => r.Korisnik)
                .Include(r => r.Termin)
                .ThenInclude(t => t.Teren)
                .AsQueryable();

            if (!User.IsInRole("Administrator") && !User.IsInRole("Radnik"))
            {
                rezervacije = rezervacije.Where(r => r.korisnikID == korisnikId);
            }

            if (datumOd.HasValue)
            {
                rezervacije = rezervacije.Where(r => r.Termin.datum.Date >= datumOd.Value.Date);
            }

            if (datumDo.HasValue)
            {
                rezervacije = rezervacije.Where(r => r.Termin.datum.Date <= datumDo.Value.Date);
            }

            if (sport.HasValue)
            {
                rezervacije = rezervacije.Where(r => r.Termin.Teren.sport == sport.Value);
            }

            if (status.HasValue)
            {
                rezervacije = rezervacije.Where(r => r.status == status.Value);
            }

            ViewBag.DatumOd = datumOd?.ToString("yyyy-MM-dd");
            ViewBag.DatumDo = datumDo?.ToString("yyyy-MM-dd");
            ViewBag.Sport = sport;
            ViewBag.Status = status;

            return View(await rezervacije
                .OrderByDescending(r => r.vrijemeRezervacije)
                .ToListAsync());
        }

        public async Task<IActionResult> Create(int? terminId)
        {
            if (terminId == null)
                return RedirectToAction("Index", "Termin");

            var termin = await _context.Termini
                .Include(t => t.Teren)
                .FirstOrDefaultAsync(t => t.id == terminId && !t.rezervisan);

            if (termin == null)
                return NotFound();

            var rezervacija = new Rezervacija
            {
                terminID = termin.id,
                Termin = termin
            };

            return View(rezervacija);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("terminID")] Rezervacija rezervacija)
        {
            var korisnikId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var termin = await _context.Termini
                .Include(t => t.Teren)
                .FirstOrDefaultAsync(t => t.id == rezervacija.terminID);

            if (termin == null) return NotFound();

            if (termin.rezervisan)
            {
                ModelState.AddModelError("", "Termin je u međuvremenu zauzet.");
                return View(rezervacija);
            }

            double cijena = termin.cijena;
            bool imaPopust = false;

            int brojRezervacija = await _context.Rezervacije
                .CountAsync(r =>
                    r.korisnikID == korisnikId &&
                    r.vrijemeRezervacije.Month == VrijemeHelper.SadaLokalno().Month &&
                    r.vrijemeRezervacije.Year == VrijemeHelper.SadaLokalno().Year);

            var popust = await _context.Popusti
                .Where(p => brojRezervacija >= p.potrebanBrojRezervacija)
                .OrderByDescending(p => p.procenat)
                .FirstOrDefaultAsync();

            if (popust != null)
            {
                cijena = cijena - (cijena * popust.procenat / 100);
                imaPopust = true;
            }

            rezervacija.korisnikID = korisnikId;
            rezervacija.status = Status.CEKANJE;
            rezervacija.vrijemeRezervacije = VrijemeHelper.SadaLokalno();
            rezervacija.primjenjenPopust = imaPopust;
            rezervacija.konacnaCijena = cijena;

            termin.rezervisan = true;

            _context.Rezervacije.Add(rezervacija);
            await _context.SaveChangesAsync();

            var korisnik = await _context.Users.FindAsync(korisnikId);
            bool mailPoslanUspjesno = false;
            //mail
            if (!string.IsNullOrEmpty(korisnik?.Email))
            {
                try {
                    await _emailService.PosaljiMail(
                        korisnik.Email,
                        "Potvrda rezervacije",
                        $@"
        <h2>Rezervacija je uspješno potvrđena</h2>
        <p><strong>Teren:</strong> {termin.Teren.naziv}</p>
        <p><strong>Sport:</strong> {termin.Teren.sport}</p>
        <p><strong>Datum i vrijeme:</strong> {termin.datum:dd.MM.yyyy. HH:mm}</p>
        <p><strong>Cijena:</strong> {termin.cijena} KM</p>
{(imaPopust// ternarni op
        ? $"<p><strong>Popust:</strong> {popust.procenat}%</p><p><strong>Cijena sa popustom:</strong> {rezervacija.konacnaCijena} KM</p>"
        : "")}
        "
                    );
                    mailPoslanUspjesno = true;
                }catch(Exception ex) 
                {
                    System.Diagnostics.Debug.WriteLine($"Greška pri slanju maila: {ex.Message}");
                }
             }

            //upisivanje u notifikacije
            var notifikacija = new Notifikacija
            {
                rezervacijaID = rezervacija.id,
                vrijemeSlanja = VrijemeHelper.SadaLokalno().AddMinutes(3),
                poslana = mailPoslanUspjesno
            };
            _context.Notifikacije.Add(notifikacija);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var korisnikId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var rezervacija = await _context.Rezervacije
                .Include(r => r.Termin)
                .ThenInclude(t => t.Teren)
                .FirstOrDefaultAsync(r => r.id == id);

            if (rezervacija == null) return NotFound();

            if (rezervacija.korisnikID != korisnikId)
                return Forbid();

            return View(rezervacija);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var rezervacija = await _context.Rezervacije
                .Include(r => r.Korisnik)
                .Include(r => r.Termin)
                .ThenInclude(t => t.Teren)
                .FirstOrDefaultAsync(r => r.id == id);

            if (rezervacija == null) return NotFound();

            var korisnikId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (rezervacija.korisnikID != korisnikId)
                return Forbid();

            if (rezervacija.Termin.datum <= VrijemeHelper.SadaLokalno().AddHours(2) && !User.IsInRole("Administrator") && !User.IsInRole("Radnik"))
            {
                ModelState.AddModelError("", "Otkazivanje više nije moguće.");
                return View("Delete", rezervacija);
            }

            if (rezervacija.Korisnik != null && !string.IsNullOrEmpty(rezervacija.Korisnik.Email))
            {
                try
                {
                    string naslov = "Otkazivanje rezervacije - E-Sportski Centar";
                    string sadrzaj = $@"
                    <h2>Vaša rezervacija je otkazana</h2>
                    <p>Obavještavamo vas da je rezervacija uspješno otkazana za sljedeći termin:</p>
                    <p><strong>Teren:</strong> {rezervacija.Termin.Teren.naziv}</p>
                    <p><strong>Sport:</strong> {rezervacija.Termin.Teren.sport}</p>
                    <p><strong>Datum i vrijeme:</strong> {rezervacija.Termin.datum:dd.MM.yyyy. HH:mm} sati</p>
                    <p>Ukoliko je došlo do greške, slobodno rezervišite novi termin na našoj stranici.</p>";

                    await _emailService.PosaljiMail(rezervacija.Korisnik.Email, naslov, sadrzaj);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Greška pri slanju maila za otkazivanje: {ex.Message}");
                }
            }
            var notifikacije = _context.Notifikacije
                                .Where(n => n.rezervacijaID == rezervacija.id);

            _context.Notifikacije.RemoveRange(notifikacije);

            rezervacija.Termin.rezervisan = false;

            _context.Rezervacije.Remove(rezervacija);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}