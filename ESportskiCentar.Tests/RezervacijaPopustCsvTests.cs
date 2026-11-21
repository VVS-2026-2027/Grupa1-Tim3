
using ESportskiCentar.Controllers;
using ESportskiCentar.Data;
using ESportskiCentar.Models;
using ESportskiCentar.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;

namespace ESportskiCentar.Tests
{
    [TestClass]
    public class RezervacijaPopustCsvTests
    {

        public static IEnumerable<object[]> PodaciIzCsv()
        {
            string putanja = Path.Combine(
                AppContext.BaseDirectory,
                "TestData",
                "cijeneRezervacija.csv");

            string[] redovi = File.ReadAllLines(putanja);

            for (int i = 0; i < redovi.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(redovi[i]))
                    continue;

                string[] podaci = redovi[i].Split(';');

                if (podaci[0].Trim() == "cijena")
                    continue;

                yield return new object[]
                {
            double.Parse(podaci[0], CultureInfo.InvariantCulture),
            podaci[1].Trim(),
            double.Parse(podaci[2], CultureInfo.InvariantCulture)
                };
            }
        }


        [TestMethod]
        [DynamicData(nameof(PodaciIzCsv),
            DynamicDataSourceType.Method)]
        public async Task Create_CijenaIzCsv_IspravnoObracunavaPopust(
            double cijena, string kod, double ocekivanaCijena)
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
                cijena = cijena,
                terenID = teren.id,
                rezervisan = false
            };

            context.Termini.Add(termin);
            await context.SaveChangesAsync();

            var servis = new EmailService(new HttpClient(), null!);
            var kontroler = new RezervacijaController(context, servis);

            var identitet = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, korisnik.Id)
                },
                "Test");

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
                uneseniKod = kod
            };

            var rezultat = await kontroler.Create(rezervacija);

            Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);

            var sacuvana = await context.Rezervacije.SingleAsync();

            Assert.AreEqual(
                ocekivanaCijena,
                sacuvana.konacnaCijena,
                0.001);

            Assert.IsTrue(termin.rezervisan);
        }
    }
}
