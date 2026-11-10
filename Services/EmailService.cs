using System.Net.Http.Json;

namespace ESportskiCentar.Services
{
    public class EmailService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public EmailService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task PosaljiMail(string primaoc, string naslov, string sadrzaj)
        {
            var apiKey = _configuration["Brevo:ApiKey"];
            var fromEmail = _configuration["Brevo:FromEmail"];
            var fromName = _configuration["Brevo:FromName"] ?? "E-Sportski Centar";

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Brevo API key nije postavljen.");

            if (string.IsNullOrWhiteSpace(fromEmail))
                throw new InvalidOperationException("Brevo sender email nije postavljen.");

            var zahtjev = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            zahtjev.Headers.Add("api-key", apiKey);

            zahtjev.Content = JsonContent.Create(new
            {
                sender = new
                {
                    name = fromName,
                    email = fromEmail
                },
                to = new[]
                {
                    new { email = primaoc }
                },
                subject = naslov,
                htmlContent = sadrzaj
            });

            var odgovor = await _httpClient.SendAsync(zahtjev);
            odgovor.EnsureSuccessStatusCode();
        }
    }
}