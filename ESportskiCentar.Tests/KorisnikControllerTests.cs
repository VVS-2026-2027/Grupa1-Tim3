
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

        private Korisnik NapraviKorisnika(string ime, string prezime)
        {
            return new Korisnik
            {
                ime = ime,
                prezime = prezime
            };
        }

        // Postavljanje korisnika u UserManager mock
        private void PostaviKorisnike(Mock<UserManager<Korisnik>> userManager)
        {
            var korisnici = new List<Korisnik>
            {
                NapraviKorisnika( "Amar", "Hodžić"),
                NapraviKorisnika( "Lejla", "Kovač"),
                NapraviKorisnika( "Ana", "Marić"),
                NapraviKorisnika( "Marko", "Petrović"),
                NapraviKorisnika( "Đorđe", "Ćosić")
            };

            userManager.Setup(m => m.Users)
                .Returns(korisnici.AsQueryable());
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
    }
}
