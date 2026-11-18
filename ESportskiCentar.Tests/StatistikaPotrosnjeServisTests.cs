using ESportskiCentar.Models;
using System;
using System.Collections.Generic;
using System.Text;
using ESportskiCentar.Services;

namespace ESportskiCentar.Tests
{
    [TestClass]
    public class StatistikaPotrosnjeServisTests
    {
        [TestMethod]
        public void BezRezervacija_VracaNule()
        {
            var rezervacije = new List<Rezervacija>();

            var rezultat = StatistikaPotrosnjeServis.Izracunaj(rezervacije, "korisnik1");

            Assert.AreEqual(0, rezultat.BrojRezervacija);
            Assert.AreEqual(0, rezultat.Ukupno);
            Assert.AreEqual(0, rezultat.Prosjek);
        }
        [TestMethod]
        public void JednaRezervacija_VracaTacnuPotrosnju()
        {
            var rezervacije = new List<Rezervacija>{ new Rezervacija { korisnikID = "korisnik1", konacnaCijena = 50 } };

            var rezultat = StatistikaPotrosnjeServis.Izracunaj(rezervacije, "korisnik1");

            Assert.AreEqual(1, rezultat.BrojRezervacija);
            Assert.AreEqual(50, rezultat.Ukupno);
            Assert.AreEqual(50, rezultat.Prosjek);
        }
        [TestMethod]
        public void ViseRezervacija_RacunaZbirIProsjek()
        {
            var rezervacije = new List<Rezervacija>
            {
                new Rezervacija{ korisnikID = "korisnik1", konacnaCijena = 20 },
                new Rezervacija{ korisnikID = "korisnik1", konacnaCijena = 30 },
                new Rezervacija{ korisnikID = "korisnik1", konacnaCijena = 40 }
            };

            var rezultat = StatistikaPotrosnjeServis.Izracunaj(rezervacije, "korisnik1");

            Assert.AreEqual(3, rezultat.BrojRezervacija);
            Assert.AreEqual(90, rezultat.Ukupno);
            Assert.AreEqual(30, rezultat.Prosjek);
        }
    }
}
