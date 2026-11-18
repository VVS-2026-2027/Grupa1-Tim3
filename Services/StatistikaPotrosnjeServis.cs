using ESportskiCentar.Models;

namespace ESportskiCentar.Services
{
    public class StatistikaPotrosnjeServis
    {
        public record StatistikaPotrosnje(int BrojRezervacija, double Ukupno, double Prosjek);
        public static StatistikaPotrosnje Izracunaj(IEnumerable<Rezervacija> rezervacije, string korisnikId)
        {
            var korisnikoveRezervacije = rezervacije.Where(r => r.korisnikID == korisnikId).ToList();

            int broj = korisnikoveRezervacije.Count;
            double ukupno = korisnikoveRezervacije.Sum(r => r.konacnaCijena);
            double prosjek = broj == 0 ? 0 : ukupno / broj;

            return new StatistikaPotrosnje(broj, ukupno, prosjek);
        }

    }
}
