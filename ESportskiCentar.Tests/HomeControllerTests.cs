
using ESportskiCentar.Controllers;
using ESportskiCentar.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace ESportskiCentar.Tests
{
    [TestClass]
    public class HomeControllerTests
    {
        [TestMethod]
        public void Index_VracaView()
        {
            var kontroler=new HomeController();
            var rez = kontroler.Index();

            Assert.IsInstanceOfType(rez, typeof(ViewResult));

        }
        [TestMethod]
        public void Error_BezAktivnosti()
        {
            var prethodna = Activity.Current;

            try
            {
                Activity.Current = null;//nikakve aktivnosti

                var kontroler = new HomeController();

                kontroler.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        TraceIdentifier = "TEST123"
                    }
                };

                var rezultat = kontroler.Error();

                Assert.IsInstanceOfType(rezultat, typeof(ViewResult));

                var view = (ViewResult)rezultat;
                var model = view.Model as ErrorViewModel;

                Assert.IsNotNull(model);
                Assert.AreEqual("TEST123", model.RequestId);//ako nema id onda default je traceidentifier, i provjeri jel dobio taj req id 
            }
            finally
            {
                Activity.Current = prethodna;//na kraju uvijek vrati aktivnost
            }
        }

        [TestMethod]
        public void Error_SaAktivnosti()
        {
            var prethodna = Activity.Current;

            try
            {
                var aktivnost = new Activity("Test");
                aktivnost.Start();//nova aktivnost, ona postaje current kad startam

                var kontroler = new HomeController();

                kontroler.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        TraceIdentifier = "HTTP-123"
                    }
                };

                var rezultat = kontroler.Error();

                Assert.IsInstanceOfType(rezultat, typeof(ViewResult));

                var view = (ViewResult)rezultat;
                var model = view.Model as ErrorViewModel;

                Assert.IsNotNull(model);
                Assert.AreEqual(aktivnost.Id, model.RequestId);
            }
            finally
            {
                Activity.Current = prethodna;
            }
        }


    }

}
