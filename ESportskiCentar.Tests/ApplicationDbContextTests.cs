
using ESportskiCentar.Data;
using ESportskiCentar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ESportskiCentar.Models;
namespace ESportskiCentar.Tests
{
    [TestClass]
    public class ApplicationDbContextTests
    {
        [TestMethod]
        public void ApplicationDbContext_DbSetoviPostoje()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;//baza nije praava, , testna

            using var context = new ApplicationDbContext(opcije);

            Assert.IsNotNull(context.Korisnici);
            Assert.IsNotNull(context.Tereni);
            Assert.IsNotNull(context.Termini);
            Assert.IsNotNull(context.Rezervacije);
            Assert.IsNotNull(context.Notifikacije);
            Assert.IsNotNull(context.Izvjestaji);
            Assert.IsNotNull(context.Popusti);
        }

        [TestMethod]
        public void ApplicationDbContext_MapiranjeTabela()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            Assert.AreEqual("Teren",
                context.Model.FindEntityType(typeof(Teren))!.GetTableName());

            Assert.AreEqual("Termin",
                context.Model.FindEntityType(typeof(Termin))!.GetTableName());

            Assert.AreEqual("Rezervacija",
                context.Model.FindEntityType(typeof(Rezervacija))!.GetTableName());//nadje taj entitet, tipa rez, uzme ime i provjeri 

            Assert.AreEqual("Notifikacija",
                context.Model.FindEntityType(typeof(Notifikacija))!.GetTableName());

            Assert.AreEqual("Izvjestaj",
                context.Model.FindEntityType(typeof(Izvjestaj))!.GetTableName());

            Assert.AreEqual("Popust",
                context.Model.FindEntityType(typeof(Popust))!.GetTableName());
        }
        [TestMethod]
        public void ApplicationDbContext_MapiranjeKorisnika()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(opcije);

            var korisnik = context.Model.FindEntityType(typeof(Korisnik));

            Assert.IsNotNull(korisnik);

            Assert.IsNotNull(korisnik.FindProperty("ime"));//je li registruje oba svojstva
            Assert.IsNotNull(korisnik.FindProperty("prezime"));
        }
    }
}
