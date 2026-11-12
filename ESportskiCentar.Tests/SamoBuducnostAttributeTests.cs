using ESportskiCentar.Attributes;
using ESportskiCentar.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ESportskiCentar.Tests
{
    [TestClass]
    public class SamoBuducnostAttributeTests
    {
        [TestMethod]
        public void DatumUProslosti_NijeValidan()
        {
            var atribut = new SamoBuducnostAttribute();
            var kontekst = new ValidationContext(new object());
            DateTime datum = VrijemeHelper.SadaLokalno().AddDays(-1);

            var rezultat = atribut.GetValidationResult(datum, kontekst);
            Assert.IsNotNull(rezultat);//rezultat mora biti gresk
            Assert.AreEqual("Datum i vrijeme moraju biti u budućnosti.",
                           rezultat.ErrorMessage);


        }

        [TestMethod]
        public void DatumUBuducnosti_Validan()
        {
            var atribut = new SamoBuducnostAttribute();
            var kontekst = new ValidationContext(new object());
            DateTime datum = VrijemeHelper.SadaLokalno().AddDays(+1);

            var rezultat = atribut.GetValidationResult(datum, kontekst);
            Assert.IsNull(rezultat);//rezultat mora ostati null jer ono vraca null za uspjeh
            
      

        }
        [TestMethod]
        public void DatumUProslosti_PrilagodjenaPoruka()
        {//test sa prilagodjenom porukoma
            var atribut = new SamoBuducnostAttribute
            {
                ErrorMessage = "Neispravan datum"
            };

            var kontekst = new ValidationContext(new object());
            DateTime datum = VrijemeHelper.SadaLokalno().AddDays(-1);

            
            var rezultat = atribut.GetValidationResult(datum, kontekst);

            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Neispravan datum", rezultat.ErrorMessage);
        }

        [TestMethod]
        public void TestKadNijeDate()
        {
            var atribut = new SamoBuducnostAttribute();
            var kontekst = new ValidationContext(new object());
            string vrijednost = "Nije datum";
                var rezultat=atribut.GetValidationResult(vrijednost, kontekst);
            Assert.IsNull (rezultat);
        }
    }
}
