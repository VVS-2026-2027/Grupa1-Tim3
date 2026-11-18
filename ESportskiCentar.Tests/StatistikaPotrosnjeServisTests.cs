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
    }
}
