
using ESportskiCentar.Controllers;
using ESportskiCentar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ESportskiCentar.Tests
{
    [TestClass]
    public class KorisnikControllerTests
    {
        // Kreiranje UserManager mock objekta, jer KorisnikController koristi UserManager, koji ne možemo
        // zamijeniti običnim EF InMemory kontekstom.
        private Mock<UserManager<Korisnik>> NapraviUserManagerMock()
        {
            var store = new Mock<IUserStore<Korisnik>>();

            return new Mock<UserManager<Korisnik>>(
                store.Object,
                Options.Create(new IdentityOptions()),
                new PasswordHasher<Korisnik>(),
                new List<IUserValidator<Korisnik>>(),
                new List<IPasswordValidator<Korisnik>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger<UserManager<Korisnik>>>());
        }

        private Korisnik NapraviKorisnika(string id, string ime, string prezime)
        {
            return new Korisnik
            {
                Id=id,
                ime = ime,
                prezime = prezime
            };
        }

        // Postavljanje korisnika u UserManager mock
        private void PostaviKorisnike(Mock<UserManager<Korisnik>> userManager)
        {
            var korisnici = new List<Korisnik>
            {
                 NapraviKorisnika("id-1", "Amar", "Hodžić"),
                NapraviKorisnika("id-2", "Lejla", "Kovač"),
                NapraviKorisnika("id-3", "Ana", "Marić"),
                NapraviKorisnika("id-4", "Marko", "Petrović"),
                NapraviKorisnika("id-5", "Đorđe", "Ćosić")
            };

            userManager.Setup(m => m.Users)
                .Returns(korisnici.AsQueryable());
        }

        private void PostaviKorisnikaZaPromjenuRole(
    Mock<UserManager<Korisnik>> userManager,
    Korisnik korisnik,
    IList<string> postojeceRole)
        {
            userManager.Setup(m => m.FindByIdAsync(korisnik.Id))
                .ReturnsAsync(korisnik);

            userManager.Setup(m => m.GetRolesAsync(korisnik))
                .ReturnsAsync(postojeceRole);

            userManager.Setup(m => m.RemoveFromRolesAsync(
                    korisnik, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);

            userManager.Setup(m => m.AddToRoleAsync(
                    korisnik, It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
        }
        // Čitanje podataka iz CSV datoteke
        public static IEnumerable<object[]> ValidnePretrage()
        {
            string putanja = Path.Combine(
                AppContext.BaseDirectory,
                "TestData",
                "PretragaKorisnika.csv");

            return File.ReadAllLines(putanja)
                .Skip(1)
                .Where(red => red.Split(';')[1] == "true")
                .Select(red =>
                {
                    var dijelovi = red.Split(';');

                    return new object[]
                    {
                dijelovi[0],
                int.Parse(dijelovi[2])
                    };
                });
        }

        public static IEnumerable<object[]> NevalidnePretrage()
        {
            string putanja = Path.Combine(
                AppContext.BaseDirectory,
                "TestData",
                "PretragaKorisnika.csv");

            return File.ReadAllLines(putanja)
                .Skip(1)
                .Where(red => red.Split(';')[1] == "false")
                .Select(red =>
                {
                    var dijelovi = red.Split(';');

                    return new object[] { dijelovi[0] };
                });
        }

        //Index: data driven CSV regex pretrage

        [TestMethod]
        [TestCategory("DataDriven")]
        [DynamicData(nameof(ValidnePretrage), DynamicDataSourceType.Method)]
        public void Index_ValidnaPretragaIzCsv_VracaOcekivaniBrojKorisnika(
           string pretraga, int ocekivaniBroj)
        {
            var userManager = NapraviUserManagerMock();
            var kontroler = new KorisnikController(userManager.Object);

            PostaviKorisnike(userManager);

            var rezultat = kontroler.Index(pretraga) as ViewResult;

            Assert.AreEqual(
                ocekivaniBroj,
                ((List<Korisnik>)rezultat.Model).Count);
        }

        [TestMethod]
        [TestCategory("DataDriven")]
        [DynamicData(nameof(NevalidnePretrage), DynamicDataSourceType.Method)]
        public void Index_NevalidnaPretragaIzCsv_VracaPraznuListuIGreskuUModelState(
           string pretraga)
        {
            var userManager = NapraviUserManagerMock();
            var kontroler = new KorisnikController(userManager.Object);

            PostaviKorisnike(userManager);

            var rezultat = kontroler.Index(pretraga) as ViewResult;

            Assert.AreEqual(0, ((List<Korisnik>)rezultat.Model).Count);
            Assert.IsFalse(kontroler.ModelState.IsValid);
        }

        [TestMethod]
        [TestCategory("DataDriven")]
        [DynamicData(nameof(NevalidnePretrage), DynamicDataSourceType.Method)]
        public void Index_NevalidnaPretragaIzCsv_PorukaGreskeJeIspravna(
           string pretraga)
        {
            var userManager = NapraviUserManagerMock();
            var kontroler = new KorisnikController(userManager.Object);

            kontroler.Index(pretraga);

            Assert.AreEqual(
                "Pretraga može sadržavati samo slova.",
                kontroler.ModelState["pretraga"].Errors[0].ErrorMessage);
        }

        [TestMethod]
        [TestCategory("DataDriven")]
        [DynamicData(nameof(NevalidnePretrage), DynamicDataSourceType.Method)]
        public void Index_NevalidnaPretragaIzCsv_NeDiraUserManagerNitiPretrazujeBazu(
            string pretraga)
        {
            var userManager = NapraviUserManagerMock();
            var kontroler = new KorisnikController(userManager.Object);

            kontroler.Index(pretraga);

            userManager.VerifyGet(m => m.Users, Times.Never);
        }

        //Index-ostalo

        [TestMethod]
        [DataRow((string)null)]
        [DataRow("")]
        [DataRow("   ")]
        public void Index_PretragaNullPrazanIliSamoRazmaci_VracaSveKorisnike(
            string pretraga)
        {
            var userManager = NapraviUserManagerMock();
            var kontroler = new KorisnikController(userManager.Object);

            PostaviKorisnike(userManager);

            var rezultat = kontroler.Index(pretraga) as ViewResult;

            Assert.AreEqual(5, ((List<Korisnik>)rezultat.Model).Count);
        }

        [TestMethod]
        public void Index_ValidnaPretraga_ZadrzavaPretraguUViewData()
        {
            var userManager = NapraviUserManagerMock();
            var kontroler = new KorisnikController(userManager.Object);

            PostaviKorisnike(userManager);

            var rezultat = kontroler.Index("amar") as ViewResult;

            Assert.AreEqual("amar", rezultat.ViewData["Pretraga"]);
        }

        [TestMethod]
        public void Index_NevalidnaPretraga_ZadrzavaPretraguUViewData()
        {
            var userManager = NapraviUserManagerMock();
            var kontroler = new KorisnikController(userManager.Object);

            var rezultat = kontroler.Index("Amar123") as ViewResult;

            Assert.AreEqual("Amar123", rezultat.ViewData["Pretraga"]);
        }

        [TestMethod]
        public void Index_PretragaPoImenu_VracaTacnoOdgovarajucegKorisnika()
        {
            var userManager = NapraviUserManagerMock();
            var kontroler = new KorisnikController(userManager.Object);

            PostaviKorisnike(userManager);

            var rezultat = kontroler.Index("lejla") as ViewResult;

            Assert.AreEqual(
                "Kovač",
                ((List<Korisnik>)rezultat.Model).Single().prezime);
        }
        //Details
        [TestMethod]
        public async Task Details_IdJeNull_VracaNotFound()
        {
            var userManager = NapraviUserManagerMock();
            var kontroler = new KorisnikController(userManager.Object);

            var rezultat = await kontroler.Details(null);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Details_KorisnikNePostoji_VracaNotFound()
        {
            var userManager = NapraviUserManagerMock();
            var kontroler = new KorisnikController(userManager.Object);

            var rezultat = await kontroler.Details("nepostojeci");

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Details_KorisnikPostoji_VracaViewSaKorisnikom()
        {
            var userManager = NapraviUserManagerMock();
            var kontroler = new KorisnikController(userManager.Object);
            var amar = NapraviKorisnika("id-1", "Amar", "Hodžić");

            userManager.Setup(m => m.FindByIdAsync("id-1"))
                .ReturnsAsync(amar);

            userManager.Setup(m => m.GetRolesAsync(amar))
                .ReturnsAsync((IList<string>)new List<string> { "Radnik" });

            var rezultat = await kontroler.Details("id-1") as ViewResult;

            Assert.AreSame(amar, rezultat.Model);
        }

        //PromijeniRolu(GET)
        [TestMethod]
        public async Task PromijeniRoluGet_IdJeNull_VracaNotFound()
        {
            var userManager = NapraviUserManagerMock();
            var kontroler = new KorisnikController(userManager.Object);

            var rezultat = await kontroler.PromijeniRolu(null);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task PromijeniRoluGet_KorisnikNePostoji_VracaNotFound()
        {
            var userManager = NapraviUserManagerMock();
            var kontroler = new KorisnikController(userManager.Object);

            userManager.Setup(m => m.FindByIdAsync("nepostojeci"))
                .ReturnsAsync((Korisnik)null);

            var rezultat = await kontroler.PromijeniRolu("nepostojeci");

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task PromijeniRoluGet_KorisnikImaRolu_TrenutnaRolaJePrvaRola()
        {
            var userManager = NapraviUserManagerMock();
            var kontroler = new KorisnikController(userManager.Object);
            var amar = NapraviKorisnika("id-1", "Amar", "Hodžić");

            userManager.Setup(m => m.FindByIdAsync("id-1"))
                .ReturnsAsync(amar);

            userManager.Setup(m => m.GetRolesAsync(amar))
                .ReturnsAsync((IList<string>)new List<string> { "Radnik" });

            var rezultat = await kontroler.PromijeniRolu("id-1") as ViewResult;

            Assert.AreEqual("Radnik", rezultat.ViewData["TrenutnaRola"]);
        }

        [TestMethod]
        public async Task PromijeniRoluGet_KorisnikBezRole_TrenutnaRolaJeBezRole()
        {
            var userManager = NapraviUserManagerMock();
            var kontroler = new KorisnikController(userManager.Object);
            var amar = NapraviKorisnika("id-1", "Amar", "Hodžić");

            userManager.Setup(m => m.FindByIdAsync("id-1"))
                .ReturnsAsync(amar);

            userManager.Setup(m => m.GetRolesAsync(amar))
                .ReturnsAsync((IList<string>)new List<string>());

            var rezultat = await kontroler.PromijeniRolu("id-1") as ViewResult;

            Assert.AreEqual("Bez role", rezultat.ViewData["TrenutnaRola"]);
        }

        //PromijeniRolu (POST)

        [TestMethod]
        public async Task PromijeniRoluPost_IdJeNull_VracaNotFound()
        {
            var userManager = NapraviUserManagerMock();
            var kontroler = new KorisnikController(userManager.Object);

            var rezultat = await kontroler.PromijeniRolu(null, "Radnik");

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task PromijeniRoluPost_NovaRolaNull_VracaNotFound()
        {
            var userManager = NapraviUserManagerMock();
            var kontroler = new KorisnikController(userManager.Object);

            var rezultat = await kontroler.PromijeniRolu("id-1", null);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        //mock 


        [TestMethod]
        [TestCategory("Mock")]
        public async Task PromijeniRoluPost_KorisnikImaRolu_UklanjaStareRoleIDodajeNovu()
        {
            var userManager = NapraviUserManagerMock();
            var kontroler = new KorisnikController(userManager.Object);
            var amar = NapraviKorisnika("id-1", "Amar", "Hodžić");

            PostaviKorisnikaZaPromjenuRole(
                userManager,
                amar,
                new List<string> { "Korisnik" });

            await kontroler.PromijeniRolu("id-1", "Radnik");

            userManager.Verify(
                m => m.RemoveFromRolesAsync(amar, It.IsAny<IEnumerable<string>>()),
                Times.Once);

            userManager.Verify(
                m => m.AddToRoleAsync(amar, "Radnik"),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task PromijeniRoluPost_KorisnikBezRole_NeUklanjaRoleAliDodajeNovu()
        {
            var userManager = NapraviUserManagerMock();
            var kontroler = new KorisnikController(userManager.Object);
            var amar = NapraviKorisnika("id-1", "Amar", "Hodžić");

            PostaviKorisnikaZaPromjenuRole(
                userManager,
                amar,
                new List<string>());

            await kontroler.PromijeniRolu("id-1", "Administrator");

            userManager.Verify(
                m => m.RemoveFromRolesAsync(
                    It.IsAny<Korisnik>(),
                    It.IsAny<IEnumerable<string>>()),
                Times.Never);

            userManager.Verify(
                m => m.AddToRoleAsync(amar, "Administrator"),
                Times.Once);
        }

        [TestMethod]
        public async Task PromijeniRoluPost_UserManagerBacaIzuzetak_IzuzetakSePropagira()
        {
            var userManager = NapraviUserManagerMock();
            var kontroler = new KorisnikController(userManager.Object);
            var amar = NapraviKorisnika("id-1", "Amar", "Hodžić");

            PostaviKorisnikaZaPromjenuRole(
                userManager,
                amar,
                new List<string>());

            userManager.Setup(m => m.AddToRoleAsync(amar, "Radnik"))
                .ThrowsAsync(new InvalidOperationException("Rola ne postoji."));

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => kontroler.PromijeniRolu("id-1", "Radnik"));
        }

    }
}
