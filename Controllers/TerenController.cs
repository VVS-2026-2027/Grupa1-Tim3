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
    [Authorize(Roles = "Administrator")]
    public class TerenController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TerenController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Teren
        public async Task<IActionResult> Index()
        {
            return View(await _context.Tereni.ToListAsync());
        }

        // GET: Teren/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teren = await _context.Tereni
                .FirstOrDefaultAsync(m => m.id == id);
            if (teren == null)
            {
                return NotFound();
            }

            return View(teren);
        }

        // GET: Teren/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Teren/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id,naziv,sport")] Teren teren)
        {
            if (ModelState.IsValid)
            {
                _context.Add(teren);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(teren);
        }

        // GET: Teren/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teren = await _context.Tereni.FindAsync(id);
            if (teren == null)
            {
                return NotFound();
            }
            return View(teren);
        }

        // POST: Teren/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id,naziv,sport")] Teren teren)
        {
            if (id != teren.id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(teren);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TerenExists(teren.id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(teren);
        }

        // GET: Teren/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teren = await _context.Tereni
                .FirstOrDefaultAsync(m => m.id == id);
            if (teren == null)
            {
                return NotFound();
            }

            return View(teren);
        }

        // POST: Teren/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var teren = await _context.Tereni.FindAsync(id);
            if (teren != null)
            {
                _context.Tereni.Remove(teren);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TerenExists(int id)
        {
            return _context.Tereni.Any(e => e.id == id);
        }
    }
}
