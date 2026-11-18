
using ESportskiCentar.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ESportskiCentar.Tests
{
    [TestClass]
    public class KuponTests
    {
        [TestMethod]
        public void Kupon_IspravanKod_UmanjujeCijenu()
        {
            var kupon = new Kupon
            {
                kod = "SPORT10",
                procenat = 10
            };

            double novaCijena = kupon.IzracunajCijenu(100, "SPORT10");

            Assert.AreEqual(90.0, novaCijena);
        }
    
    [TestMethod]
        public void Kupon_PogresanKod_NemaPopusta()
        {
            var kupon = new Kupon
            {
                kod = "SPORT10",
                procenat = 10
            };

            double novaCijena = kupon.IzracunajCijenu(100, "POGRESAN");

            Assert.AreEqual(100.0, novaCijena);
        }

        [TestMethod]
        public void Kupon_KodSaRazmacima_UmanjujeCijenu()
        {
            var kupon = new Kupon
            {
                kod = "SPORT10",
                procenat = 10
            };

            double novaCijena = kupon.IzracunajCijenu(100, " SPORT10 ");

            Assert.AreEqual(90.0, novaCijena);
        }
        [TestMethod]
        public void Kupon_MalaSlova()
        {
            var kupon = new Kupon { kod = "SPORT10", procenat = 10 };

            Assert.AreEqual(90.0, kupon.IzracunajCijenu(100, "sport10"));
        }
        [TestMethod]
        public void Kupon_NeispravanProcenat()
        {
            var kupon = new Kupon { kod = "SPORT10", procenat = 150 };

            Assert.AreEqual(100.0, kupon.IzracunajCijenu(100, "SPORT10"));
        }

    }
}