using ESportskiCentar.Models;

namespace ESportskiCentar.Services
{
    public class StatistikaPotrosnjeServis
    {
        public record StatistikaPotrosnje(int BrojRezervacija, double Ukupno, double Prosjek);
        public static StatistikaPotrosnje Izracunaj(IEnumerable<Rezervacija> rezervacije, string korisnikId)
        {
            var lista = rezervacije.Where(r => r.korisnikID == korisnikId).ToList();
            int broj = lista.Count;

            if (broj == 0)
                return new StatistikaPotrosnje(0, 0, 0);

            double ukupno = lista.Sum(r => r.konacnaCijena);
            double prosjek = ukupno / broj;

            return new StatistikaPotrosnje(broj, ukupno, prosjek);
        }

    }
}
