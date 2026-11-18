namespace ESportskiCentar.Models
{
    public class Kupon
    {
        public string kod { get; set; } = "";
        public double procenat { get; set; }

        public double IzracunajCijenu(double cijena, string uneseniKod)
        {
            if (procenat < 0 || procenat > 100)
                return cijena;

            if (string.Equals(uneseniKod?.Trim(), kod,
                StringComparison.OrdinalIgnoreCase))
            {
                return cijena - cijena * procenat / 100;
            }

            return cijena;
        }
    }
}