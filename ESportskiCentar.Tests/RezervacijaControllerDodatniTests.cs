
using ESportskiCentar.Controllers;
using ESportskiCentar.Data;
using ESportskiCentar.Models;
using ESportskiCentar.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System.Net;
using System.Security.Claims;

namespace ESportskiCentar.Tests
{
    [TestClass]
    public class RezervacijaControllerDodatniTests
    {
        [TestMethod]
        [DataRow(202, true)]
        [DataRow(400, false)]
        public async Task CreatePost_SlanjeMaila_BiljeziStatusNotifikacije(
            int httpStatus, bool ocekivanoPoslana)
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            var korisnik = new Korisnik
            {
                Id = "test-korisnik",
                UserName = "test",
                Email = "korisnik@primjer.ba",
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

            var handler = new Mock<HttpMessageHandler>();

            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(
                    new HttpResponseMessage((HttpStatusCode)httpStatus));

            var client = new HttpClient(handler.Object);

            var podaci = new Dictionary<string, string?>
            {
                ["Brevo:ApiKey"] = "test-key",
                ["Brevo:FromEmail"] = "centar@primjer.ba",
                ["Brevo:FromName"] = "Test centar"
            };

            var konfiguracija = new ConfigurationBuilder()
                .AddInMemoryCollection(podaci)
                .Build();

            var emailService = new EmailService(client, konfiguracija);

            var kontroler = new RezervacijaController(
                context, emailService);

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
                terminID = termin.id
            };

            var rezultat = await kontroler.Create(rezervacija);

            Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);

            var sacuvanaRezervacija =
                await context.Rezervacije.SingleAsync();

            var notifikacija =
                await context.Notifikacije.SingleAsync();

            Assert.AreEqual(
                sacuvanaRezervacija.id,
                notifikacija.rezervacijaID);

            Assert.AreEqual(
                ocekivanoPoslana,
                notifikacija.poslana);

            Assert.IsTrue(termin.rezervisan);

            handler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }
    }
}
