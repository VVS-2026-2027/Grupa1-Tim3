using ESportskiCentar.Attributes;
using ESportskiCentar.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Xml.Linq;


namespace ESportskiCentar.Tests
{
    [TestClass]
    public class ValidacijaRasponaDvaDatumaTests
    {
        // Čitanje validnih raspona iz XML datoteke
        public static IEnumerable<object[]> ValidniRasponiIzXml()
        {
            string putanja = Path.Combine(
                AppContext.BaseDirectory,
                "TestData",
                "datumi.xml");

            var dokument = XDocument.Load(putanja);

            return dokument.Descendants("Test")
                .Select(test => new object[]
                {
                    DateTime.Parse(
                        test.Element("DatumOd")!.Value,
                        CultureInfo.InvariantCulture),

                    DateTime.Parse(
                        test.Element("DatumDo")!.Value,
                        CultureInfo.InvariantCulture)
                })
                .ToList();
        }

        // Čitanje nevalidnih raspona iz CSV datoteke
        public static IEnumerable<object[]> NevalidniRasponiIzCsv()
        {
            string putanja = Path.Combine(
                AppContext.BaseDirectory,
                "TestData",
                "datumi.csv");

            return File.ReadAllLines(putanja)
                .Skip(1)
                .Where(red => !string.IsNullOrWhiteSpace(red))
                .Select(red =>
                {
                    var dijelovi = red.Split(',');

                    return new object[]
                    {
                        DateTime.Parse(dijelovi[0].Trim(), CultureInfo.InvariantCulture),
                        DateTime.Parse(dijelovi[1].Trim(), CultureInfo.InvariantCulture)
                    };
                })
                .ToArray();
        }

        // Validni rasponi iz XML datoteke

        [TestMethod]
        [TestCategory("DataDriven")]
        [DynamicData(nameof(ValidniRasponiIzXml), DynamicDataSourceType.Method)]
        public void GetValidationResult_ValidniRasponiIzXml_VracaSuccess(
            DateTime datumOd,
            DateTime datumDo)
        {
            var atribut = new ValidacijaRasponaDvaDatuma();

            var model = new Izvjestaj
            {
                datumOd = datumOd,
                datumDo = datumDo
            };

            var rezultat = atribut.GetValidationResult(
                model.datumOd,
                new ValidationContext(model));

            Assert.IsNull(rezultat);
        }

        // Nevalidni rasponi iz CSV datoteke

        [TestMethod]
        [TestCategory("DataDriven")]
        [DynamicData(nameof(NevalidniRasponiIzCsv), DynamicDataSourceType.Method)]
        public void GetValidationResult_NevalidniRasponiIzCsv_VracaGresku(
            DateTime datumOd,
            DateTime datumDo)
        {
            var atribut = new ValidacijaRasponaDvaDatuma();

            var model = new Izvjestaj
            {
                datumOd = datumOd,
                datumDo = datumDo
            };

            var rezultat = atribut.GetValidationResult(
                model.datumOd,
                new ValidationContext(model));

            Assert.AreEqual(
                "Datum kraja ne može biti prije datuma početka!",
                rezultat.ErrorMessage);
        }

        [TestMethod]
        public void IstiDatum_VracaSuccess()
        {
            var atribut = new ValidacijaRasponaDvaDatuma();

            var model = new Izvjestaj
            {
                datumOd = new DateTime(2026, 1, 15),
                datumDo = new DateTime(2026, 1, 15)
            };

            var rezultat = atribut.GetValidationResult(
                model.datumOd,
                new ValidationContext(model));

            Assert.IsNull(rezultat);
        }

        [TestMethod]
        public void DatumDoPrijeDatumaOdSaVlastitomPorukom_VracaVlastituPoruku()
        {
            var atribut = new ValidacijaRasponaDvaDatuma
            {
                ErrorMessage = "Neispravan raspon."
            };

            var model = new Izvjestaj
            {
                datumOd = new DateTime(2026, 2, 1),
                datumDo = new DateTime(2026, 1, 1)
            };

            var rezultat = atribut.GetValidationResult(
                model.datumOd,
                new ValidationContext(model));

            Assert.AreEqual("Neispravan raspon.", rezultat.ErrorMessage);
        }

        [TestMethod]
        public void ObjekatNijeIzvjestaj_VracaSuccess()
        {
            var atribut = new ValidacijaRasponaDvaDatuma();
            var drugiObjekat = new object();

            var rezultat = atribut.GetValidationResult(
                DateTime.Now,
                new ValidationContext(drugiObjekat));

            Assert.IsNull(rezultat);
        }
    }
}
