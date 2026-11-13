using ESportskiCentar.Controllers;
using ESportskiCentar.Data;
using ESportskiCentar.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using System.Xml.Linq;

namespace ESportskiCentar.Tests
{
    [TestClass]
    public class IzvjestajControllerTests
    {
        private IzvjestajController NapraviKontroler(ApplicationDbContext kontekst, string korisnikId = "test-korisnik")
        {
            var kontroler = new IzvjestajController(kontekst);

            var identitet = new ClaimsIdentity(
                new[]
                {
                new Claim(ClaimTypes.NameIdentifier, korisnikId)
                },
                "Test");

            kontroler.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identitet)
                }
            };

            return kontroler;
        }

        private Korisnik NapraviKorisnika()
        {
            return new Korisnik
            {
                Id = "test-korisnik",
                UserName = "testkorisnik",
                ime = "Test",
                prezime = "Testić"
            };
        }

        private async Task DodajRezervacije(ApplicationDbContext kontekst)
        {
            var teren = new Teren
            {
                naziv = "Testni teren",
                sport = Sport.FUDBAL
            };

            kontekst.Tereni.Add(teren);
            await kontekst.SaveChangesAsync();

            var termin1 = new Termin
            {
                datum = new DateTime(2026, 1, 10),
                cijena = 50,
                terenID = teren.id,
                rezervisan = true
            };

            var termin2 = new Termin
            {
                datum = new DateTime(2026, 1, 20),
                cijena = 75,
                terenID = teren.id,
                rezervisan = true
            };

            var termin3 = new Termin
            {
                datum = new DateTime(2026, 2, 10),
                cijena = 100,
                terenID = teren.id,
                rezervisan = true
            };

            kontekst.Termini.AddRange(termin1, termin2, termin3);
            await kontekst.SaveChangesAsync();

            kontekst.Rezervacije.AddRange(
                new Rezervacija
                {
                    terminID = termin1.id,
                    Termin = termin1,
                    korisnikID = "test-korisnik",
                    vrijemeRezervacije = new DateTime(2026, 1, 10),
                    konacnaCijena = 50,
                    status = Status.IZVRSENA
                },
                new Rezervacija
                {
                    terminID = termin2.id,
                    Termin = termin2,
                    korisnikID = "test-korisnik",
                    vrijemeRezervacije = new DateTime(2026, 1, 20),
                    konacnaCijena = 75,
                    status = Status.IZVRSENA
                },
                new Rezervacija
                {
                    terminID = termin3.id,
                    Termin = termin3,
                    korisnikID = "test-korisnik",
                    vrijemeRezervacije = new DateTime(2026, 2, 10),
                    konacnaCijena = 100,
                    status = Status.IZVRSENA
                }
            );

            await kontekst.SaveChangesAsync();
        }

        private async Task DodajIzvjestaj(ApplicationDbContext kontekst, int id, DateTime datumGenerisanja)
        {
            kontekst.Izvjestaji.Add(new Izvjestaj
            {
                id = id,
                korisnikID = "test-korisnik",
                datumOd = new DateTime(2026, 1, 1),
                datumDo = new DateTime(2026, 1, 31),
                datumGenerisanja = datumGenerisanja,
                ukupnoRezervacija = 2,
                ukupnaZarada = 125
            });

            await kontekst.SaveChangesAsync();
        }

        public static IEnumerable<object[]> PodaciRasponiDatuma()
        {
            string putanja = Path.Combine(
                AppContext.BaseDirectory, "TestData", "datumi.csv");

            return File.ReadAllLines(putanja)
                .Skip(1)
                .Select(red =>
                {
                    var dijelovi = red.Split(',');

                    return new object[]
                    {
                DateTime.Parse(dijelovi[0]),
                DateTime.Parse(dijelovi[1])
                    };
                });
        }

        [TestMethod]
        [DynamicData(nameof(PodaciRasponiDatuma), DynamicDataSourceType.Method)]
        public async Task Create_NevalidanRasponIzCsv_VracaViewSaGreskomUModelState(
    DateTime datumOd, DateTime datumDo)
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            var kontroler = new IzvjestajController(context);

            var rezultat = await kontroler.Create(datumOd, datumDo);

            var poruka = kontroler.ModelState[string.Empty]?
                .Errors.FirstOrDefault()?.ErrorMessage;

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));

            Assert.AreEqual(
                "Datum do ne može biti prije datuma od.",
                poruka);
        }

        [TestMethod]
        [DynamicData(nameof(PodaciRasponiDatuma), DynamicDataSourceType.Method)]
        public async Task Create_NevalidanRasponIzCsv_NeSnimaIzvjestaj(
    DateTime datumOd, DateTime datumDo)
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            var kontroler = new IzvjestajController(context);

            await kontroler.Create(datumOd, datumDo);

            Assert.AreEqual(0, await context.Izvjestaji.CountAsync());
        }

        // Create

        [TestMethod]
        public async Task Create_SnimaOcekivaniBrojRezervacija()
        {

            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
           .UseInMemoryDatabase(Guid.NewGuid().ToString())
           .Options;

            using var kontekst = new ApplicationDbContext(opcije);

            kontekst.Users.Add(NapraviKorisnika());
            await kontekst.SaveChangesAsync();

            await DodajRezervacije(kontekst);

            var kontroler = NapraviKontroler(kontekst);

            await kontroler.Create(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
            var snimljen = await kontekst.Izvjestaji.SingleAsync();

            Assert.AreEqual(2, snimljen.ukupnoRezervacija);
        }

        [TestMethod]
        public async Task Create_SnimaOcekivanuZaradu()
        {

            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

            using var kontekst = new ApplicationDbContext(opcije);

            kontekst.Users.Add(NapraviKorisnika());
            await kontekst.SaveChangesAsync();

            await DodajRezervacije(kontekst);

            var kontroler = NapraviKontroler(kontekst);

            await kontroler.Create(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

            var snimljen = await kontekst.Izvjestaji.SingleAsync();

            Assert.AreEqual(125.0, snimljen.ukupnaZarada, 0.001);
        }

        [TestMethod]
        public void CreateGet_VracaViewResult()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var kontekst = new ApplicationDbContext(opcije);
            var kontroler = NapraviKontroler(kontekst);

            var rezultat = kontroler.Create();

            Assert.IsInstanceOfType<ViewResult>(rezultat);
        }

        [TestMethod]
        public async Task CreatePost_PreusmjeravaNaDetails()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var kontekst = new ApplicationDbContext(opcije);

            kontekst.Users.Add(NapraviKorisnika());
            await kontekst.SaveChangesAsync();

            await DodajRezervacije(kontekst);

            var kontroler = NapraviKontroler(kontekst);

            var rezultat = await kontroler.Create(
                new DateTime(2026, 1, 1),
                new DateTime(2026, 1, 31)) as RedirectToActionResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Details", rezultat.ActionName);
            Assert.AreEqual((await kontekst.Izvjestaji.SingleAsync()).id, rezultat.RouteValues["id"]);
        }
        [TestMethod]
        public async Task CreatePost_PopunjavaPodatkeIzvjestaja()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var kontekst = new ApplicationDbContext(opcije);

            kontekst.Users.Add(NapraviKorisnika());
            await kontekst.SaveChangesAsync();

            await DodajRezervacije(kontekst);

            var kontroler = NapraviKontroler(kontekst);

            await kontroler.Create(
                new DateTime(2026, 1, 1),
                new DateTime(2026, 1, 31));

            var snimljen = await kontekst.Izvjestaji.SingleAsync();

            Assert.AreEqual("test-korisnik", snimljen.korisnikID);
            Assert.AreEqual(new DateTime(2026, 1, 1), snimljen.datumOd);
            Assert.AreEqual(new DateTime(2026, 1, 31), snimljen.datumDo);
        }

        [TestMethod]
        public async Task CreatePost_BezRezervacijaUBazi_SnimaIzvjestajSaNulama()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var kontekst = new ApplicationDbContext(opcije);

            kontekst.Users.Add(NapraviKorisnika());
            await kontekst.SaveChangesAsync();

            var kontroler = NapraviKontroler(kontekst);

            await kontroler.Create(
                new DateTime(2026, 1, 1),
                new DateTime(2026, 1, 31));

            var snimljen = await kontekst.Izvjestaji.SingleAsync();

            Assert.AreEqual(0, snimljen.ukupnoRezervacija);
            Assert.AreEqual(0.0, snimljen.ukupnaZarada);
        }

        //Details

        [TestMethod]
        public async Task Details_IdJeNull_VracaNotFound()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var kontekst = new ApplicationDbContext(opcije);
            var kontroler = NapraviKontroler(kontekst);

            var rezultat = await kontroler.Details(null);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Details_IzvjestajNePostoji_VracaNotFound()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var kontekst = new ApplicationDbContext(opcije);
            var kontroler = NapraviKontroler(kontekst);

            var rezultat = await kontroler.Details(99);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Details_IzvjestajPostoji_VracaViewSaIzvjestajem()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var kontekst = new ApplicationDbContext(opcije);

            kontekst.Users.Add(NapraviKorisnika());
            await kontekst.SaveChangesAsync();

            await DodajIzvjestaj(kontekst, 5, new DateTime(2026, 2, 1));

            var kontroler = NapraviKontroler(kontekst);

            var rezultat = await kontroler.Details(5) as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(5, ((Izvjestaj)rezultat.Model).id);
        }

        [TestMethod]
        public async Task Details_IzvjestajPostoji_UcitavaKorisnikaKojiJeGenerisaoIzvjestaj()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var kontekst = new ApplicationDbContext(opcije);

            kontekst.Users.Add(NapraviKorisnika());
            await kontekst.SaveChangesAsync();

            await DodajIzvjestaj(kontekst, 5, new DateTime(2026, 2, 1));

            var kontroler = NapraviKontroler(kontekst);

            var rezultat = await kontroler.Details(5) as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Testić", ((Izvjestaj)rezultat.Model).Korisnik.prezime);
        }

        //Index
        [TestMethod]
        public async Task Index_BezIzvjestaja_VracaPrazanModel()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var kontekst = new ApplicationDbContext(opcije);
            var kontroler = NapraviKontroler(kontekst);

            var rezultat = await kontroler.Index() as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(0, ((List<Izvjestaj>)rezultat.Model).Count);
        }

        [TestMethod]
        public async Task Index_ViseIzvjestaja_SortiraIhOdNajnovijegPremaNajstarijem()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var kontekst = new ApplicationDbContext(opcije);

            kontekst.Users.Add(NapraviKorisnika());
            await kontekst.SaveChangesAsync();

            await DodajIzvjestaj(kontekst, 1, new DateTime(2026, 1, 1));
            await DodajIzvjestaj(kontekst, 2, new DateTime(2026, 3, 1));
            await DodajIzvjestaj(kontekst, 3, new DateTime(2026, 2, 1));

            var kontroler = NapraviKontroler(kontekst);

            var rezultat = await kontroler.Index() as ViewResult;

            var idevi = ((List<Izvjestaj>)rezultat.Model)
                .Select(i => i.id)
                .ToList();

            CollectionAssert.AreEqual(new[] { 2, 3, 1 }, idevi);
        }

    }
}
