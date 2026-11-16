using ESportskiCentar.Models;

namespace ESportskiCentar.Services
{
    public class StatistikaPotrosnjeServis
    {
        public record StatistikaPotrosnje(
        int BrojRezervacija,
        double Ukupno,
        double Prosjek);

            public static StatistikaPotrosnje Izracunaj(
                IEnumerable<Rezervacija> rezervacije,
                string korisnikId)
            {
                return new StatistikaPotrosnje(0, 0, 0);
            }
        
    }
}
