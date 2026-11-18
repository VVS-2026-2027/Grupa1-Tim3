namespace ESportskiCentar.Models
{
    public class Kupon
    {
        public string kod { get; set; } = "";
        public double procenat { get; set; }

        public double IzracunajCijenu(double cijena, string uneseniKod)
        {
            if (uneseniKod?.Trim() == kod)
            {
                return cijena - (cijena * procenat / 100);
            }

            return cijena;
        }
    }
}