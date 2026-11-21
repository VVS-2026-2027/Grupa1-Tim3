
using ESportskiCentar.Controllers;
using ESportskiCentar.Data;
using ESportskiCentar.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ESportskiCentar.Tests
{
    [TestClass]
    public class NotifikacijaControllerTests
    {
        private static ApplicationDbContext NapraviKontekst()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(opcije);
        }

        private static async Task<Notifikacija> DodajNotifikaciju(
            ApplicationDbContext kontekst)
        {
            var korisnik = new Korisnik
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "test",
                ime = "Test",
                prezime = "Korisnik"
            };

            var teren = new Teren
            {
                naziv = "Testni teren",
                sport = Sport.FUDBAL
            };

            kontekst.Users.Add(korisnik);
            kontekst.Tereni.Add(teren);
            await kontekst.SaveChangesAsync();

            var termin = new Termin
            {
                datum = DateTime.Now.AddDays(7),
                cijena = 100,
                terenID = teren.id,
                rezervisan = true
            };

            kontekst.Termini.Add(termin);
            await kontekst.SaveChangesAsync();

            var rezervacija = new Rezervacija
            {
                korisnikID = korisnik.Id,
                terminID = termin.id,
                konacnaCijena = 100,
                vrijemeRezervacije = DateTime.Now
            };

            kontekst.Rezervacije.Add(rezervacija);
            await kontekst.SaveChangesAsync();

            var notifikacija = new Notifikacija
            {
                rezervacijaID = rezervacija.id,
                vrijemeSlanja = DateTime.Now.AddDays(1),
                poslana = false
            };

            kontekst.Notifikacije.Add(notifikacija);
            await kontekst.SaveChangesAsync();

            return notifikacija;
        }

        [TestMethod]
        public async Task Index_PraznaBaza_VracaPraznuListu()
        {
            using var kontekst = NapraviKontekst();

            var rezultat = await new NotifikacijaController(kontekst)
                .Index() as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(0,
                ((List<Notifikacija>)rezultat.Model!).Count);
        }

        [TestMethod]
        public async Task Index_DvijeNotifikacije_VracaObje()
        {
            using var kontekst = NapraviKontekst();

            await DodajNotifikaciju(kontekst);
            await DodajNotifikaciju(kontekst);

            var rezultat = await new NotifikacijaController(kontekst)
                .Index() as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(2,
                ((List<Notifikacija>)rezultat.Model!).Count);
        }

        [TestMethod]
        public async Task Details_NullId_VracaNotFound()
        {
            using var kontekst = NapraviKontekst();

            var rezultat = await new NotifikacijaController(kontekst)
                .Details(null);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Details_NepostojeciId_VracaNotFound()
        {
            using var kontekst = NapraviKontekst();

            var rezultat = await new NotifikacijaController(kontekst)
                .Details(999);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Details_PostojeciId_VracaNotifikaciju()
        {
            using var kontekst = NapraviKontekst();

            var notifikacija = await DodajNotifikaciju(kontekst);

            var rezultat = await new NotifikacijaController(kontekst)
                .Details(notifikacija.id) as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(notifikacija.id,
                ((Notifikacija)rezultat.Model!).id);
        }

        [TestMethod]
        public async Task Delete_NullId_VracaNotFound()
        {
            using var kontekst = NapraviKontekst();

            var rezultat = await new NotifikacijaController(kontekst)
                .Delete(null);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Delete_NepostojeciId_VracaNotFound()
        {
            using var kontekst = NapraviKontekst();

            var rezultat = await new NotifikacijaController(kontekst)
                .Delete(999);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Delete_PostojeciId_VracaNotifikaciju()
        {
            using var kontekst = NapraviKontekst();

            var notifikacija = await DodajNotifikaciju(kontekst);

            var rezultat = await new NotifikacijaController(kontekst)
                .Delete(notifikacija.id) as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(notifikacija.id,
                ((Notifikacija)rezultat.Model!).id);
        }

        [TestMethod]
        public async Task DeleteConfirmed_PostojeciId_BriseNotifikaciju()
        {
            using var kontekst = NapraviKontekst();

            var notifikacija = await DodajNotifikaciju(kontekst);

            var rezultat = await new NotifikacijaController(kontekst)
                .DeleteConfirmed(notifikacija.id);

            Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);

            Assert.AreEqual(0,
                await kontekst.Notifikacije.CountAsync());
        }

        [TestMethod]
        public async Task DeleteConfirmed_NepostojeciId_PreusmjeravaBezGreske()
        {
            using var kontekst = NapraviKontekst();

            var rezultat = await new NotifikacijaController(kontekst)
                .DeleteConfirmed(999);

            Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);

            Assert.AreEqual("Index",
                ((RedirectToActionResult)rezultat).ActionName);
        }
    }
}
