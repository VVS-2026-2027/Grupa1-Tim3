
using ESportskiCentar.Models;
using System.Text.Json;

namespace ESportskiCentar.Tests
{
    [TestClass]
    public class RezervacijaNotifikacijaTests
    {
        public static IEnumerable<object[]> PodaciIzJson()
        {
            string putanja = Path.Combine(
                AppContext.BaseDirectory,
                "TestData",
                "rezervacijeNotifikacije.json");

            string json = File.ReadAllText(putanja);

            using var dokument = JsonDocument.Parse(json);

            foreach (var podatak in dokument.RootElement.EnumerateArray())
            {
                yield return new object[]
                {
                    podatak.GetProperty("tip").GetString()!,
                    podatak.GetProperty("id").GetInt32(),
                    podatak.GetProperty("vrijednost").GetDouble(),
                    podatak.GetProperty("ocekivano").GetDouble()
                };
            }
        }

        [TestMethod]
        [DynamicData(nameof(PodaciIzJson),
            DynamicDataSourceType.Method)]
        public void Modeli_PodaciIzJson_IspravnoCuvajuVrijednosti(
            string tip, int id, double vrijednost, double ocekivano)
        {
            if (tip == "Rezervacija")
            {
                var rezervacija = new Rezervacija
                {
                    id = id,
                    konacnaCijena = vrijednost
                };

                Assert.AreEqual(id, rezervacija.id);
                Assert.AreEqual(ocekivano,
                    rezervacija.konacnaCijena, 0.001);
            }
            else
            {
                var notifikacija = new Notifikacija
                {
                    id = id,
                    rezervacijaID = (int)vrijednost
                };

                Assert.AreEqual(id, notifikacija.id);
                Assert.AreEqual((int)ocekivano,
                    notifikacija.rezervacijaID);
            }
        }

        [TestMethod]
        public void Rezervacija_PostavljenKod_CuvaKod()
        {
            var rezervacija = new Rezervacija
            {
                uneseniKod = "SPORT10"
            };

            Assert.AreEqual("SPORT10", rezervacija.uneseniKod);
        }

        [TestMethod]
        public void Rezervacija_PrimijenjenPopust_CuvaVrijednost()
        {
            var rezervacija = new Rezervacija
            {
                primjenjenPopust = true
            };

            Assert.IsTrue(rezervacija.primjenjenPopust);
        }

        [TestMethod]
        public void Rezervacija_PostavljenTermin_CuvaTermin()
        {
            var rezervacija = new Rezervacija
            {
                terminID = 5,
                korisnikID = "korisnik1"
            };

            Assert.AreEqual(5, rezervacija.terminID);
            Assert.AreEqual("korisnik1", rezervacija.korisnikID);
        }

        [TestMethod]
        public void Notifikacija_NijePoslana_CuvaStatus()
        {
            var notifikacija = new Notifikacija
            {
                rezervacijaID = 1,
                poslana = false
            };

            Assert.IsFalse(notifikacija.poslana);
        }

        [TestMethod]
        public void Notifikacija_Poslana_CuvaStatus()
        {
            var notifikacija = new Notifikacija
            {
                rezervacijaID = 1,
                poslana = true
            };

            Assert.IsTrue(notifikacija.poslana);
        }

        [TestMethod]
        public void Notifikacija_PostavljenoVrijeme_CuvaVrijeme()
        {
            var vrijeme = DateTime.Now.AddDays(1);

            var notifikacija = new Notifikacija
            {
                vrijemeSlanja = vrijeme
            };

            Assert.AreEqual(vrijeme,
                notifikacija.vrijemeSlanja);
        }

        [TestMethod]
        public void Notifikacija_PostavljenaRezervacija_VracaRezervaciju()
        {
            var rezervacija = new Rezervacija
            {
                id = 1,
                korisnikID = "korisnik1",
                terminID = 1,
                konacnaCijena = 100
            };

            var notifikacija = new Notifikacija
            {
                rezervacijaID = rezervacija.id,
                Rezervacija = rezervacija,
                vrijemeSlanja = DateTime.Now.AddDays(1)
            };

            Assert.IsNotNull(notifikacija.Rezervacija);
            Assert.AreEqual(rezervacija.id,
                notifikacija.Rezervacija.id);
        }
    }
}
