using ESportskiCentar.Controllers;
using ESportskiCentar.Data;
using ESportskiCentar.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace ESportskiCentar.Tests
{
    [TestClass]
    public class TerenControllerTests
    {
        // napravi bazu u RAMu za testove
        private static ApplicationDbContext NapraviKontekst()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            return new ApplicationDbContext(opcije);
        }

        private static TerenController NapraviKontroler(ApplicationDbContext kontekst)
        { 
            return new TerenController(kontekst);
        }
 
        private static async Task<Teren> DodajTeren(ApplicationDbContext kontekst)
        {
            var teren = new Teren { naziv = "Testni teren", sport = Sport.FUDBAL };
            kontekst.Tereni.Add(teren);
            await kontekst.SaveChangesAsync();
            return teren;
        }

        // zamjenski objekat koji simulira greske koje bi bacao EFC prilikom snimanja izmjena u bazi
        private class KontekstSaIzuzetkom : ApplicationDbContext
        {
            public bool BaciIzuzetak { get; set; }

            public KontekstSaIzuzetkom(DbContextOptions<ApplicationDbContext> opcije): base(opcije) { }

            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                if (BaciIzuzetak)
                    throw new DbUpdateConcurrencyException("Simulirana konkurentna izmjena.");
                return base.SaveChangesAsync(cancellationToken);
            }
        }
        // index testovi
        [TestMethod]
        public async Task Index_PraznaBaza_VracaPraznuListu()
        {
            // using znaci koristi ovaj objekat dok traje metoda, a na kraju oslobodi njegove resurse
            using var kontekst = NapraviKontekst();
            var rezultat = await NapraviKontroler(kontekst).Index() as ViewResult;
            Assert.IsNotNull(rezultat);
            Assert.AreEqual(0, ((List<Teren>)rezultat.Model).Count);
        }

        [TestMethod]
        public async Task Index_DvaTerena_VracaOba()
        {
            using var kontekst = NapraviKontekst();
            await DodajTeren(kontekst);
            kontekst.Tereni.Add(new Teren { naziv = "Drugi teren", sport = Sport.FUDBAL });
            await kontekst.SaveChangesAsync();
            var rezultat = await NapraviKontroler(kontekst).Index() as ViewResult;
            Assert.IsNotNull(rezultat);
            var tereni = (List<Teren>)rezultat.Model;
            Assert.AreEqual(2, tereni.Count);
            Assert.IsTrue(tereni.Any(t => t.naziv == "Testni teren"));
            Assert.IsTrue(tereni.Any(t => t.naziv == "Drugi teren"));
        }

        // details testovi
        [TestMethod]
        public async Task Details_NullId_VracaNotFound()
        {
            var rezultat = await NapraviKontroler(null).Details(null);
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Details_NepostojeciId_VracaNotFound()
        {
            using var kontekst = NapraviKontekst();
            var rezultat = await NapraviKontroler(kontekst).Details(999);
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Details_PostojeciId_VracaTeren()
        {
            using var kontekst = NapraviKontekst();
            var teren = await DodajTeren(kontekst);
            var rezultat = await NapraviKontroler(kontekst).Details(teren.id) as ViewResult;
            Assert.IsNotNull(rezultat);
            var model = rezultat.Model as Teren;
            Assert.IsNotNull(model);
            Assert.AreEqual(teren.id, model.id);
            Assert.AreEqual("Testni teren", model.naziv);
        }

        // create testovi
        [TestMethod]
        public void CreateGet_VracaView()
        {
            Assert.IsInstanceOfType<ViewResult>(NapraviKontroler(null).Create());
        }

        [TestMethod]
        public async Task CreatePost_ValidanModel_DodajeTerenIPreusmjerava()
        {
            using var kontekst = NapraviKontekst();
            var rezultat = await NapraviKontroler(kontekst).Create(new Teren { naziv = "Novi teren", sport = Sport.FUDBAL });
            Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
            Assert.AreEqual("Index", ((RedirectToActionResult)rezultat).ActionName);
            // SingleAsync() vraca tacno 1 zapis
            var snimljen = await kontekst.Tereni.SingleAsync();
            Assert.AreEqual("Novi teren", snimljen.naziv);
            Assert.AreEqual(Sport.FUDBAL, snimljen.sport);
        }

        [TestMethod]
        public async Task CreatePost_NevalidanModel_VracaViewINeSnima()
        {
            using var kontekst = NapraviKontekst();
            var kontroler = NapraviKontroler(kontekst);
            kontroler.ModelState.AddModelError("naziv", "Neispravan naziv");
            var teren = new Teren { naziv = "", sport = Sport.FUDBAL };
            var rezultat = await kontroler.Create(teren) as ViewResult;
            Assert.IsNotNull(rezultat);
            Assert.AreSame(teren, rezultat.Model);
            Assert.AreEqual(0, await kontekst.Tereni.CountAsync());
        }

        // edit get testovi
        [TestMethod]
        public async Task EditGet_NepostojeciId_VracaNotFound()
        {
            using var kontekst = NapraviKontekst();
            Assert.IsInstanceOfType<NotFoundResult>(
                await NapraviKontroler(kontekst).Edit(999));
        }

        [TestMethod]
        public async Task EditGet_PostojeciId_VracaTeren()
        {
            using var kontekst = NapraviKontekst();
            var teren = await DodajTeren(kontekst);
            var rezultat = await NapraviKontroler(kontekst).Edit(teren.id) as ViewResult;
            Assert.IsNotNull(rezultat);
            Assert.AreEqual(teren.id, ((Teren)rezultat.Model).id);
        }

        // edit post testovi
        [TestMethod]
        public async Task EditPost_RazlicitiId_VracaNotFound()
        {
            var rezultat = await NapraviKontroler(null).Edit(2,new Teren { id = 1, naziv = "Teren", sport = Sport.FUDBAL });
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task EditPost_ValidanModel_AzuriraTeren()
        {
            using var kontekst = NapraviKontekst();
            var teren = await DodajTeren(kontekst);
            int id = teren.id;
            // konekst nece pratit vise onaj stari objekat jer ce se desiti kolizija dva ce imati isti ID
            kontekst.ChangeTracker.Clear(); 
            var rezultat = await NapraviKontroler(kontekst).Edit(id, new Teren { id = id, naziv = "Izmijenjeni teren", sport = Sport.FUDBAL });
            Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
            Assert.AreEqual("Index", ((RedirectToActionResult)rezultat).ActionName);
            var sacuvani = await kontekst.Tereni.SingleAsync(t => t.id == id);
            Assert.AreEqual("Izmijenjeni teren", sacuvani.naziv);
        }

        [TestMethod]
        public async Task EditPost_NevalidanModel_NeAzuriraTeren()
        {
            using var kontekst = NapraviKontekst();
            var teren = await DodajTeren(kontekst);
            var id = teren.id;
            var kontroler = NapraviKontroler(kontekst);
            kontroler.ModelState.AddModelError("naziv", "Neispravan naziv");
            var izmijenjeni = new Teren { id = id, naziv = "Izmijenjeni teren", sport = Sport.FUDBAL };
            var rezultat = await kontroler.Edit(id, izmijenjeni) as ViewResult;
            Assert.IsNotNull(rezultat);
            Assert.AreSame(izmijenjeni, rezultat.Model);
            var sacuvani = await kontekst.Tereni.SingleAsync(t => t.id == id);
            Assert.AreEqual("Testni teren", sacuvani.naziv);
        }

        // edit post testovi, koristen zamjenski objekat
        [TestMethod]
        public async Task EditPost_IzuzetakITerenNePostoji_VracaNotFound()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            using var kontekst = new KontekstSaIzuzetkom(opcije);
            kontekst.BaciIzuzetak = true;
            var rezultat = await NapraviKontroler(kontekst).Edit(999, new Teren { id = 999, naziv = "Nepostojeci teren", sport = Sport.FUDBAL });
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task EditPost_IzuzetakITerenPostoji_PonovoBacaIzuzetak()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            using var kontekst = new KontekstSaIzuzetkom(opcije);
            var teren = await DodajTeren(kontekst);
            int id = teren.id;
            kontekst.ChangeTracker.Clear();
            kontekst.BaciIzuzetak = true;
            var kontroler = NapraviKontroler(kontekst);
            await Assert.ThrowsExactlyAsync<DbUpdateConcurrencyException>(async () =>
                await kontroler.Edit(id, new Teren { id = id, naziv = "Izmijenjeni teren", sport = Sport.FUDBAL }));
        }

        // delete get testovi
        [TestMethod]
        public async Task DeleteGet_NullId_VracaNotFound()
        {
            Assert.IsInstanceOfType<NotFoundResult>( await NapraviKontroler(null).Delete(null));
        }

        [TestMethod]
        public async Task DeleteGet_NepostojeciId_VracaNotFound()
        {
            using var kontekst = NapraviKontekst();
            Assert.IsInstanceOfType<NotFoundResult>(await NapraviKontroler(kontekst).Delete(999));
        }

        [TestMethod]
        public async Task DeleteGet_PostojeciId_VracaTerenINeBriseGa()
        {
            using var kontekst = NapraviKontekst();
            var teren = await DodajTeren(kontekst);
            var rezultat = await NapraviKontroler(kontekst).Delete(teren.id) as ViewResult;
            Assert.IsNotNull(rezultat);
            Assert.AreEqual(teren.id, ((Teren)rezultat.Model).id);
            Assert.AreEqual(1, await kontekst.Tereni.CountAsync());
        }

        // delete post testovi
        [TestMethod]
        public async Task DeleteConfirmed_PostojeciTeren_BriseIPreusmjerava()
        {
            using var kontekst = NapraviKontekst();
            var teren = await DodajTeren(kontekst);
            var rezultat = await NapraviKontroler(kontekst).DeleteConfirmed(teren.id);
            Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
            Assert.AreEqual("Index", ((RedirectToActionResult)rezultat).ActionName);
            Assert.AreEqual(0, await kontekst.Tereni.CountAsync());
        }

        [TestMethod]
        public async Task DeleteConfirmed_NepostojeciTeren_NeBriseOstale()
        {
            using var kontekst = NapraviKontekst();
            var teren = await DodajTeren(kontekst);
            var rezultat = await NapraviKontroler(kontekst).DeleteConfirmed(999);
            Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
            Assert.AreEqual("Index", ((RedirectToActionResult)rezultat).ActionName);
            Assert.AreEqual(1, await kontekst.Tereni.CountAsync());
            Assert.AreEqual(teren.id, (await kontekst.Tereni.SingleAsync()).id);
        }
    }
}
