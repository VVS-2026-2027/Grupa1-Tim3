using ESportskiCentar.Controllers;
using ESportskiCentar.Data;
using ESportskiCentar.Models;
using ESportskiCentar.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Security.Claims;

namespace ESportskiCentar.Tests
{
    [TestClass]
    public class RezervacijaControllerTests
    {
        [TestMethod]
        public async Task Create_IspravanKupon_UmanjujeCijenuRezervacije()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            var korisnik = new Korisnik
            {
                Id = "test-korisnik",
                UserName = "test",
                ime = "Test",
                prezime = "Korisnik"
            };

            var teren = new Teren
            {
                naziv = "Testni teren",
                sport = Sport.FUDBAL
            };

            context.Users.Add(korisnik);
            context.Tereni.Add(teren);
            await context.SaveChangesAsync();

            var termin = new Termin
            {
                datum = DateTime.Today.AddDays(7),
                cijena = 100,
                terenID = teren.id,
                rezervisan = false
            };

            context.Termini.Add(termin);
            await context.SaveChangesAsync();

            var kontroler = new RezervacijaController(
                context,
                new EmailService(new HttpClient(), null!)
            );

            var identitet = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, korisnik.Id)
                },
                "Test"
            );

            kontroler.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identitet)
                }
            };

            var rezervacija = new Rezervacija
            {
                terminID = termin.id,
                uneseniKod = "SPORT10"
            };

            await kontroler.Create(rezervacija);

            var sacuvana = await context.Rezervacije.SingleAsync();

            Assert.AreEqual(90.0, sacuvana.konacnaCijena, 0.001);
            Assert.IsTrue(sacuvana.primjenjenPopust);
        }
    }
}