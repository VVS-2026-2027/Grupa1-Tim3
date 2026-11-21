
using ESportskiCentar.Controllers;
using ESportskiCentar.Data;
using ESportskiCentar.Models;
using ESportskiCentar.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System.Net;

namespace ESportskiCentar.Tests
{
    [TestClass]
    public class RezervacijaControllerOstaleMetodeTests
    {
        private static ApplicationDbContext NapraviKontekst()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(opcije);
        }

        private static RezervacijaController NapraviKontroler(
            ApplicationDbContext context,
            string korisnikId = "korisnik1",
            string? uloga = null)
        {
            var servis = new EmailService(new HttpClient(), null!);
            var kontroler = new RezervacijaController(context, servis);

            var tvrdnje = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, korisnikId)
            };

            if (uloga != null)
                tvrdnje.Add(new Claim(ClaimTypes.Role, uloga));

            var identitet = new ClaimsIdentity(tvrdnje, "Test");

            kontroler.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identitet)
                }
            };

            return kontroler;
        }

        private static async Task<Rezervacija> DodajRezervaciju(
            ApplicationDbContext context,
            string korisnikId = "korisnik1",
            DateTime? datum = null)
        {
            if (!await context.Users.AnyAsync(k => k.Id == korisnikId))
            {
                context.Users.Add(new Korisnik
                {
                    Id = korisnikId,
                    UserName = korisnikId,
                    ime = "Test",
                    prezime = "Korisnik"
                });
            }

            var teren = new Teren
            {
                naziv = "Testni teren",
                sport = Sport.FUDBAL
            };

            context.Tereni.Add(teren);
            await context.SaveChangesAsync();

            var termin = new Termin
            {
                datum = datum ?? DateTime.Now.AddDays(7),
                cijena = 100,
                terenID = teren.id,
                rezervisan = true
            };

            context.Termini.Add(termin);
            await context.SaveChangesAsync();

            var rezervacija = new Rezervacija
            {
                korisnikID = korisnikId,
                terminID = termin.id,
                status = Status.CEKANJE,
                konacnaCijena = 100,
                vrijemeRezervacije = DateTime.Now
            };

            context.Rezervacije.Add(rezervacija);
            await context.SaveChangesAsync();

            return rezervacija;
        }

        // INDEX

        [TestMethod]
        public async Task Index_PraznaBaza_VracaPraznuListu()
        {
            using var context = NapraviKontekst();
            var kontroler = NapraviKontroler(context);

            var rezultat = await kontroler.Index(null, null, null, null)
                as ViewResult;

            Assert.IsNotNull(rezultat);

            var lista = rezultat.Model as List<Rezervacija>;

            Assert.IsNotNull(lista);
            Assert.AreEqual(0, lista.Count);
        }

        [TestMethod]
        public async Task Index_NeispravanRasponDatuma_VracaGresku()
        {
            using var context = NapraviKontekst();
            var kontroler = NapraviKontroler(context);

            var od = DateTime.Today.AddDays(10);
            var doDatuma = DateTime.Today.AddDays(5);

            var rezultat = await kontroler.Index(
                od, doDatuma, null, null) as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.IsFalse(kontroler.ModelState.IsValid);

            var lista = rezultat.Model as List<Rezervacija>;

            Assert.IsNotNull(lista);
            Assert.AreEqual(0, lista.Count);
        }

        [TestMethod]
        public async Task Index_ObicanKorisnik_VidiSamoSvojeRezervacije()
        {
            using var context = NapraviKontekst();

            await DodajRezervaciju(context, "korisnik1");
            await DodajRezervaciju(context, "korisnik2");

            var kontroler = NapraviKontroler(context, "korisnik1");

            var rezultat = await kontroler.Index(
                null, null, null, null) as ViewResult;

            Assert.IsNotNull(rezultat);

            var lista = rezultat.Model as List<Rezervacija>;

            Assert.IsNotNull(lista);
            Assert.AreEqual(1, lista.Count);
            Assert.AreEqual("korisnik1", lista[0].korisnikID);
        }

        [TestMethod]
        public async Task Index_Administrator_VidiSveRezervacije()
        {
            using var context = NapraviKontekst();

            await DodajRezervaciju(context, "korisnik1");
            await DodajRezervaciju(context, "korisnik2");

            var kontroler = NapraviKontroler(
                context, "admin", "Administrator");

            var rezultat = await kontroler.Index(
                null, null, null, null) as ViewResult;

            Assert.IsNotNull(rezultat);

            var lista = rezultat.Model as List<Rezervacija>;

            Assert.IsNotNull(lista);
            Assert.AreEqual(2, lista.Count);
        }

        [TestMethod]
        public async Task Index_Radnik_VidiSveRezervacije()
        {
            using var context = NapraviKontekst();

            await DodajRezervaciju(context, "korisnik1");
            await DodajRezervaciju(context, "korisnik2");

            var kontroler = NapraviKontroler(
                context, "radnik", "Radnik");

            var rezultat = await kontroler.Index(
                null, null, null, null) as ViewResult;

            Assert.IsNotNull(rezultat);

            var lista = rezultat.Model as List<Rezervacija>;

            Assert.IsNotNull(lista);
            Assert.AreEqual(2, lista.Count);
        }

        [TestMethod]
        public async Task Index_Filteri_VracajuOdgovarajucuRezervaciju()
        {
            using var context = NapraviKontekst();

            var datum = DateTime.Today.AddDays(7);

            await DodajRezervaciju(context, "korisnik1", datum);
            await DodajRezervaciju(context, "korisnik1",
                DateTime.Today.AddDays(15));

            var kontroler = NapraviKontroler(context);

            var rezultat = await kontroler.Index(
                datum.AddDays(-1),
                datum.AddDays(1),
                Sport.FUDBAL,
                Status.CEKANJE) as ViewResult;

            Assert.IsNotNull(rezultat);

            var lista = rezultat.Model as List<Rezervacija>;

            Assert.IsNotNull(lista);
            Assert.AreEqual(1, lista.Count);
        }

        [TestMethod]
        public async Task Index_ProsaoTermin_MijenjaStatusRezervacije()
        {
            using var context = NapraviKontekst();

            var rezervacija = await DodajRezervaciju(
                context, "korisnik1", DateTime.Now.AddDays(-3));

            var kontroler = NapraviKontroler(context);

            await kontroler.Index(null, null, null, null);

            var sacuvana = await context.Rezervacije
                .FindAsync(rezervacija.id);

            Assert.IsNotNull(sacuvana);
            Assert.AreEqual(Status.IZVRSENA, sacuvana.status);
        }

        // CREATE GET

        [TestMethod]
        public async Task CreateGet_NullTerminId_RedirectNaTermin()
        {
            using var context = NapraviKontekst();
            var kontroler = NapraviKontroler(context);

            var rezultat = await kontroler.Create((int?)null)
                as RedirectToActionResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Index", rezultat.ActionName);
            Assert.AreEqual("Termin", rezultat.ControllerName);
        }

        [TestMethod]
        public async Task CreateGet_NepostojeciTermin_VracaNotFound()
        {
            using var context = NapraviKontekst();
            var kontroler = NapraviKontroler(context);

            var rezultat = await kontroler.Create((int?)999);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task CreateGet_ZauzetTermin_VracaNotFound()
        {
            using var context = NapraviKontekst();

            var rezervacija = await DodajRezervaciju(context);

            var kontroler = NapraviKontroler(context);

            var rezultat = await kontroler.Create(
                (int?)rezervacija.terminID);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task CreateGet_SlobodanTermin_VracaRezervaciju()
        {
            using var context = NapraviKontekst();

            var rezervacija = await DodajRezervaciju(context);
            var termin = await context.Termini.FindAsync(
                rezervacija.terminID);

            Assert.IsNotNull(termin);

            termin.rezervisan = false;
            await context.SaveChangesAsync();

            var kontroler = NapraviKontroler(context);

            var rezultat = await kontroler.Create((int?)termin.id)
                as ViewResult;

            Assert.IsNotNull(rezultat);

            var model = rezultat.Model as Rezervacija;

            Assert.IsNotNull(model);
            Assert.AreEqual(termin.id, model.terminID);
        }

        // DELETE GET

        [TestMethod]
        public async Task Delete_NullId_VracaNotFound()
        {
            using var context = NapraviKontekst();
            var kontroler = NapraviKontroler(context);

            var rezultat = await kontroler.Delete(null);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Delete_NepostojeciId_VracaNotFound()
        {
            using var context = NapraviKontekst();
            var kontroler = NapraviKontroler(context);

            var rezultat = await kontroler.Delete(999);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Delete_TudjaRezervacija_VracaForbid()
        {
            using var context = NapraviKontekst();

            var rezervacija = await DodajRezervaciju(
                context, "korisnik2");

            var kontroler = NapraviKontroler(
                context, "korisnik1");

            var rezultat = await kontroler.Delete(rezervacija.id);

            Assert.IsInstanceOfType<ForbidResult>(rezultat);
        }

        [TestMethod]
        public async Task Delete_SvojaRezervacija_VracaView()
        {
            using var context = NapraviKontekst();

            var rezervacija = await DodajRezervaciju(context);

            var kontroler = NapraviKontroler(context);

            var rezultat = await kontroler.Delete(rezervacija.id)
                as ViewResult;

            Assert.IsNotNull(rezultat);

            var model = rezultat.Model as Rezervacija;

            Assert.IsNotNull(model);
            Assert.AreEqual(rezervacija.id, model.id);
        }

        // DELETE CONFIRMED

        [TestMethod]
        public async Task DeleteConfirmed_NepostojeciId_VracaNotFound()
        {
            using var context = NapraviKontekst();
            var kontroler = NapraviKontroler(context);

            var rezultat = await kontroler.DeleteConfirmed(999);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task DeleteConfirmed_TudjaRezervacija_VracaForbid()
        {
            using var context = NapraviKontekst();

            var rezervacija = await DodajRezervaciju(
                context, "korisnik2");

            var kontroler = NapraviKontroler(
                context, "korisnik1");

            var rezultat = await kontroler.DeleteConfirmed(
                rezervacija.id);

            Assert.IsInstanceOfType<ForbidResult>(rezultat);

            Assert.AreEqual(1,
                await context.Rezervacije.CountAsync());
        }

        [TestMethod]
        public async Task DeleteConfirmed_PrekasnoOtkazivanje_VracaView()
        {
            using var context = NapraviKontekst();

            var rezervacija = await DodajRezervaciju(
                context, "korisnik1",
                DateTime.Now.AddHours(1));

            var kontroler = NapraviKontroler(context);

            var rezultat = await kontroler.DeleteConfirmed(
                rezervacija.id) as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Delete", rezultat.ViewName);
            Assert.IsFalse(kontroler.ModelState.IsValid);

            Assert.AreEqual(1,
                await context.Rezervacije.CountAsync());
        }

        [TestMethod]
        public async Task DeleteConfirmed_IspravnoOtkazivanje_BriseRezervaciju()
        {
            using var context = NapraviKontekst();

            var rezervacija = await DodajRezervaciju(context);

            var kontroler = NapraviKontroler(context);

            var rezultat = await kontroler.DeleteConfirmed(
                rezervacija.id);

            Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);

            Assert.AreEqual(0,
                await context.Rezervacije.CountAsync());

            var termin = await context.Termini
                .FindAsync(rezervacija.terminID);

            Assert.IsNotNull(termin);
            Assert.IsFalse(termin.rezervisan);
        }

        [TestMethod]
        public async Task DeleteConfirmed_PostojecaNotifikacija_BriseNotifikaciju()
        {
            using var context = NapraviKontekst();

            var rezervacija = await DodajRezervaciju(context);

            context.Notifikacije.Add(new Notifikacija
            {
                rezervacijaID = rezervacija.id,
                vrijemeSlanja = DateTime.Now.AddDays(1),
                poslana = false
            });

            await context.SaveChangesAsync();

            var kontroler = NapraviKontroler(context);

            await kontroler.DeleteConfirmed(rezervacija.id);

            Assert.AreEqual(0,
                await context.Notifikacije.CountAsync());
        }

        [TestMethod]
        public async Task DeleteConfirmed_Administrator_MozeOtkazatiPredTermin()
        {
            using var context = NapraviKontekst();

            var rezervacija = await DodajRezervaciju(
                context, "korisnik1",
                DateTime.Now.AddHours(1));

            var kontroler = NapraviKontroler(
                context, "korisnik1", "Administrator");

            var rezultat = await kontroler.DeleteConfirmed(
                rezervacija.id);

            Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);

            Assert.AreEqual(0,
                await context.Rezervacije.CountAsync());
        }

        [TestMethod]
        public async Task DeleteConfirmed_Radnik_MozeOtkazatiPredTermin()
        {
            using var context = NapraviKontekst();

            var rezervacija = await DodajRezervaciju(
                context, "korisnik1",
                DateTime.Now.AddHours(1));

            var kontroler = NapraviKontroler(
                context, "korisnik1", "Radnik");

            var rezultat = await kontroler.DeleteConfirmed(
                rezervacija.id);

            Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);

            Assert.AreEqual(0,
                await context.Rezervacije.CountAsync());
        }


        // CREATE POST - nepostojeci termin

        [TestMethod]
        public async Task CreatePost_NepostojeciTermin_VracaNotFound()
        {
            using var context = NapraviKontekst();
            var kontroler = NapraviKontroler(context);

            var rezervacija = new Rezervacija
            {
                terminID = 999
            };

            var rezultat = await kontroler.Create(rezervacija);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
            Assert.AreEqual(0, await context.Rezervacije.CountAsync());
        }


        // CREATE POST - zauzet termin

        [TestMethod]
        public async Task CreatePost_ZauzetTermin_VracaViewSaGreskom()
        {
            using var context = NapraviKontekst();

            var postojeca = await DodajRezervaciju(context);
            var kontroler = NapraviKontroler(context);

            var rezervacija = new Rezervacija
            {
                terminID = postojeca.terminID
            };

            var rezultat = await kontroler.Create(rezervacija);

            Assert.IsInstanceOfType<ViewResult>(rezultat);
            Assert.IsFalse(kontroler.ModelState.IsValid);

            Assert.AreEqual(1, await context.Rezervacije.CountAsync());
        }


        // CREATE POST - automatski popust

        [TestMethod]
        [DataRow(0, 80.0)]
        [DataRow(5, 100.0)]
        public async Task CreatePost_AutomatskiPopust_IspravnoRacunaCijenu(
            int potrebanBroj, double ocekivanaCijena)
        {
            using var context = NapraviKontekst();

            var postojeca = await DodajRezervaciju(context);

            var termin = await context.Termini.FindAsync(
                postojeca.terminID);

            Assert.IsNotNull(termin);

            context.Rezervacije.Remove(postojeca);
            termin.rezervisan = false;

            context.Popusti.Add(new Popust
            {
                potrebanBrojRezervacija = potrebanBroj,
                procenat = 20
            });

            await context.SaveChangesAsync();

            var kontroler = NapraviKontroler(context);

            var rezervacija = new Rezervacija
            {
                terminID = termin.id
            };

            var rezultat = await kontroler.Create(rezervacija);

            Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);

            var sacuvana = await context.Rezervacije.SingleAsync();

            Assert.AreEqual(
                ocekivanaCijena,
                sacuvana.konacnaCijena,
                0.001);

            Assert.AreEqual(
                ocekivanaCijena < 100,
                sacuvana.primjenjenPopust);
        }


        // DELETE CONFIRMED - slanje emaila

        [TestMethod]
        [DataRow(202)]
        [DataRow(400)]
        public async Task DeleteConfirmed_SlanjeMaila_BriseRezervaciju(
            int httpStatus)
        {
            using var context = NapraviKontekst();

            var rezervacija = await DodajRezervaciju(context);

            var korisnik = await context.Users.FindAsync(
                rezervacija.korisnikID);

            Assert.IsNotNull(korisnik);

            korisnik.Email = "korisnik@primjer.ba";

            await context.SaveChangesAsync();

            var handler = new Mock<HttpMessageHandler>();

            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(
                    new HttpResponseMessage((HttpStatusCode)httpStatus));

            using var client = new HttpClient(handler.Object);

            var podaci = new Dictionary<string, string?>
            {
                ["Brevo:ApiKey"] = "test-key",
                ["Brevo:FromEmail"] = "centar@primjer.ba",
                ["Brevo:FromName"] = "Test centar"
            };

            var konfiguracija = new ConfigurationBuilder()
                .AddInMemoryCollection(podaci)
                .Build();

            var servis = new EmailService(client, konfiguracija);

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

            var rezultat = await kontroler.DeleteConfirmed(
                rezervacija.id);

            Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);

            Assert.AreEqual(
                0,
                await context.Rezervacije.CountAsync());

            var termin = await context.Termini.FindAsync(
                rezervacija.terminID);

            Assert.IsNotNull(termin);
            Assert.IsFalse(termin.rezervisan);

            handler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }


        [TestMethod]
        [DataRow("", false)]
        [DataRow("SPORT10", true)]
        public async Task CreatePost_PopustIKupon_SaljeEmail(
            string kod, bool imaKupon)
        {
            using var context = NapraviKontekst();

            // Koristimo postojecu pomocnu metodu
            var postojeca = await DodajRezervaciju(context);

            var termin = await context.Termini.FindAsync(
                postojeca.terminID);

            Assert.IsNotNull(termin);

            // Oslobadjamo termin za novu rezervaciju
            context.Rezervacije.Remove(postojeca);
            termin.rezervisan = false;

            var korisnik = await context.Users.FindAsync(
                postojeca.korisnikID);

            Assert.IsNotNull(korisnik);

            korisnik.Email = "test@primjer.ba";

            // Automatski popust od 20%
            context.Popusti.Add(new Popust
            {
                potrebanBrojRezervacija = 0,
                procenat = 20
            });

            await context.SaveChangesAsync();

            // Simuliramo uspjesno slanje emaila
            var handler = new Mock<HttpMessageHandler>();

            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Accepted));

            using var client = new HttpClient(handler.Object);

            var podaci = new Dictionary<string, string?>
            {
                ["Brevo:ApiKey"] = "test-key",
                ["Brevo:FromEmail"] = "centar@primjer.ba",
                ["Brevo:FromName"] = "Test centar"
            };

            var konfiguracija = new ConfigurationBuilder()
                .AddInMemoryCollection(podaci)
                .Build();

            var servis = new EmailService(client, konfiguracija);

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

            Assert.IsTrue(sacuvana.primjenjenPopust);
            Assert.IsTrue(sacuvana.konacnaCijena < termin.cijena);

            var notifikacija = await context.Notifikacije.SingleAsync();

            Assert.IsTrue(notifikacija.poslana);

            handler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }


    }
}
