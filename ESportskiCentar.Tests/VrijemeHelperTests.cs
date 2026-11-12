using ESportskiCentar.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ESportskiCentar.Tests
{
    [TestClass]
    public class VrijemeHelperTests
    {
        [TestMethod]
        public void TestMethod1()
        {//ne mogu vise postici od 77% jer nisam na linux
            DateTime prije = DateTime.UtcNow;

            
            DateTime rezultat = VrijemeHelper.SadaLokalno();

            DateTime poslije = DateTime.UtcNow;

            TimeZoneInfo zona = TimeZoneInfo.FindSystemTimeZoneById(
                "Central European Standard Time");
            DateTime ocekivanoPrije =
                TimeZoneInfo.ConvertTimeFromUtc(prije, zona);

            DateTime ocekivanoPoslije =
                TimeZoneInfo.ConvertTimeFromUtc(poslije, zona);
            Assert.IsTrue(rezultat >= ocekivanoPrije &&
                       rezultat <= ocekivanoPoslije);
        }
    }
}