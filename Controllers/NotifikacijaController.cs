using ESportskiCentar.Data;
using ESportskiCentar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ESportskiCentar.Controllers
{
    [NonController]
    public class NotifikacijaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotifikacijaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Notifikacija
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Notifikacije
                .Include(n => n.Rezervacija)
                .ThenInclude(r => r.Korisnik);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Notifikacija/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var notifikacija = await _context.Notifikacije
                .Include(n => n.Rezervacija)
                .ThenInclude(r => r.Korisnik)
                .FirstOrDefaultAsync(m => m.id == id);
            if (notifikacija == null)
            {
                return NotFound();
            }

            return View(notifikacija);
        }

        // GET: Notifikacija/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var notifikacija = await _context.Notifikacije
                .Include(n => n.Rezervacija)
                .FirstOrDefaultAsync(m => m.id == id);
            if (notifikacija == null)
            {
                return NotFound();
            }

            return View(notifikacija);
        }

        // POST: Notifikacija/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var notifikacija = await _context.Notifikacije.FindAsync(id);
            if (notifikacija != null)
            {
                _context.Notifikacije.Remove(notifikacija);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool NotifikacijaExists(int id)
        {
            return _context.Notifikacije.Any(e => e.id == id);
        }
    }
}