using ESportskiCentar.Controllers;
using ESportskiCentar.Data;
using ESportskiCentar.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Xml.Linq;
using System.Security.Claims;
namespace ESportskiCentar.Tests
{

    [TestClass]
    public class TerminControllerTests
    {
        public static IEnumerable<object[]> PodaciDatumi()
        {
            string putanja = Path.Combine(
                AppContext.BaseDirectory, "TestData", "datumi.csv");

            return File.ReadAllLines(putanja)
                .Skip(1)
                .Select(red =>
                {
                    var dijelovi = red.Split(',');

                    return new object[]
                    {
                DateTime.Parse(dijelovi[0]),
                DateTime.Parse(dijelovi[1])
                    };
                });
        }
        public static IEnumerable<object[]> PodaciDatumiXML()
        {
            string putanja = Path.Combine(
                AppContext.BaseDirectory, "TestData", "datumi.xml");//putanju nadje

            var dokument = XDocument.Load(putanja);//ucitava

            return dokument.Descendants("Test")//pronalazi svaki test ele
                .Select(t => new object[]
                {
            DateTime.Parse(t.Element("DatumOd")!.Value),
            DateTime.Parse(t.Element("DatumDo")!.Value)
                })
                .ToList();
        }
        [TestMethod]
        [DynamicData(nameof(PodaciDatumi), DynamicDataSourceType.Method)]
        public async Task Index_DatumKrajaPrijePocetka_CSV(DateTime datumOd, DateTime datumDo)
        {
            var kontroler = new TerminController(null);
            var rezultat = await kontroler.Index(datumOd, datumDo, null);//await jer je index asihrona metoda
            var poruka = kontroler.ModelState[string.Empty]?.Errors.FirstOrDefault()?.ErrorMessage;
            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));//da li je vratio viewres
            Assert.AreEqual(
                "Datum kraja ne može biti prije datuma početka!",
                poruka);


        }
        [TestMethod]
        [DynamicData(nameof(PodaciDatumiXML), DynamicDataSourceType.Method)]
        public async Task Index_DatumiUProslosti(DateTime datumOd, DateTime datumDo)
        {
            var kontroler = new TerminController(null!);
            kontroler.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()//simulira http kontekst jer nam treba uloga 
            };
            var rezultat = await kontroler.Index(datumOd, datumDo, null);
            var poruka = kontroler.ModelState[string.Empty]?
        .Errors.FirstOrDefault()?.ErrorMessage;
            Assert.AreEqual("Ne možete tražiti termine u prošlosti!", poruka);

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));


        }

        [TestMethod]
        public async Task Index_ObicniKorisnik_VidiSamoSlobodneBuduceTermine()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            var teren = new Teren
            {
                naziv = "Testni teren"
            };

            context.Tereni.Add(teren);
            await context.SaveChangesAsync();

            var slobodan = new Termin
            {
                datum = DateTime.Today.AddDays(7),
                cijena = 50,
                terenID = teren.id,
                rezervisan = false
            };

            var rezervisan = new Termin
            {
                datum = DateTime.Today.AddDays(8),
                cijena = 60,
                terenID = teren.id,
                rezervisan = true
            };

            var prosli = new Termin
            {
                datum = DateTime.Today.AddDays(-7),
                cijena = 40,
                terenID = teren.id,
                rezervisan = false
            };

            context.Termini.AddRange(slobodan, rezervisan, prosli);//addrrange isto ko i add samo moze vise odjednom dodati
            await context.SaveChangesAsync();

            var kontroler = new TerminController(context);

            kontroler.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()//nema ni admin ni radnik
            };

            var rezultat = await kontroler.Index(null, null, null);//nije postavio filtere za datum, sport,. .

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));

            var view = (ViewResult)rezultat;
            var lista = view.Model as List<Termin>;//pretovrim u listu termina

            Assert.IsNotNull(lista);
            Assert.AreEqual(1, lista.Count);//provjera da je vratio samo slobodni
            Assert.AreEqual(slobodan.id, lista[0].id);//da je tacno jedan slobodno
        }

        [TestMethod]
        public async Task Index_FiltriranjePoDatumima()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            var teren = new Teren
            {
                naziv = "Testni teren"
            };

            context.Tereni.Add(teren);
            await context.SaveChangesAsync();

            var prvi = new Termin
            {
                datum = DateTime.Today.AddDays(7),
                cijena = 40,
                terenID = teren.id,
                rezervisan = false
            };

            var drugi = new Termin
            {
                datum = DateTime.Today.AddDays(10),
                cijena = 50,
                terenID = teren.id,
                rezervisan = false
            };

            var treci = new Termin
            {
                datum = DateTime.Today.AddDays(14),
                cijena = 60,
                terenID = teren.id,
                rezervisan = false
            };

            context.Termini.AddRange(prvi, drugi, treci);
            await context.SaveChangesAsync();

            var kontroler = new TerminController(context);

            kontroler.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var datumOd = DateTime.Today.AddDays(9);
            var datumDo = DateTime.Today.AddDays(12);

            var rezultat = await kontroler.Index(datumOd, datumDo, null);

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));

            var view = (ViewResult)rezultat;//uzme rezultantnu str
            var lista = view.Model as List<Termin>;//uzme pod koji se salju 

            Assert.IsNotNull(lista);
            Assert.AreEqual(1, lista.Count);//samo jedan mora vratiti
            Assert.AreEqual(drugi.id, lista[0].id);//provjera da je drugi
        }

        [TestMethod]
        public async Task Index_FiltriranjePoSportu()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            // Uzimamo dva razlicita sporta iz enuma Sport
            var sportovi = Enum.GetValues<Sport>();
            Assert.IsTrue(sportovi.Length >= 2);

            var teren1 = new Teren
            {
                naziv = "Prvi teren",
                sport = sportovi[0]
            };

            var teren2 = new Teren
            {
                naziv = "Drugi teren",
                sport = sportovi[1]
            };

            context.Tereni.AddRange(teren1, teren2);
            await context.SaveChangesAsync();

            var termin1 = new Termin
            {
                datum = DateTime.Today.AddDays(7),
                cijena = 50,
                terenID = teren1.id,
                rezervisan = false
            };

            var termin2 = new Termin
            {
                datum = DateTime.Today.AddDays(8),
                cijena = 60,
                terenID = teren2.id,
                rezervisan = false
            };

            context.Termini.AddRange(termin1, termin2);
            await context.SaveChangesAsync();

            var kontroler = new TerminController(context);

            kontroler.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Trazimo termine samo za prvi sport
            var rezultat = await kontroler.Index(null, null, sportovi[0]);

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));

            var view = (ViewResult)rezultat;//uzme tu str
            var lista = view.Model as List<Termin>;

            Assert.IsNotNull(lista);
            Assert.AreEqual(1, lista.Count);
            Assert.AreEqual(termin1.id, lista[0].id);
        }

        [TestMethod]
        public async Task Index_Administrator_VidiSveTermine()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            var teren = new Teren
            {
                naziv = "Testni teren"
            };

            context.Tereni.Add(teren);
            await context.SaveChangesAsync();

            var slobodan = new Termin
            {
                datum = DateTime.Today.AddDays(7),
                cijena = 50,
                terenID = teren.id,
                rezervisan = false
            };

            var rezervisan = new Termin
            {
                datum = DateTime.Today.AddDays(8),
                cijena = 60,
                terenID = teren.id,
                rezervisan = true
            };

            var prosli = new Termin
            {
                datum = DateTime.Today.AddDays(-7),
                cijena = 40,
                terenID = teren.id,
                rezervisan = false
            };

            context.Termini.AddRange(slobodan, rezervisan, prosli);
            await context.SaveChangesAsync();

            // Simuliramo korisnika s ulogom Administrator
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Role, "Administrator")
    };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var korisnik = new ClaimsPrincipal(identity);

            var kontroler = new TerminController(context);
            kontroler.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = korisnik
                }
            };

            var rezultat = await kontroler.Index(null, null, null);

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));

            var view = (ViewResult)rezultat;
            var lista = view.Model as List<Termin>;

            Assert.IsNotNull(lista);
            Assert.AreEqual(3, lista.Count);

            Assert.IsTrue(lista.Any(t => t.id == slobodan.id));
            Assert.IsTrue(lista.Any(t => t.id == rezervisan.id));
            Assert.IsTrue(lista.Any(t => t.id == prosli.id));//da vidi sve vrste 
        }


        [TestMethod]
        public void ProvjeriUcitavanjeCSV()
        {
            var podaci = PodaciDatumi().ToList();

            Assert.AreEqual(3, podaci.Count);
        }
        [TestMethod]
        public async Task Details_NullId_VracaNotFound()
        {
            var kontroler = new TerminController(null!);

            var rezultat = await kontroler.Details(null);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Details_RezervacijaBezKorisnika()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            var teren = new Teren
            {
                naziv = "Testni teren"
            };

            context.Tereni.Add(teren);
            await context.SaveChangesAsync();

            var termin = new Termin
            {
                datum = DateTime.Today.AddDays(7),
                cijena = 50,
                terenID = teren.id,
                rezervisan = true
            };

            context.Termini.Add(termin);
            await context.SaveChangesAsync();

            // Rezervacija postoji, ali korisnik s ovim id ne postoji
            var rezervacija = new Rezervacija
            {
                terminID = termin.id,
                korisnikID = "nepostojeci"
            };

            context.Rezervacije.Add(rezervacija);
            await context.SaveChangesAsync();

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Role, "Radnik")
    };

            var radnik = new ClaimsPrincipal(
                new ClaimsIdentity(claims, "TestAuth"));

            var kontroler = new TerminController(context);

            kontroler.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = radnik
                }
            };

            var rezultat = await kontroler.Details(termin.id);

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
            Assert.IsNull(kontroler.ViewBag.EmailKorisnika);//nema korisnika s tim id u bazi,al ima rez
        }


        [TestMethod]
        public async Task Details_TerminNePostoji()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;//lazna baza

            using var context = new ApplicationDbContext(opcije);

            var kontroler = new TerminController(context);

            var rezultat = await kontroler.Details(5);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }
        [TestMethod]
        public async Task Details_TerminPostoji()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);
            var teren = new Teren//nije radilo, jer nije bilo terena u bazi 
            {
                naziv = "Testni teren"
            };

            context.Tereni.Add(teren);
            await context.SaveChangesAsync();
            // Dodajem termin u testnu bazu
            var termin = new Termin
            {
                datum = DateTime.Today.AddDays(7),
                cijena = 50,
                terenID = teren.id,
                rezervisan = false
            };

            context.Termini.Add(termin);
            await context.SaveChangesAsync();

            var kontroler = new TerminController(context);
            kontroler.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            var rezultat= await kontroler.Details(termin.id);
            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
            var view = (ViewResult)rezultat;
            var dohvaceniTermin = view.Model as Termin;//view model salje termin, ako ga nema onda je null

            Assert.IsNotNull(dohvaceniTermin);
            Assert.AreEqual(termin.id, dohvaceniTermin.id);
        }
        [TestMethod]
        public async Task Details_Administrator_BezRezervacije()//kad admin gleda termin kji nije rez 
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            var teren = new Teren { naziv = "Testni teren" };
            context.Tereni.Add(teren);
            await context.SaveChangesAsync();

            var termin = new Termin
            {
                datum = DateTime.Today.AddDays(7),
                cijena = 50,
                terenID = teren.id,
                rezervisan = false
            };

            context.Termini.Add(termin);
            await context.SaveChangesAsync();

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Role, "Administrator")
    };

            var korisnik = new ClaimsPrincipal(
                new ClaimsIdentity(claims, "TestAuth"));

            var kontroler = new TerminController(context);
            kontroler.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = korisnik
                }
            };

            var rezultat = await kontroler.Details(termin.id);

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
            Assert.IsNull(kontroler.ViewBag.EmailKorisnika);//mora vratitit null jer nema korisnika za taj termin , viewbag da se posalju dodatni
        }
        [TestMethod]
        public async Task Details_Radnik_VidiKoJeRezervisao()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            var teren = new Teren
            {
                naziv = "Testni teren"
            };

            context.Tereni.Add(teren);
            await context.SaveChangesAsync();

            var termin = new Termin
            {
                datum = DateTime.Today.AddDays(7),
                cijena = 50,
                terenID = teren.id,
                rezervisan = true
            };

            context.Termini.Add(termin);
            await context.SaveChangesAsync();

            var korisnik = new Korisnik
            {
                ime = "Test",
                prezime = "Korisnik",
                UserName = "test@example.com",
                Email = "test@example.com"
            };

            context.Users.Add(korisnik);
            await context.SaveChangesAsync();

            var rezervacija = new Rezervacija
            {
                terminID = termin.id,
                korisnikID = korisnik.Id,
                Termin = termin,
                Korisnik = korisnik
            };

            context.Rezervacije.Add(rezervacija);
            await context.SaveChangesAsync();

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Role, "Radnik")
    };

            var radnik = new ClaimsPrincipal(
                new ClaimsIdentity(claims, "TestAuth"));

            var kontroler = new TerminController(context);

            kontroler.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = radnik
                }
            };

            var rezultat = await kontroler.Details(termin.id);//detalji termina

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));

            Assert.AreEqual(
                "test@example.com",
                (string?)kontroler.ViewBag.EmailKorisnika);// mora biti mail korisnika 
        }
        [TestMethod]
        public void Create_Get_VracaView()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            var kontroler = new TerminController(context);

            var rezultat = kontroler.Create();

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
            Assert.IsNotNull(kontroler.ViewData["terenID"]);//provjera jel napravio listu, i prazna se pika
        }
        [TestMethod]
        public async Task Create_Post_UspjesnoDodavanje()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);
            var teren = new Teren
            {
                naziv = "Testni teren"
            };

            context.Tereni.Add(teren);
            await context.SaveChangesAsync();
            var termin = new Termin
            {
                datum = DateTime.Today.AddDays(7),
                cijena = 50,
                terenID = teren.id,
                rezervisan = true
            };

            var kontroler = new TerminController(context);

            var rezultat = await kontroler.Create(termin);

            Assert.IsInstanceOfType(rezultat, typeof(RedirectToActionResult));//provjeravamo da preusmjeri 

            var redirect = (RedirectToActionResult)rezultat;
            Assert.AreEqual("Index", redirect.ActionName);//provjera je li na index 

            var sacuvaniTermin = await context.Termini.FirstOrDefaultAsync();

            Assert.IsNotNull(sacuvaniTermin);
            Assert.AreEqual(50, sacuvaniTermin.cijena);
            Assert.IsFalse(sacuvaniTermin.rezervisan);//provjeravam i da je novododdani nerezervisan
        }

        private class TestTempDataProvider : ITempDataProvider
        {
            public IDictionary<string, object> LoadTempData(HttpContext context)
            {
                return new Dictionary<string, object>();
            }

            public void SaveTempData(
                HttpContext context,
                IDictionary<string, object> values)
            {
            }
        }//ovo nam treba valjda jer  ovaj create u gresci koristiTempData


        [TestMethod]
        public async Task Create_Post_DuplikatTermina()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            var teren = new Teren
            {
                naziv = "Testni teren"
            };

            context.Tereni.Add(teren);
            await context.SaveChangesAsync();

            // Termin koji već postoji
            var postojeci = new Termin
            {
                datum = DateTime.Today.AddDays(7),
                cijena = 50,
                terenID = teren.id,
                rezervisan = false
            };

            context.Termini.Add(postojeci);
            await context.SaveChangesAsync();

            // Pokušavamo dodati termin za isti teren i vrijeme
            var novi = new Termin
            {
                datum = postojeci.datum,
                cijena = 60,
                terenID = teren.id
            };

            var kontroler = new TerminController(context);//proslijedim bazu kontroleru

            kontroler.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                new TestTempDataProvider());

            var rezultat = await kontroler.Create(novi);

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
            Assert.IsFalse(kontroler.ModelState.IsValid);// jesu li podaci prosli validaciju

            Assert.AreEqual(1, await context.Termini.CountAsync());//novi ne smije 
        }

        [TestMethod]
        public async Task Edit_Get_TerminPostoji()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            var teren = new Teren
            {
                naziv = "Testni teren"
            };

            context.Tereni.Add(teren);
            await context.SaveChangesAsync();

            var termin = new Termin
            {
                datum = DateTime.Today.AddDays(7),
                cijena = 50,
                terenID = teren.id,
                rezervisan = false
            };

            context.Termini.Add(termin);
            await context.SaveChangesAsync();

            var kontroler = new TerminController(context);

            var rezultat = await kontroler.Edit(termin.id);

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));

            var view = (ViewResult)rezultat;//moram da bi pristupio .model
            var dohvaceniTermin = view.Model as Termin;//izvlacimo sta je kontroler poslao stranici

            Assert.IsNotNull(dohvaceniTermin);
            Assert.AreEqual(termin.id, dohvaceniTermin.id);

            Assert.IsNotNull(kontroler.ViewData["terenID"]);
        }

        [TestMethod]
        public async Task Edit_Post_RezervisanTermin()//edit post 
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            var teren = new Teren
            {
                naziv = "Testni teren"
            };

            context.Tereni.Add(teren);
            await context.SaveChangesAsync();

            var termin = new Termin
            {
                datum = DateTime.Today.AddDays(7),
                cijena = 50,
                terenID = teren.id,
                rezervisan = true
            };

            context.Termini.Add(termin);
            await context.SaveChangesAsync();
            var izmijenjeni = new Termin
            {
                id = termin.id,
                datum = termin.datum,
                cijena = 100,
                terenID = teren.id
            };

            var kontroler = new TerminController(context);

            kontroler.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                new TestTempDataProvider());

            var rezultat = await kontroler.Edit(termin.id, izmijenjeni);

            Assert.IsInstanceOfType(rezultat, typeof(RedirectToActionResult));

            var redirect = (RedirectToActionResult)rezultat;
            Assert.AreEqual("Index", redirect.ActionName);

            Assert.AreEqual(
                "Ne možete uređivati termin koji je već rezervisan.",
                kontroler.TempData["Greska"]);

            var sacuvani = await context.Termini
                .AsNoTracking()
                .FirstAsync(t => t.id == termin.id);

            Assert.AreEqual(50, sacuvani.cijena);
        }

        [TestMethod]
        public async Task Edit_Get_RezervisanTermin()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            var teren = new Teren
            {
                naziv = "Testni teren"
            };

            context.Tereni.Add(teren);
            await context.SaveChangesAsync();

            var termin = new Termin
            {
                datum = DateTime.Today.AddDays(7),
                cijena = 50,
                terenID = teren.id,
                rezervisan = true
            };

            context.Termini.Add(termin);
            await context.SaveChangesAsync();

            var kontroler = new TerminController(context);

            kontroler.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                new TestTempDataProvider());

            var rezultat = await kontroler.Edit(termin.id);

            Assert.IsInstanceOfType(rezultat, typeof(RedirectToActionResult));

            var redirect = (RedirectToActionResult)rezultat;
            Assert.AreEqual("Index", redirect.ActionName);

            Assert.AreEqual(
                "Ne možete uređivati termin koji je već rezervisan.",
                kontroler.TempData["Greska"]);
        }
        [TestMethod]
        public async Task Edit_Get_NullId()
        {
            var kontroler = new TerminController(null!);

            var rezultat = await kontroler.Edit(null);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }//null id vraca mi notFound

        [TestMethod]
        public async Task Edit_Get_TerminNePostoji()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            var kontroler = new TerminController(context);

            var rezultat = await kontroler.Edit(999);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }//pokusam uzeti termin koji ne postoji 

        [TestMethod]
        public async Task Edit_Post_RazlicitiId()
        {
            var termin = new Termin
            {
                id = 6,
                datum = DateTime.Today.AddDays(7),
                cijena = 50,
                terenID = 1
            };

            var kontroler = new TerminController(null!);

            var rezultat = await kontroler.Edit(5, termin);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }// ako je razlicit termin id i broj koji saljem ide notFound

        [TestMethod]
        public async Task Edit_Post_TerminNePostoji()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            var termin = new Termin
            {
                id = 5,
                datum = DateTime.Today.AddDays(7),
                cijena = 50,
                terenID = 1
            };

            var kontroler = new TerminController(context);

            var rezultat = await kontroler.Edit(5, termin);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }//isti id, ali baza prazna pa nema sta naci 

        [TestMethod]
        public async Task Edit_Post_UspjesnaIzmjena()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            var teren = new Teren
            {
                naziv = "Testni teren"
            };

            context.Tereni.Add(teren);
            await context.SaveChangesAsync();

            var termin = new Termin
            {
                datum = DateTime.Today.AddDays(7),
                cijena = 50,
                terenID = teren.id,
                rezervisan = false
            };

            context.Termini.Add(termin);
            await context.SaveChangesAsync();

            // Prekidamo pracenje prethodno sacuvanih objekata, da imam onaj u bazi al ne pratim
            context.ChangeTracker.Clear();

            var izmijenjeni = new Termin
            {
                id = termin.id,
                datum = termin.datum,
                cijena = 100,
                terenID = teren.id
            };
            //ne dodajem ga u bazu 
            var kontroler = new TerminController(context);

            var rezultat = await kontroler.Edit(termin.id, izmijenjeni);//mijenjam postojeci

            Assert.IsInstanceOfType(rezultat, typeof(RedirectToActionResult));

            var redirect = (RedirectToActionResult)rezultat;
            Assert.AreEqual("Index", redirect.ActionName);

            var sacuvani = await context.Termini
                .FirstAsync(t => t.id == termin.id);//provjeravam je li se promijenio 
                

            Assert.AreEqual(100, sacuvani.cijena);
            Assert.IsFalse(sacuvani.rezervisan);
        }

        [TestMethod]
        public async Task Edit_Post_NevalidniPodaci()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            var teren = new Teren
            {
                naziv = "Testni teren"
            };

            context.Tereni.Add(teren);
            await context.SaveChangesAsync();

            var termin = new Termin
            {
                datum = DateTime.Today.AddDays(7),
                cijena = 50,
                terenID = teren.id,
                rezervisan = false
            };

            context.Termini.Add(termin);
            await context.SaveChangesAsync();


            var izmijenjeni = new Termin
            {
                id = termin.id,
                datum = termin.datum,
                cijena = 100,
                terenID = teren.id
            };

            var kontroler = new TerminController(context);

            // Namjerno simuliramo nevalidne podatke, da aktiviram !modelstate.isvalid
            kontroler.ModelState.AddModelError(
                "cijena", "Neispravna cijena");

            var rezultat = await kontroler.Edit(termin.id, izmijenjeni);

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));//vraca view
            Assert.IsFalse(kontroler.ModelState.IsValid);

            var sacuvani = await context.Termini
                .AsNoTracking()
                .FirstAsync(t => t.id == termin.id);//pronalazi prvi termin koji zadovoljava uslov, u bazi 

            Assert.AreEqual(50, sacuvani.cijena);
        }

        [TestMethod]
        public async Task Delete_Get_TerminPostoji()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            var teren = new Teren
            {
                naziv = "Testni teren"
            };

            context.Tereni.Add(teren);
            await context.SaveChangesAsync();

            var termin = new Termin
            {
                datum = DateTime.Today.AddDays(7),
                cijena = 50,
                terenID = teren.id,
                rezervisan = false
            };

            context.Termini.Add(termin);
            await context.SaveChangesAsync();

            var kontroler = new TerminController(context);

            var rezultat = await kontroler.Delete(termin.id);

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));//delete vraca view

            var view = (ViewResult)rezultat;
            var dohvaceniTermin = view.Model as Termin;//uzimam termin koji salje 

            Assert.IsNotNull(dohvaceniTermin);
            Assert.AreEqual(termin.id, dohvaceniTermin.id);

            Assert.AreEqual(1, await context.Termini.CountAsync());//ne brise 
        }//ovdje testiram delete GET , on ne brise nego pokazuje potvrdu

        [TestMethod]
        public async Task Delete_Get_NullId()
        {
            var kontroler = new TerminController(null!);

            var rezultat = await kontroler.Delete(null);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }//ne treba baza je prvo ide ifGET
        [TestMethod]
        public async Task Delete_Get_TerminNePostoji()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            var kontroler = new TerminController(context);

            var rezultat = await kontroler.Delete(999);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }//999 nema  GET
        [TestMethod]
        public async Task DeleteConfirmed_TerminNePostoji()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            var kontroler = new TerminController(context);

            var rezultat = await kontroler.DeleteConfirmed(999);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }//ovo je za post 

        [TestMethod]
        public async Task DeleteConfirmed_RezervisanTermin()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            var teren = new Teren
            {
                naziv = "Testni teren"
            };

            context.Tereni.Add(teren);
            await context.SaveChangesAsync();

            var termin = new Termin
            {
                datum = DateTime.Today.AddDays(7),
                cijena = 50,
                terenID = teren.id,
                rezervisan = true
            };

            context.Termini.Add(termin);
            await context.SaveChangesAsync();

            var kontroler = new TerminController(context);

            var rezultat = await kontroler.DeleteConfirmed(termin.id);

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));

            var view = (ViewResult)rezultat;
            Assert.AreEqual("Delete", view.ViewName);//vraca na prikazdelete 

            Assert.IsFalse(kontroler.ModelState.IsValid);//netacni pod

            var poruka = kontroler.ModelState[string.Empty]?
                .Errors.FirstOrDefault()?.ErrorMessage;//uzmi poruku

            Assert.AreEqual("Ne možete obrisati rezervisan termin.", poruka);

            Assert.AreEqual(1, await context.Termini.CountAsync());//ne smije brisati rezervisan termin
        }

        [TestMethod]
        public async Task DeleteConfirmed_UspjesnoBrisanje()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            var teren = new Teren
            {
                naziv = "Testni teren"
            };

            context.Tereni.Add(teren);
            await context.SaveChangesAsync();

            var termin = new Termin
            {
                datum = DateTime.Today.AddDays(7),
                cijena = 50,
                terenID = teren.id,
                rezervisan = false
            };

            context.Termini.Add(termin);
            await context.SaveChangesAsync();

            var kontroler = new TerminController(context);

            var rezultat = await kontroler.DeleteConfirmed(termin.id);//brise

            Assert.IsInstanceOfType(rezultat, typeof(RedirectToActionResult));//da vraca na index

            var redirect = (RedirectToActionResult)rezultat;//prevaram da vidim gdje preusmjeri 
            Assert.AreEqual("Index", redirect.ActionName);

            Assert.AreEqual(0, await context.Termini.CountAsync());//posto nije rezervisan treba ga obrisati
        }




    }
}