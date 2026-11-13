using ESportskiCentar.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ESportskiCentar.Areas.Identity.Pages.Account;
using Microsoft.Extensions.Logging;
using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace ESportskiCentar.Tests
{
    [TestClass]
    public class LoginModelTests
    {
        private (LoginModel Model,
            Mock<SignInManager<Korisnik>> SignInManager,
            Mock<ILogger<LoginModel>> Logger,
            Mock<IAuthenticationService> Autentifikacija) NapraviOkruzenje(
                bool prijavljen = false,
                string referer = null)
        {
            var store = new Mock<IUserStore<Korisnik>>();
            store.As<IUserEmailStore<Korisnik>>();

            var userManager = new Mock<UserManager<Korisnik>>(
                store.Object, null, null, null, null, null, null, null, null);

            var signInManager = new Mock<SignInManager<Korisnik>>(
                userManager.Object,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<Korisnik>>().Object,
                null, null, null, null);

            signInManager
                .Setup(s => s.GetExternalAuthenticationSchemesAsync())
                .ReturnsAsync(new List<AuthenticationScheme>());

            var logger = new Mock<ILogger<LoginModel>>();
            var model = new LoginModel(signInManager.Object, logger.Object);

            model.Input = new LoginModel.InputModel
            {
                Email = "amar@test.ba",
                Password = "Lozinka1",
                RememberMe = true
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

            var urlHelper = new Mock<IUrlHelper>();
            urlHelper.Setup(u => u.Content("~/")).Returns("/");

            model.PageContext = new PageContext
            {
                HttpContext = httpKontekst
            };

            model.Url = urlHelper.Object;

            return (model, signInManager, logger, autentifikacija);
        }

        private void PostaviRezultatPrijave(
            Mock<SignInManager<Korisnik>> signInManager,
            IdentitySignInResult rezultat)
        {
            signInManager
                .Setup(s => s.PasswordSignInAsync(
                    "amar@test.ba", "Lozinka1", true, false))
                .ReturnsAsync(rezultat);
        }

        private void ProvjeriLogiranje(
            Mock<ILogger<LoginModel>> logger,
            LogLevel nivo)
        {
            logger.Verify(l => l.Log(
                nivo,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        private List<string> PorukeValidacije(object model)
        {
            var greske = new List<ValidationResult>();

            Validator.TryValidateObject(
                model,
                new ValidationContext(model),
                greske,
                validateAllProperties: true);

            return greske.Select(g => g.ErrorMessage).ToList();
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
        [TestCategory("Mock")]
        public async Task OnGet_KorisnikVecPrijavljen_NeUcitavaVanjskePrijave()
        {
            var okruzenje = NapraviOkruzenje(
                prijavljen: true,
                referer: "https://localhost/Termin");

            await okruzenje.Model.OnGetAsync();

            okruzenje.SignInManager.Verify(
                s => s.GetExternalAuthenticationSchemesAsync(),
                Times.Never);
        }

        [TestMethod]
        public async Task OnGet_ReturnUrlJeZadan_ZadrzavaZadaniReturnUrl()
        {
            var okruzenje = NapraviOkruzenje();

            await okruzenje.Model.OnGetAsync("/Termin/Index");

            Assert.AreEqual("/Termin/Index", okruzenje.Model.ReturnUrl);
        }

        [TestMethod]
        public async Task OnGet_NemaErrorMessage_ModelStateJeValidan()
        {
            var okruzenje = NapraviOkruzenje();

            await okruzenje.Model.OnGetAsync();

            Assert.IsTrue(okruzenje.Model.ModelState.IsValid);
        }

        [TestMethod]
        public async Task OnGet_PostojiErrorMessage_DodajeGaUModelState()
        {
            var okruzenje = NapraviOkruzenje();
            okruzenje.Model.ErrorMessage = "Sesija je istekla.";

            await okruzenje.Model.OnGetAsync();

            Assert.AreEqual(
                "Sesija je istekla.",
                okruzenje.Model.ModelState[string.Empty].Errors[0].ErrorMessage);
        }


        [TestMethod]
        [TestCategory("Mock")]
        public async Task OnPost_ModelStateNijeValidan_VracaPageResultBezPokusajaPrijave()
        {
            var okruzenje = NapraviOkruzenje();
            okruzenje.Model.ModelState.AddModelError(
                "Email",
                "Email adresa je obavezna za prijavu.");

            var rezultat = await okruzenje.Model.OnPostAsync();

            Assert.IsInstanceOfType<PageResult>(rezultat);

            okruzenje.SignInManager.Verify(
                s => s.PasswordSignInAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()),
                Times.Never);
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task OnPost_UspjesnaPrijava_PreusmjeravaNaReturnUrl()
        {
            var okruzenje = NapraviOkruzenje();
            PostaviRezultatPrijave(
                okruzenje.SignInManager,
                IdentitySignInResult.Success);

            var rezultat = await okruzenje.Model.OnPostAsync("/Termin/Index")
                as LocalRedirectResult;

            Assert.AreEqual("/Termin/Index", rezultat.Url);
        }

        [TestMethod]
        public async Task OnPost_UspjesnaPrijavaBezReturnUrl_PreusmjeravaNaPocetnuStranicu()
        {
            var okruzenje = NapraviOkruzenje();
            PostaviRezultatPrijave(
                okruzenje.SignInManager,
                IdentitySignInResult.Success);

            var rezultat = await okruzenje.Model.OnPostAsync()
                as LocalRedirectResult;

            Assert.AreEqual("/", rezultat.Url);
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task OnPost_UspjesnaPrijava_LogujeInformaciju()
        {
            var okruzenje = NapraviOkruzenje();
            PostaviRezultatPrijave(
                okruzenje.SignInManager,
                IdentitySignInResult.Success);

            await okruzenje.Model.OnPostAsync();

            ProvjeriLogiranje(okruzenje.Logger, LogLevel.Information);
        }

        [TestMethod]
        public async Task OnPost_NeispravniPodaci_VracaPageResult()
        {
            var okruzenje = NapraviOkruzenje();
            PostaviRezultatPrijave(
                okruzenje.SignInManager,
                IdentitySignInResult.Failed);

            var rezultat = await okruzenje.Model.OnPostAsync();

            Assert.IsInstanceOfType<PageResult>(rezultat);
        }

        [TestMethod]
        public async Task OnPost_NeispravniPodaci_DodajeGreskuUModelState()
        {
            var okruzenje = NapraviOkruzenje();
            PostaviRezultatPrijave(
                okruzenje.SignInManager,
                IdentitySignInResult.Failed);

            await okruzenje.Model.OnPostAsync();

            Assert.AreEqual(
                "Neispravan email ili lozinka.",
                okruzenje.Model.ModelState[string.Empty].Errors[0].ErrorMessage);
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task OnPost_ValidanModel_PrijavuPokusavaBezZakljucavanjaNaloga()
        {
            var okruzenje = NapraviOkruzenje();
            PostaviRezultatPrijave(
                okruzenje.SignInManager,
                IdentitySignInResult.Success);

            await okruzenje.Model.OnPostAsync();

            okruzenje.SignInManager.Verify(
                s => s.PasswordSignInAsync(
                    "amar@test.ba",
                    "Lozinka1",
                    true,
                    false),
                Times.Once);
        }

        [TestMethod]
        public async Task OnPost_ZabranjenaPrijava_VracaPageSaGreskom()
        {
            var okruzenje = NapraviOkruzenje();
            PostaviRezultatPrijave(
                okruzenje.SignInManager,
                IdentitySignInResult.NotAllowed);

            await okruzenje.Model.OnPostAsync();

            Assert.IsFalse(okruzenje.Model.ModelState.IsValid);
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task OnPost_NalogJeZakljucan_LogujeUpozorenje()
        {
            var okruzenje = NapraviOkruzenje();
            PostaviRezultatPrijave(
                okruzenje.SignInManager,
                IdentitySignInResult.LockedOut);

            await okruzenje.Model.OnPostAsync();

            ProvjeriLogiranje(okruzenje.Logger, LogLevel.Warning);
        }

        [TestMethod]
        public async Task OnPost_TrebaDvofaznaAutentifikacija_PreusmjeravaNaLoginWith2fa()
        {
            var okruzenje = NapraviOkruzenje();
            PostaviRezultatPrijave(
                okruzenje.SignInManager,
                IdentitySignInResult.TwoFactorRequired);

            var rezultat = await okruzenje.Model.OnPostAsync("/Termin/Index")
                as RedirectToPageResult;

            Assert.AreEqual("./LoginWith2fa", rezultat.PageName);
        }

        [TestMethod]
        [TestCategory("Izuzeci")]
        public async Task OnPost_SignInManagerBacaIzuzetak_IzuzetakSePropagira()
        {
            var okruzenje = NapraviOkruzenje();

            okruzenje.SignInManager
                .Setup(s => s.PasswordSignInAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
                .ThrowsAsync(new InvalidOperationException("Baza nije dostupna."));

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => okruzenje.Model.OnPostAsync());
        }
    }
}
