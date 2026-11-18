using ESportskiCentar.Models;

namespace ESportskiCentar.Services
{
    public class StatistikaPotrosnjeServis
    {
        public record StatistikaPotrosnje(int BrojRezervacija, double Ukupno, double Prosjek);
        public static StatistikaPotrosnje Izracunaj(IEnumerable<Rezervacija> rezervacije, string korisnikId)
        {
            var lista = rezervacije.ToList();
            if (lista.Count == 0)
                return new StatistikaPotrosnje(0, 0, 0);
            return new StatistikaPotrosnje(1, lista[0].konacnaCijena, lista[0].konacnaCijena);
        }

    }
}
