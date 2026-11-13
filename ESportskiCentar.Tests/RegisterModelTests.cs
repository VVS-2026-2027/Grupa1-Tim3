using ESportskiCentar.Areas.Identity.Pages.Account;
using ESportskiCentar.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace ESportskiCentar.Tests
{
    [TestClass]
    public class RegisterModelTests
    {
        private (RegisterModel Model,
            Mock<IUserStore<Korisnik>> UserStore,
            Mock<UserManager<Korisnik>> UserManager,
            Mock<SignInManager<Korisnik>> SignInManager,
            Mock<ILogger<RegisterModel>> Logger) NapraviOkruzenje(bool prijavljen = false, string referer = null)
        {
            var userStore = new Mock<IUserStore<Korisnik>>();
            userStore.As<IUserEmailStore<Korisnik>>();

            var userManager = new Mock<UserManager<Korisnik>>(
                userStore.Object, null, null, null, null, null, null, null, null);

            userManager.Setup(m => m.SupportsUserEmail).Returns(true);

            var signInManager = new Mock<SignInManager<Korisnik>>(
                userManager.Object,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<Korisnik>>().Object,
                null, null, null, null);

            signInManager.Setup(s => s.GetExternalAuthenticationSchemesAsync())
                .ReturnsAsync(new List<AuthenticationScheme>());

            var logger = new Mock<ILogger<RegisterModel>>();

            var model = new RegisterModel(
                userManager.Object,
                userStore.Object,
                signInManager.Object,
                logger.Object);

            model.Input = new RegisterModel.InputModel
            {
                Ime = "Amar",
                Prezime = "Hodžić",
                Email = "amar@test.ba",
                Password = "Lozinka1",
                ConfirmPassword = "Lozinka1"
            };

            var autentifikacija = new Mock<IAuthenticationService>();

            autentifikacija
                .Setup(a => a.SignOutAsync(
                    It.IsAny<HttpContext>(),
                    It.IsAny<string>(),
                    It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var servisi = new ServiceCollection()
                .AddSingleton<IAuthenticationService>(autentifikacija.Object)
                .BuildServiceProvider();

            var httpKontekst = new DefaultHttpContext
            {
                RequestServices = servisi
            };

            if (prijavljen)
            {
                var identitet = new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.Name, "test@test.ba") },
                    "TestAutentifikacija");

                httpKontekst.User = new ClaimsPrincipal(identitet);
            }

            httpKontekst.Request.Headers["Referer"] = referer ?? string.Empty;

            model.PageContext = new PageContext
            {
                HttpContext = httpKontekst
            };

            var urlHelper = new Mock<IUrlHelper>();
            urlHelper.Setup(u => u.Content("~/")).Returns("/");
            model.Url = urlHelper.Object;

            return (model, userStore, userManager, signInManager, logger);
        }

        private void PostaviUspjesnuRegistraciju(Mock<UserManager<Korisnik>> userManager,
            Mock<SignInManager<Korisnik>> signInManager)
        {
            userManager
                .Setup(m => m.CreateAsync(It.IsAny<Korisnik>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            userManager
                .Setup(m => m.AddToRoleAsync(It.IsAny<Korisnik>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            signInManager
                .Setup(s => s.SignInAsync(
                    It.IsAny<Korisnik>(), false, It.IsAny<string>()))
                .Returns(Task.CompletedTask);
        }

        private void PostaviNeuspjesnuRegistraciju(
            Mock<UserManager<Korisnik>> userManager,
            params IdentityError[] greske)
        {
            userManager
                .Setup(m => m.CreateAsync(It.IsAny<Korisnik>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(greske));
        }

        private List<string> PorukeValidacije(object model)
        {
            var rezultati = new List<ValidationResult>();

            Validator.TryValidateObject(
                model,
                new ValidationContext(model),
                rezultati,
                validateAllProperties: true);

            return rezultati
                .Select(r => r.ErrorMessage)
                .ToList();
        }
        public static IEnumerable<object[]> RegistracijaIzJsona()
        {
            var putanja = Path.Combine(
                AppContext.BaseDirectory,
                "TestData",
                "RegistracijaPodaci.json");

            using var dokument = JsonDocument.Parse(File.ReadAllText(putanja));

            return dokument.RootElement
                .EnumerateArray()
                .Select(podatak => new object[]
                {
                podatak.GetProperty("ime").GetString(),
                podatak.GetProperty("prezime").GetString(),
                podatak.GetProperty("email").GetString(),
                podatak.GetProperty("password").GetString(),
                podatak.GetProperty("confirmPassword").GetString(),
                podatak.GetProperty("ocekivaneGreske")
                    .EnumerateArray()
                    .Select(greska => greska.GetString())
                    .ToArray()
                })
                .ToArray();
        }

        [TestMethod]
        [TestCategory("DataDriven")]
        [DynamicData(nameof(RegistracijaIzJsona), DynamicDataSourceType.Method)]
        public void InputModel_PodaciIzJsona_VracajuOcekivaneGreskeValidacije(
       string ime,
       string prezime,
       string email,
       string password,
       string confirmPassword,
       string[] ocekivaneGreske)
        {
            var input = new RegisterModel.InputModel
            {
                Ime = ime,
                Prezime = prezime,
                Email = email,
                Password = password,
                ConfirmPassword = confirmPassword
            };

            var greske = PorukeValidacije(input);

            CollectionAssert.AreEquivalent(ocekivaneGreske, greske);
        }

        [TestMethod]
        public void Konstruktor_StoreSaPodrskomZaEmail_KreiraModel()
        {
            var okruzenje = NapraviOkruzenje();

            Assert.IsNotNull(okruzenje.Model);
        }

        [TestMethod]
        [TestCategory("Izuzeci")]
        public void Konstruktor_UserManagerNePodrzavaEmail_BacaNotSupportedException()
        {
            var okruzenje = NapraviOkruzenje();

            okruzenje.UserManager
                .Setup(m => m.SupportsUserEmail)
                .Returns(false);

            var izuzetak = Assert.ThrowsExactly<NotSupportedException>(
                () => new RegisterModel(
                    okruzenje.UserManager.Object,
                    okruzenje.UserStore.Object,
                    okruzenje.SignInManager.Object,
                    okruzenje.Logger.Object));

            StringAssert.Contains(izuzetak.Message, "podrškom za email");
        }


        [TestMethod]
        public async Task OnGet_KorisnikVecPrijavljenSaRefererom_PreusmjeravaNaPrethodnuStranicu()
        {
            var okruzenje = NapraviOkruzenje(
                prijavljen: true,
                referer: "https://localhost/Termin");

            var rezultat = await okruzenje.Model.OnGetAsync() as RedirectResult;

            Assert.AreEqual("https://localhost/Termin", rezultat.Url);
        }

        [TestMethod]
        public async Task OnGet_KorisnikVecPrijavljenBezReferera_PreusmjeravaNaHomeIndex()
        {
            var okruzenje = NapraviOkruzenje(prijavljen: true);

            var rezultat = await okruzenje.Model.OnGetAsync() as RedirectToActionResult;

            Assert.AreEqual("Index", rezultat.ActionName);
            Assert.AreEqual("Home", rezultat.ControllerName);
        }

        [TestMethod]
        public async Task OnGet_AnonimniKorisnik_VracaPageResult()
        {
            var okruzenje = NapraviOkruzenje();

            var rezultat = await okruzenje.Model.OnGetAsync("/Termin/Index");

            Assert.IsInstanceOfType<PageResult>(rezultat);
        }

        [TestMethod]
        public async Task OnGet_AnonimniKorisnik_PostavljaReturnUrl()
        {
            var okruzenje = NapraviOkruzenje();

            await okruzenje.Model.OnGetAsync("/Termin/Index");

            Assert.AreEqual("/Termin/Index", okruzenje.Model.ReturnUrl);
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task OnGet_AnonimniKorisnik_PopunjavaListuVanjskihPrijava()
        {
            var okruzenje = NapraviOkruzenje();

            await okruzenje.Model.OnGetAsync();

            Assert.AreEqual(0, okruzenje.Model.ExternalLogins.Count);
            okruzenje.SignInManager.Verify(
                s => s.GetExternalAuthenticationSchemesAsync(),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task OnPost_ModelStateNijeValidan_VracaPageResultBezKreiranjaKorisnika()
        {
            var okruzenje = NapraviOkruzenje();

            okruzenje.Model.ModelState.AddModelError(
                "Input.Email",
                "Email adresa je obavezna.");

            var rezultat = await okruzenje.Model.OnPostAsync();

            Assert.IsInstanceOfType<PageResult>(rezultat);

            okruzenje.UserManager.Verify(
                m => m.CreateAsync(It.IsAny<Korisnik>(), It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public async Task OnPost_IdentityVracaViseGresaka_SveSeDodajuUModelState()
        {
            var okruzenje = NapraviOkruzenje();

            PostaviNeuspjesnuRegistraciju(
                okruzenje.UserManager,
                new IdentityError { Code = "DuplicateEmail", Description = "x" },
                new IdentityError { Code = "PasswordTooShort", Description = "y" });

            await okruzenje.Model.OnPostAsync();

            Assert.AreEqual(
                2,
                okruzenje.Model.ModelState[string.Empty].Errors.Count);
        }

        [TestMethod]
        public async Task OnPost_IdentityVracaGresku_VracaPageResult()
        {
            var okruzenje = NapraviOkruzenje();

            PostaviNeuspjesnuRegistraciju(
                okruzenje.UserManager,
                new IdentityError { Code = "DuplicateEmail", Description = "x" });

            var rezultat = await okruzenje.Model.OnPostAsync();

            Assert.IsInstanceOfType<PageResult>(rezultat);
        }

        [TestMethod]
        public async Task OnPost_UspjesnaRegistracija_PreusmjeravaNaReturnUrl()
        {
            var okruzenje = NapraviOkruzenje();

            PostaviUspjesnuRegistraciju(
                okruzenje.UserManager,
                okruzenje.SignInManager);

            var rezultat = await okruzenje.Model.OnPostAsync("/Termin/Index")
                as LocalRedirectResult;

            Assert.AreEqual("/Termin/Index", rezultat.Url);
        }

        [TestMethod]
        public async Task OnPost_UspjesnaRegistracijaBezReturnUrl_PreusmjeravaNaPocetnuStranicu()
        {
            var okruzenje = NapraviOkruzenje();

            PostaviUspjesnuRegistraciju(
                okruzenje.UserManager,
                okruzenje.SignInManager);

            var rezultat = await okruzenje.Model.OnPostAsync() as LocalRedirectResult;

            Assert.AreEqual("/", rezultat.Url);
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task OnPost_UspjesnaRegistracija_KreiraKorisnikaSaImenomIPrezimenom()
        {
            var okruzenje = NapraviOkruzenje();

            PostaviUspjesnuRegistraciju(
                okruzenje.UserManager,
                okruzenje.SignInManager);

            await okruzenje.Model.OnPostAsync();

            okruzenje.UserManager.Verify(
                m => m.CreateAsync(
                    It.Is<Korisnik>(k =>
                        k.ime == "Amar" && k.prezime == "Hodžić"),
                    "Lozinka1"),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task OnPost_UspjesnaRegistracija_PostavljaEmailConfirmedNaTrue()
        {
            var okruzenje = NapraviOkruzenje();

            PostaviUspjesnuRegistraciju(
                okruzenje.UserManager,
                okruzenje.SignInManager);

            await okruzenje.Model.OnPostAsync();

            okruzenje.UserManager.Verify(
                m => m.CreateAsync(
                    It.Is<Korisnik>(k => k.EmailConfirmed),
                    It.IsAny<string>()),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task OnPost_UspjesnaRegistracija_PostavljaUserNameUStoreu()
        {
            var okruzenje = NapraviOkruzenje();

            PostaviUspjesnuRegistraciju(
                okruzenje.UserManager,
                okruzenje.SignInManager);

            await okruzenje.Model.OnPostAsync();

            okruzenje.UserStore.Verify(
                s => s.SetUserNameAsync(
                    It.IsAny<Korisnik>(),
                    "amar@test.ba",
                    CancellationToken.None),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task OnPost_UspjesnaRegistracija_PostavljaEmailUEmailStoreu()
        {
            var okruzenje = NapraviOkruzenje();

            PostaviUspjesnuRegistraciju(
                okruzenje.UserManager,
                okruzenje.SignInManager);

            await okruzenje.Model.OnPostAsync();

            okruzenje.UserStore
                .As<IUserEmailStore<Korisnik>>()
                .Verify(
                    s => s.SetEmailAsync(
                        It.IsAny<Korisnik>(),
                        "amar@test.ba",
                        CancellationToken.None),
                    Times.Once);
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task OnPost_UspjesnaRegistracija_DodjeljujeRoluKorisnik()
        {
            var okruzenje = NapraviOkruzenje();

            PostaviUspjesnuRegistraciju(
                okruzenje.UserManager,
                okruzenje.SignInManager);

            await okruzenje.Model.OnPostAsync();

            okruzenje.UserManager.Verify(
                m => m.AddToRoleAsync(It.IsAny<Korisnik>(), "Korisnik"),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("Izuzeci")]
        public async Task OnPost_UserManagerBacaIzuzetak_IzuzetakSePropagira()
        {
            var okruzenje = NapraviOkruzenje();

            okruzenje.UserManager
                .Setup(m => m.CreateAsync(
                    It.IsAny<Korisnik>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Baza nije dostupna."));

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => okruzenje.Model.OnPostAsync());
        }

    }
}
