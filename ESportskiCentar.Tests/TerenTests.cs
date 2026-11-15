using ESportskiCentar.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace ESportskiCentar.Tests
{
    [TestClass]
    public class TerenTests
    {
        // ucitavanje JSON podataka, provjerava se validnost naziva
        public static IEnumerable<object[]> PodaciNaziviTerenJson()
        {
            var putanja = Path.Combine(AppContext.BaseDirectory, "TestData", "tereni.json");

            using var dokument = JsonDocument.Parse(File.ReadAllText(putanja));

            return dokument.RootElement.EnumerateArray()
                .Select(element => new object[]
                {
                    element.GetProperty("naziv").GetString()!,
                    element.GetProperty("ispravno").GetBoolean()
                }).ToList();
        }

        [TestMethod]
        [DynamicData(nameof(PodaciNaziviTerenJson), DynamicDataSourceType.Method)]
        public void Teren_NaziviIzJson_ProvjeravaValidaciju(string naziv, bool ispravno)
        {
            var teren = new Teren { naziv = naziv, sport = Sport.FUDBAL };
            bool validan = Validator.TryValidateObject(teren, new ValidationContext(teren), null, true);
            Assert.AreEqual(ispravno, validan);
        }
        // provjera poruke greske za prazan naziv
        [TestMethod]
        public void Teren_PrazanNaziv_VracaGresku()
        {
            var teren = new Teren { naziv = "", sport = Sport.FUDBAL };

            var greske = new List<ValidationResult>();
            bool validan = Validator.TryValidateObject(teren, new ValidationContext(teren), greske, true);

            Assert.IsFalse(validan);
            Assert.IsTrue(greske.Any(g => g.ErrorMessage == "Naziv terena je obavezan."));
        }

        // provjera poruke greske za prekratak naziv
        [TestMethod]
        public void Teren_PrekratakNaziv_VracaGresku()
        {
            var teren = new Teren{ naziv = "AB", sport = Sport.FUDBAL };

            var greske = new List<ValidationResult>();
            bool validan = Validator.TryValidateObject(teren, new ValidationContext(teren), greske, true);

            Assert.IsFalse(validan);
            Assert.IsTrue(greske.Any(g => g.ErrorMessage == "Naziv terena mora imati između 3 i 100 karaktera."));
        }

        // provjera poruke greske za predug naziv
        [TestMethod]
        public void Teren_PredugNaziv_VracaGresku()
        {
            var teren = new Teren { naziv = new string('A', 101), sport = Sport.FUDBAL };

            var greske = new List<ValidationResult>();
            bool validan = Validator.TryValidateObject(teren, new ValidationContext(teren), greske, true);

            Assert.IsFalse(validan);
            Assert.IsTrue(greske.Any(g => g.ErrorMessage == "Naziv terena mora imati između 3 i 100 karaktera."));
        }

        // provjera validnog terena
        [TestMethod]
        public void Teren_ValidneVrijednosti_ProlaziValidaciju()
        {
            var teren = new Teren { naziv = "Fudbalski teren", sport = Sport.FUDBAL };

            bool validan = Validator.TryValidateObject(teren, new ValidationContext(teren), null, true);
            Assert.IsTrue(validan);
        }
    }
}
