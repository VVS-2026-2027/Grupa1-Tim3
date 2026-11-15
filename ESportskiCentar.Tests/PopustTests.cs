using ESportskiCentar.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;

namespace ESportskiCentar.Tests
{
    [TestClass]
    public class PopustTests
    {
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
        // data driven test kroz csv
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
        [TestMethod]
        public void Popust_NegativanBrojRezervacija_VracaGresku()
        {
            var popust = new Popust{ procenat = 10, potrebanBrojRezervacija = -1 };

            var greske = new List<ValidationResult>();
            bool validan = Validator.TryValidateObject(popust, new ValidationContext(popust), greske, true);

            Assert.IsFalse(validan);
            Assert.IsTrue(greske.Any(g => g.ErrorMessage == "Potreban broj rezervacija mora biti najmanje 0."));
        }

        [TestMethod]
        public void Popust_ProcenatVeciOd100_VracaGresku()
        {
            var popust = new Popust{ procenat = 101, potrebanBrojRezervacija = 5 };

            var greske = new List<ValidationResult>();
            bool validan = Validator.TryValidateObject(popust, new ValidationContext(popust), greske, true);

            Assert.IsFalse(validan);
            Assert.IsTrue(greske.Any(g => g.ErrorMessage == "Procenat popusta mora biti između 0.1 i 100."));
        }

        [TestMethod]
        public void Popust_ValidneVrijednosti_ProlaziValidaciju()
        {
            var popust = new Popust{ procenat = 50, potrebanBrojRezervacija = 10 };
            bool validan = Validator.TryValidateObject(popust, new ValidationContext(popust), null, true);
            Assert.IsTrue(validan);
        }
        [TestMethod]
        public void Popust_ProcenatManjiOdMinimuma_VracaGresku()
        {
            var popust = new Popust{ procenat = 0, potrebanBrojRezervacija = 5 };

            var greske = new List<ValidationResult>();
            bool validan = Validator.TryValidateObject(popust, new ValidationContext(popust), greske, true);

            Assert.IsFalse(validan);
            Assert.IsTrue(greske.Any(g => g.ErrorMessage == "Procenat popusta mora biti između 0.1 i 100."));
        }
    }
}
