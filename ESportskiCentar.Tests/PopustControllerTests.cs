using ESportskiCentar.Controllers;
using ESportskiCentar.Data;
using ESportskiCentar.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;


namespace ESportskiCentar.Tests
{
    [TestClass]
    public class PopustControllerTests
    {
        // napravi bazu u RAMu za testove
        private static ApplicationDbContext NapraviKontekst()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            return new ApplicationDbContext(opcije);
        }

        private static PopustController NapraviKontroler(ApplicationDbContext kontekst)
        {
            var kontroler = new PopustController(kontekst);
            kontroler.TempData = new TempDataDictionary(new DefaultHttpContext(), new TestTempDataProvider());
            return kontroler;
        }
        // u kontroleru greska se smjesta u TempData["Greska"] pa moramo napraviti lazni
        private class TestTempDataProvider : ITempDataProvider
        {
            public IDictionary<string, object> LoadTempData(HttpContext context)
            {
                return new Dictionary<string, object>();
            }

            public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
        }

        private static async Task<Popust> DodajPopust(ApplicationDbContext kontekst)
        {
            var popust = new Popust { potrebanBrojRezervacija = 5, procenat = 10 };
            kontekst.Popusti.Add(popust);
            await kontekst.SaveChangesAsync();
            return popust;
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
        // citanje podataka iz CSVa
        public static IEnumerable<object[]> PodaciPopusti()
        {
            var putanja = Path.Combine(AppContext.BaseDirectory, "TestData", "popusti.csv");

            return File.ReadAllLines(putanja)
                .Skip(1)
                .Where(red => !string.IsNullOrWhiteSpace(red))
                .Select(red =>
                {
                    var dijelovi = red.Split(',');
                    return new object[]
                    {
                        double.Parse(dijelovi[0], CultureInfo.InvariantCulture),
                        int.Parse(dijelovi[1]),
                        bool.Parse(dijelovi[2])
                    };
                }).ToList();
        }
        // DATA DRIVEN: provjerava DataAnnotations pravila modela Popust.
        // Ovaj test prolazi samo ako pravila u Popust.cs odgovaraju CSV očekivanjima.
        [TestMethod]
        // [DynamicData()] govori MSTestuu da podatke za test uzima iz metode PodaciPopusti()
        [DynamicData(nameof(PodaciPopusti), DynamicDataSourceType.Method)]
        public void Popust_GranicneVrijednostiIzCsv_ProvjeravaValidaciju(double procenat, int potrebanBrojRezervacija, bool ispravno)
        {
            var popust = new Popust
            {
                procenat = procenat,
                potrebanBrojRezervacija = potrebanBrojRezervacija
            };

            bool validan = Validator.TryValidateObject(popust, new ValidationContext(popust), null, true);
            // provjerava da li je popust validan: ValidationContext daje informacije o objektu,
            // null znaci da ne cuvamo poruke o greskama, a true da provjeravamo sve validacijske
            // atribute svojstava (npr. Range). Metoda vraca true ako je validan, inace false
            Assert.AreEqual(ispravno, validan);
        }
        // index testovi
        [TestMethod]
        public async Task Index_PraznaBaza_VracaPraznuListu()
        {
            using var kontekst = NapraviKontekst();
            var kontroler = NapraviKontroler(kontekst);
            var rezultat = await kontroler.Index() as ViewResult;
            Assert.IsNotNull(rezultat);
            Assert.AreEqual(0, ((List<Popust>)rezultat.Model).Count);
        }

        [TestMethod]
        public async Task Index_DvaPopusta_VracaOba()
        {
            // dodajemo jedan preko pomocne DodajPopust, a jedan direktno u kontekst preko Add
            using var kontekst = NapraviKontekst();
            await DodajPopust(kontekst);
            kontekst.Popusti.Add(new Popust { potrebanBrojRezervacija = 10, procenat = 20 });
            await kontekst.SaveChangesAsync();
            var kontroler = NapraviKontroler(kontekst);
            var rezultat = await kontroler.Index() as ViewResult;
            Assert.IsNotNull(rezultat);
            var popusti = (List<Popust>)rezultat.Model;
            Assert.AreEqual(2, popusti.Count);
            Assert.IsTrue(popusti.Any(p => p.procenat == 10));
            Assert.IsTrue(popusti.Any(p => p.procenat == 20));
        }

        // details testovi 
        [TestMethod]
        public async Task Details_NullId_VracaNotFound()
        {
            var kontroler = new PopustController(null!);
            var rezultat = await kontroler.Details(null);
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
        public async Task Details_PostojeciId_VracaPopust()
        {
            using var kontekst = NapraviKontekst();
            var popust = await DodajPopust(kontekst);
            var rezultat = await NapraviKontroler(kontekst).Details(popust.id) as ViewResult;
            Assert.IsNotNull(rezultat);
            var model = rezultat.Model as Popust;
            Assert.IsNotNull(model);
            Assert.AreEqual(popust.id, model.id);
            Assert.AreEqual(10, model.procenat);
            Assert.AreEqual(5, model.potrebanBrojRezervacija);
        }

        // create get testovi
        [TestMethod]
        public void CreateGet_VracaView()
        {
            Assert.IsInstanceOfType<ViewResult>(NapraviKontroler(null).Create());
        }

        // create post testovi
        [TestMethod]
        public async Task CreatePost_ValidanModel_DodajePopust()
        {
            using var kontekst = NapraviKontekst();
            var kontroler = NapraviKontroler(kontekst);
            var rezultat = await kontroler.Create(new Popust{ potrebanBrojRezervacija = 5, procenat = 10 });
            Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
            // ako je dobro vraca na Index
            Assert.AreEqual("Index", ((RedirectToActionResult)rezultat).ActionName);
            var snimljen = await kontekst.Popusti.SingleAsync();
            Assert.AreEqual(5, snimljen.potrebanBrojRezervacija);
            Assert.AreEqual(10, snimljen.procenat);
        }

        [TestMethod]
        public async Task CreatePost_NevalidanModel_VracaViewINeSnima()
        {
            using var kontekst = NapraviKontekst();
            var kontroler = NapraviKontroler(kontekst);
            // stavimo prije u ModelState gresku i nece uspjeti napraviti popust
            kontroler.ModelState.AddModelError("procenat", "Neispravan procenat");
            var popust = new Popust { potrebanBrojRezervacija = 5, procenat = 10 };
            var rezultat = await kontroler.Create(popust) as ViewResult;
            Assert.IsNotNull(rezultat);
            Assert.AreSame(popust, rezultat.Model);
            Assert.AreEqual(0, await kontekst.Popusti.CountAsync());
            Assert.IsTrue(((string)kontroler.TempData["Greska"]!).Contains("Neispravan procenat"));
        }

        // edit get testovi
        [TestMethod]
        public async Task EditGet_NullId_VracaNotFound()
        {
            var kontroler = NapraviKontroler(null);
            var rezultat = await kontroler.Edit(null);
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task EditGet_NepostojeciId_VracaNotFound()
        {
            using var kontekst = NapraviKontekst();
            var rezultat = await NapraviKontroler(kontekst).Edit(999);
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task EditGet_PostojeciId_VracaPopust()
        {
            using var kontekst = NapraviKontekst();
            var popust = await DodajPopust(kontekst);
            var rezultat = await NapraviKontroler(kontekst).Edit(popust.id) as ViewResult;
            Assert.IsNotNull(rezultat);
            Assert.AreEqual(popust.id, ((Popust)rezultat.Model).id);
        }
        // edit post i edit get razlikujem kada pozivam preko parametara
        // edit post testovi
        [TestMethod]
        public async Task EditPost_RazlicitiId_VracaNotFound()
        {
            var kontroler = NapraviKontroler(null);
            var rezultat = await kontroler.Edit(2, new Popust{ id = 1, potrebanBrojRezervacija = 5, procenat = 10 });
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task EditPost_ValidanModel_AzuriraPopust()
        {
            using var kontekst = NapraviKontekst();
            var popust = await DodajPopust(kontekst);
            int id = popust.id;
            // ChangeTracker pamti koje objekte EFC prati i njihovo stanje i da ne izbrisemo te objekte koje prati
            // doslo bi do konflikta kada bi kontroler zvao Update nad novim objekotm koji ima isti ID kao i ovaj koji prati
            kontekst.ChangeTracker.Clear();
            var kontroler = NapraviKontroler(kontekst);
            var rezultat = await kontroler.Edit(id, new Popust{ id = id, potrebanBrojRezervacija = 15, procenat = 30 });
            Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
            Assert.AreEqual("Index", ((RedirectToActionResult)rezultat).ActionName);
            var sacuvani = await kontekst.Popusti.SingleAsync(p => p.id == id);
            Assert.AreEqual(15, sacuvani.potrebanBrojRezervacija);
            Assert.AreEqual(30, sacuvani.procenat);
        }

        [TestMethod]
        public async Task EditPost_NevalidanModel_NeAzuriraPopust()
        {
            using var kontekst = NapraviKontekst();
            var popust = await DodajPopust(kontekst);
            int id = popust.id;
            // ChangeTracker pamti koje objekte EFC prati i njihovo stanje i da ne izbrisemo te objekte koje prati
            // doslo bi do konflikta kada bi kontroler zvao Update nad novim objekotm koji ima isti ID kao i ovaj koji prati
            kontekst.ChangeTracker.Clear();
            var kontroler = NapraviKontroler(kontekst);
            kontroler.ModelState.AddModelError("procenat", "Neispravan procenat");
            var izmijenjeni = new Popust { id = id, potrebanBrojRezervacija = 15, procenat = 30 };
            // edit nije uspio i vraca View sa objektom izmjenjenog
            var rezultat = await kontroler.Edit(id, izmijenjeni) as ViewResult;
            Assert.IsNotNull(rezultat);
            Assert.AreSame(izmijenjeni, rezultat.Model);
            Assert.IsTrue(((string)kontroler.TempData["Greska"]!).Contains("Neispravan procenat"));
            var sacuvani = await kontekst.Popusti.SingleAsync(p => p.id == id);
            Assert.AreEqual(5, sacuvani.potrebanBrojRezervacija);
            Assert.AreEqual(10, sacuvani.procenat);
        }

        // edit post - zamjenski objekat 
        [TestMethod]
        public async Task EditPost_IzuzetakIPopustNePostoji_VracaNotFound()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            using var kontekst = new KontekstSaIzuzetkom(opcije) { BaciIzuzetak = true };
            var kontroler = NapraviKontroler(kontekst);
            var rezultat = await kontroler.Edit(999, new Popust{ id = 999, potrebanBrojRezervacija = 5, procenat = 10 });
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task EditPost_IzuzetakIPopustPostoji_PonovoBacaIzuzetak()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            using var kontekst = new KontekstSaIzuzetkom(opcije);
            var popust = await DodajPopust(kontekst);
            int id = popust.id;
            kontekst.ChangeTracker.Clear();
            kontekst.BaciIzuzetak = true;
            var kontroler = NapraviKontroler(kontekst);
            await Assert.ThrowsExactlyAsync<DbUpdateConcurrencyException>(async () =>
                await kontroler.Edit(id, new Popust{ id = id, potrebanBrojRezervacija = 15, procenat = 30 }));
        }

        // delete get testovi
        [TestMethod]
        public async Task DeleteGet_NullId_VracaNotFound()
        {
            var rezultat = await NapraviKontroler(null).Delete(null);
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task DeleteGet_NepostojeciId_VracaNotFound()
        {
            using var kontekst = NapraviKontekst();
            var rezultat = await NapraviKontroler(kontekst).Delete(999);
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task DeleteGet_PostojeciId_VracaPopustINeBriseGa()
        {
            using var kontekst = NapraviKontekst();
            var popust = await DodajPopust(kontekst);
            var rezultat = await NapraviKontroler(kontekst).Delete(popust.id) as ViewResult;
            Assert.IsNotNull(rezultat);
            Assert.AreEqual(popust.id, ((Popust)rezultat.Model!).id);
            Assert.AreEqual(1, await kontekst.Popusti.CountAsync());
        }

        // delete post testovi
        [TestMethod]
        public async Task DeleteConfirmed_PostojeciPopust_BriseIPreusmjerava()
        {
            using var kontekst = NapraviKontekst();
            var popust = await DodajPopust(kontekst);
            var rezultat = await NapraviKontroler(kontekst).DeleteConfirmed(popust.id);
            Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
            Assert.AreEqual("Index", ((RedirectToActionResult)rezultat).ActionName);
            Assert.AreEqual(0, await kontekst.Popusti.CountAsync());
        }

        [TestMethod]
        public async Task DeleteConfirmed_NepostojeciPopust_NeBriseOstale()
        {
            using var kontekst = NapraviKontekst();
            var popust = await DodajPopust(kontekst);
            var rezultat = await NapraviKontroler(kontekst).DeleteConfirmed(999);
            Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
            Assert.AreEqual("Index", ((RedirectToActionResult)rezultat).ActionName);
            Assert.AreEqual(1, await kontekst.Popusti.CountAsync());
            Assert.AreEqual(popust.id, (await kontekst.Popusti.SingleAsync()).id);
        }
    }
}
