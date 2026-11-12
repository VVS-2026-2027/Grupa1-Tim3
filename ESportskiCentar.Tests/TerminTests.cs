using ESportskiCentar.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel.DataAnnotations;

namespace ESportskiCentar.Tests
{
    [TestClass]
    public class TerminTests
    {
        [TestMethod]
        public void Termin_PodrazumijevanoSlobodan()
        {
            var termin = new Termin();

            Assert.IsFalse(termin.rezervisan);
        }
        [TestMethod]
        public void Termin_IspravnoPostavljanjePodataka()
        {
            var datum = DateTime.Now.AddDays(7);

            var termin = new Termin
            {
                id = 1,
                datum = datum,
                cijena = 50,
                rezervisan = true,
                terenID = 2
            };

            Assert.AreEqual(1, termin.id);
            Assert.AreEqual(datum, termin.datum);
            Assert.AreEqual(50, termin.cijena);
            Assert.IsTrue(termin.rezervisan);
            Assert.AreEqual(2, termin.terenID);
        }
        [TestMethod]
        public void Termin_DatumUProslosti()
        {
            var termin = new Termin
            {
                datum = DateTime.Now.AddDays(-1)
            };

            var greske = new List<ValidationResult>();

            var kontekst = new ValidationContext(termin)
            {
                MemberName = nameof(Termin.datum)//kazemo da provjeravamo datum 
            };

            bool ispravno = Validator.TryValidateProperty(//property jer testiramo samo datum
                termin.datum, kontekst, greske);

            Assert.IsFalse(ispravno);
            Assert.AreEqual(
                "Termin se ne može kreirati u prošlosti! Izaberite buduće vrijeme.",
                greske[0].ErrorMessage);
        }
        [TestMethod]
        public void Termin_DatumUBuducnosti()
        {
            var termin = new Termin
            {
                datum = DateTime.Now.AddDays(7)
            };

            var greske = new List<ValidationResult>();

            var kontekst = new ValidationContext(termin)
            {
                MemberName = nameof(Termin.datum)
            };

            bool ispravno = Validator.TryValidateProperty(
                termin.datum, kontekst, greske);

            Assert.IsTrue(ispravno);
            Assert.AreEqual(0, greske.Count);
        }
        [TestMethod]
        public void Termin_NegativnaCijena()
        {
            var termin = new Termin
            {
                cijena = -10
            };

            var greske = new List<ValidationResult>();

            var kontekst = new ValidationContext(termin)
            {
                MemberName = nameof(Termin.cijena)
            };

            bool ispravno = Validator.TryValidateProperty(
                termin.cijena, kontekst, greske);

            Assert.IsFalse(ispravno);
            Assert.IsTrue(greske.Any(g =>
                g.ErrorMessage == "Cijena ne može biti negativna."));//provjera da li postoji ocekivana poruka greske 
        }
        [TestMethod]
        public void Termin_CijenaNula()
        {
            var termin = new Termin
            {
                cijena = 0
            };

            var greske = new List<ValidationResult>();

            var kontekst = new ValidationContext(termin)
            {
                MemberName = nameof(Termin.cijena)
            };

            bool ispravno = Validator.TryValidateProperty(
                termin.cijena, kontekst, greske);

            Assert.IsTrue(ispravno);
            Assert.AreEqual(0, greske.Count);
        }
        [TestMethod]
        public void Termin_CijenaSaDecimalama()
        {
            var termin = new Termin
            {
                cijena = 15.50
            };

            var greske = new List<ValidationResult>();

            var kontekst = new ValidationContext(termin)
            {
                MemberName = nameof(Termin.cijena)
            };

            bool ispravno = Validator.TryValidateProperty(
                termin.cijena, kontekst, greske);

            Assert.IsTrue(ispravno);
            Assert.AreEqual(0, greske.Count);
        }
        [TestMethod]
        public void Termin_CijenaViseOdDvijeDecimale()
        {
            var termin = new Termin
            {
                cijena = 15.555
            };

            var greske = new List<ValidationResult>();

            var kontekst = new ValidationContext(termin)
            {
                MemberName = nameof(Termin.cijena)
            };

            bool ispravno = Validator.TryValidateProperty(
                termin.cijena, kontekst, greske);

            Assert.IsFalse(ispravno);
            Assert.IsTrue(greske.Any(g =>
                g.ErrorMessage == "Cijena mora biti broj (npr. 15 ili 15.50)!"));
        }
        [TestMethod]
        public void Termin_TerenID_Ispravan()
        {
            var termin = new Termin
            {
                terenID = 2
            };

            var greske = new List<ValidationResult>();

            var kontekst = new ValidationContext(termin)
            {
                MemberName = nameof(Termin.terenID)
            };

            bool ispravno = Validator.TryValidateProperty(
                termin.terenID, kontekst, greske);

            Assert.IsTrue(ispravno);
            Assert.AreEqual(0, greske.Count);
        }
        [TestMethod]
        public void Termin_PovezivanjeSaTerenom()
        {
            var teren = new Teren
            {
                id = 2,
                naziv = "Fudbalski teren"
            };

            var termin = new Termin
            {
                terenID = 2,
                Teren = teren
            };

            Assert.IsNotNull(termin.Teren);
            Assert.AreEqual(2, termin.Teren.id);
            Assert.AreEqual("Fudbalski teren", termin.Teren.naziv);
            Assert.AreEqual(teren.id, termin.terenID);
        }
        [TestMethod]
        public void Termin_TerenMozeBitiNull()
        {
            var termin = new Termin
            {
                terenID = 2,
                Teren = null
            };

            Assert.IsNull(termin.Teren);
            Assert.AreEqual(2, termin.terenID);
        }

    }
}