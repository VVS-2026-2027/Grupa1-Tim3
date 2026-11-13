using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ESportskiCentar.Models;

namespace ESportskiCentar.Tests
{
    [TestClass]
    public class IzvjestajTests
    {
        private Izvjestaj NapraviIzvjestaj() {

            return new Izvjestaj
            {
                id = 7,
                korisnikID = "korisnik-1",
                datumGenerisanja = new DateTime(2026, 2, 1, 9, 30, 0),
                datumOd = new DateTime(2026, 1, 1),
                datumDo = new DateTime(2026, 1, 31),
                ukupnoRezervacija = 12,
                ukupnaZarada = 345.5
            };
        }

        private List<string> PorukeValidacije(Izvjestaj izvjestaj)
        {
            var rezultati = new List<ValidationResult>();

            Validator.TryValidateObject(
                izvjestaj,
                new ValidationContext(izvjestaj),
                rezultati,
                validateAllProperties: true);

            return rezultati
                .Select(rezultat => rezultat.ErrorMessage)
                .ToList();
        }
        [TestMethod]
        public void Izvjestaj_IspravnoPostavljanjePodataka()
        {
            var datumGenerisanja = new DateTime(2026, 2, 1, 9, 30, 0);
            var datumOd = new DateTime(2026, 1, 1);
            var datumDo = new DateTime(2026, 1, 31);

            var izvjestaj = new Izvjestaj
            {
                id = 7,
                korisnikID = "korisnik-1",
                datumGenerisanja = datumGenerisanja,
                datumOd = datumOd,
                datumDo = datumDo,
                ukupnoRezervacija = 12,
                ukupnaZarada = 345.5
            };

            Assert.AreEqual(7, izvjestaj.id);
            Assert.AreEqual("korisnik-1", izvjestaj.korisnikID);
            Assert.AreEqual(datumGenerisanja, izvjestaj.datumGenerisanja);
            Assert.AreEqual(datumOd, izvjestaj.datumOd);
            Assert.AreEqual(datumDo, izvjestaj.datumDo);
            Assert.AreEqual(12, izvjestaj.ukupnoRezervacija);
            Assert.AreEqual(345.5, izvjestaj.ukupnaZarada);
        }

        [TestMethod]
        public void Svojstva_PostavljenaVrijednosti_VracajuPostavljeneVrijednosti()
        {
            var izvjestaj = NapraviIzvjestaj();

            Assert.AreEqual(7, izvjestaj.id);
            Assert.AreEqual("korisnik-1", izvjestaj.korisnikID);
            Assert.AreEqual(
                new DateTime(2026, 2, 1, 9, 30, 0),
                izvjestaj.datumGenerisanja);
            Assert.AreEqual(new DateTime(2026, 1, 1), izvjestaj.datumOd);
            Assert.AreEqual(new DateTime(2026, 1, 31), izvjestaj.datumDo);
            Assert.AreEqual(12, izvjestaj.ukupnoRezervacija);
            Assert.AreEqual(345.5, izvjestaj.ukupnaZarada);
        }

        [TestMethod]
        public void NoviIzvjestaj_BezPostavljanja_NavigacijaJeNullABrojaciNula()
        {
            var prazan = new Izvjestaj();

            Assert.IsNull(prazan.Korisnik);
            Assert.AreEqual(0, prazan.ukupnoRezervacija);
            Assert.AreEqual(0.0, prazan.ukupnaZarada);
        }

        [TestMethod]
        public void Validacija_ValidanIzvjestaj_NemaGresaka()
        {
            var izvjestaj = NapraviIzvjestaj();

            var greske = PorukeValidacije(izvjestaj);

            Assert.AreEqual(0, greske.Count);
        }

        [TestMethod]
        public void Validacija_NegativanBrojRezervacija_VracaGresku()
        {
            var izvjestaj = NapraviIzvjestaj();
            izvjestaj.ukupnoRezervacija = -5;

            var greske = PorukeValidacije(izvjestaj);

            CollectionAssert.AreEqual(
                new[] { "Broj rezervacija ne može biti negativan." },
                greske);
        }

        [TestMethod]
        public void Validacija_NegativnaZarada_VracaGresku()
        {
            var izvjestaj = NapraviIzvjestaj();
            izvjestaj.ukupnaZarada = -55.00;

            var greske = PorukeValidacije(izvjestaj);

            CollectionAssert.AreEqual(
                new[] { "Ukupna zarada ne može biti negativna." },
                greske);
        }

        [TestMethod]
        public void Validacija_KorisnikIdNijePostavljen_VracaGresku()
        {
            var izvjestaj = NapraviIzvjestaj();
            izvjestaj.korisnikID = null;

            var greske = PorukeValidacije(izvjestaj);

            CollectionAssert.AreEqual(
                new[] { "Korisnik je obavezan." },
                greske);
        }

        [TestMethod]
        public void Validacija_DatumDoPrijeDatumaOd_VracaGreskuRasponaDatuma()
        {
            var izvjestaj = NapraviIzvjestaj();
            izvjestaj.datumOd = new DateTime(2026, 2, 1);
            izvjestaj.datumDo = new DateTime(2026, 1, 1);

            var greske = PorukeValidacije(izvjestaj);

            CollectionAssert.AreEqual(
                new[] { "Datum kraja ne može biti prije datuma početka!" },
                greske);
        }

    }
}
