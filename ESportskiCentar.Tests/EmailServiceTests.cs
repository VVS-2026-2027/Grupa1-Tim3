
using ESportskiCentar.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System.Net;

namespace ESportskiCentar.Tests
{
    [TestClass]
    public class EmailServiceTests
    {
        private static IConfiguration NapraviKonfiguraciju(
            string? apiKey = "test-key",
            string? fromEmail = "centar@primjer.ba",
            string? fromName = "Test centar")
        {
            var podaci = new Dictionary<string, string?>
            {
                ["Brevo:ApiKey"] = apiKey,
                ["Brevo:FromEmail"] = fromEmail,
                ["Brevo:FromName"] = fromName
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(podaci)
                .Build();
        }

        private static Mock<HttpMessageHandler> NapraviHandler(
            HttpStatusCode status)
        {
            var handler = new Mock<HttpMessageHandler>();

            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(status));

            return handler;
        }

        [TestMethod]
        public async Task PosaljiMail_IspravniPodaci_SaljePostZahtjev()
        {
            var handler = NapraviHandler(HttpStatusCode.Accepted);
            var client = new HttpClient(handler.Object);
            var konfiguracija = NapraviKonfiguraciju();

            var servis = new EmailService(client, konfiguracija);

            await servis.PosaljiMail(
                "korisnik@primjer.ba",
                "Potvrda rezervacije",
                "Rezervacija je uspjesna.");

            handler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post &&
                    r.RequestUri != null &&
                    r.RequestUri.ToString() ==
                        "https://api.brevo.com/v3/smtp/email" &&
                    r.Headers.Contains("api-key")),
                ItExpr.IsAny<CancellationToken>());
        }

        [TestMethod]
        public async Task PosaljiMail_NeuspjesanHTTPStatus_BacaIzuzetak()
        {
            var handler = NapraviHandler(HttpStatusCode.BadRequest);
            var client = new HttpClient(handler.Object);
            var servis = new EmailService(
                client, NapraviKonfiguraciju());

            await Assert.ThrowsExactlyAsync<HttpRequestException>(
                async () =>
                    await servis.PosaljiMail(
                        "korisnik@primjer.ba",
                        "Naslov",
                        "Sadrzaj"));
        }

        [TestMethod]
        public async Task PosaljiMail_NedostajeApiKey_BacaIzuzetak()
        {
            var handler = NapraviHandler(HttpStatusCode.OK);
            var client = new HttpClient(handler.Object);
            var servis = new EmailService(
                client, NapraviKonfiguraciju(apiKey: null));

            var izuzetak =
                await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                    async () =>
                        await servis.PosaljiMail(
                            "korisnik@primjer.ba",
                            "Naslov",
                            "Sadrzaj"));

            Assert.AreEqual(
                "Brevo API key nije postavljen.",
                izuzetak.Message);
        }

        [TestMethod]
        public async Task PosaljiMail_NedostajeAdresaPosiljaoca_BacaIzuzetak()
        {
            var handler = NapraviHandler(HttpStatusCode.OK);
            var client = new HttpClient(handler.Object);
            var servis = new EmailService(
                client, NapraviKonfiguraciju(fromEmail: null));

            var izuzetak =
                await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                    async () =>
                        await servis.PosaljiMail(
                            "korisnik@primjer.ba",
                            "Naslov",
                            "Sadrzaj"));

            Assert.AreEqual(
                "Brevo sender email nije postavljen.",
                izuzetak.Message);
        }

        [TestMethod]
        public async Task PosaljiMail_BezNazivaPosiljaoca_KoristiPodrazumijevaniNaziv()
        {
            var handler = NapraviHandler(HttpStatusCode.OK);
            var client = new HttpClient(handler.Object);
            var servis = new EmailService(
                client, NapraviKonfiguraciju(fromName: null));

            await servis.PosaljiMail(
                "korisnik@primjer.ba",
                "Naslov",
                "Sadrzaj");

            handler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }
    }
}
