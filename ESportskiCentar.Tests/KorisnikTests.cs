using ESportskiCentar.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel.DataAnnotations;

namespace ESportskiCentar.Tests
{
    [TestClass]
    public class KorisnikTests
    {

        [TestMethod]
        public void Korisnik_IspravniPodaci()
        {
            var korisnik = new Korisnik
            {
                ime = "AAA",
                prezime = "BBB"
            };

            Assert.AreEqual("AAA", korisnik.ime);
            Assert.AreEqual("BBB", korisnik.prezime);

            var greske = new List<ValidationResult>();
            var kontekst = new ValidationContext(korisnik);

            bool ispravno = Validator.TryValidateObject(
                korisnik, kontekst, greske, true);

            Assert.IsTrue(ispravno);
            Assert.AreEqual(0, greske.Count);
        }
        [TestMethod]
        public void Korisnik_ImeNedostaje()
        {
            var korisnik = new Korisnik
            {
                ime = "",
                prezime = "BBB"
            };

            var greske = new List<ValidationResult>();//lista gresaka
            var kontekst = new ValidationContext(korisnik);//kazemo sta validiramo

            bool ispravno = Validator.TryValidateObject(
                korisnik, kontekst, greske, true);

            Assert.IsFalse(ispravno);
            Assert.AreEqual("Ime je obavezno polje.", greske[0].ErrorMessage);
        }
        [TestMethod]
        public void Korisnik_PrezimeNedostaje()
        {
            var korisnik = new Korisnik
            {
                ime = "AAA",
                prezime = ""
            };

            var greske = new List<ValidationResult>();
            var kontekst = new ValidationContext(korisnik);

            bool ispravno = Validator.TryValidateObject(
                korisnik, kontekst, greske, true);

            Assert.IsFalse(ispravno);
            Assert.AreEqual("Prezime je obavezno polje.", greske[0].ErrorMessage);
        }
        [TestMethod]
        public void Korisnik_ImePrekratko()
        {
            var korisnik = new Korisnik
            {
                ime = "A",
                prezime = "BBB"
            };

            var greske = new List<ValidationResult>();
            var kontekst = new ValidationContext(korisnik);

            bool ispravno = Validator.TryValidateObject(
                korisnik, kontekst, greske, true);

            Assert.IsFalse(ispravno);
            Assert.AreEqual(
                "Ime mora imati između 2 i 30 karaktera.",
                greske[0].ErrorMessage);
        }
        [TestMethod]
        public void Korisnik_ImePredugo()
        {
            var korisnik = new Korisnik
            {
                ime = new string('A', 31),//stavim preko 30 karaktera
                prezime = "BBB"
            };

            var greske = new List<ValidationResult>();
            var kontekst = new ValidationContext(korisnik);

            bool ispravno = Validator.TryValidateObject(
                korisnik, kontekst, greske, true);

            Assert.IsFalse(ispravno);//ne prodje validaciju 
            Assert.AreEqual(
                "Ime mora imati između 2 i 30 karaktera.",
                greske[0].ErrorMessage);
        }
        [TestMethod]
        public void Korisnik_ImeSadrziBrojeve()
        {
            var korisnik = new Korisnik
            {
                ime = "AAA123",
                prezime = "BBB"
            };

            var greske = new List<ValidationResult>();
            var kontekst = new ValidationContext(korisnik);

            bool ispravno = Validator.TryValidateObject(
                korisnik, kontekst, greske, true);

            Assert.IsFalse(ispravno);
            Assert.AreEqual(
                "Ime može sadržavati samo slova.",
                greske[0].ErrorMessage);
        }
        [TestMethod]
        public void Korisnik_PreizmePrekratko()
        {
            var korisnik = new Korisnik
            {
                ime = "AAA",
                prezime = "B"
            };
            var greske= new List<ValidationResult>();
            var kontekst = new ValidationContext(korisnik);
            bool ispravno = Validator.TryValidateObject(korisnik, kontekst, greske, true);
            Assert.IsFalse(ispravno);
            Assert.AreEqual(
                "Prezime mora imati između 2 i 30 karaktera.",
                greske[0].ErrorMessage);
        }
        [TestMethod]
        public void Korisnik_PrezimePredugo()
        {
            var korisnik = new Korisnik
            {
                ime = "AAA",
                prezime = new string('B', 31)
            };

            var greske = new List<ValidationResult>();
            var kontekst = new ValidationContext(korisnik);

            bool ispravno = Validator.TryValidateObject(
                korisnik, kontekst, greske, true);

            Assert.IsFalse(ispravno);
            Assert.AreEqual(
                "Prezime mora imati između 2 i 30 karaktera.",
                greske[0].ErrorMessage);
        }
        [TestMethod]
        public void Korisnik_PrezimeSadrziBrojeve()
        {
            var korisnik = new Korisnik
            {
                ime = "AAA",
                prezime = "BBB123"
            };

            var greske = new List<ValidationResult>();
            var kontekst = new ValidationContext(korisnik);

            bool ispravno = Validator.TryValidateObject(
                korisnik, kontekst, greske, true);

            Assert.IsFalse(ispravno);
            Assert.AreEqual(
                "Prezime može sadržavati samo slova.",
                greske[0].ErrorMessage);
        }
        [TestMethod]
        public void Korisnik_MinimalnaDuzina()
        {
            var korisnik = new Korisnik
            {
                ime = "AA",
                prezime = "BB"
            };

            var greske = new List<ValidationResult>();
            var kontekst = new ValidationContext(korisnik);

            bool ispravno = Validator.TryValidateObject(
                korisnik, kontekst, greske, true);

            Assert.IsTrue(ispravno);
            Assert.AreEqual(0, greske.Count);
        }
    }
}