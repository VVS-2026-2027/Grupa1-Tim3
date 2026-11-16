using ESportskiCentar.Attributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ESportskiCentar.Models
{
    public class Izvjestaj
    {
        [Key]
        public int id { get; set; }

        [Required]
        [DisplayName("Datum generisanja")]
        [DataType(DataType.DateTime)]
        public DateTime datumGenerisanja { get; set; }

        [Required(ErrorMessage = "Korisnik je obavezan.")]
        [DisplayName("Korisnik")]
        [ForeignKey("Korisnik")]
        public String korisnikID { get; set; }

        [Required(ErrorMessage = "Početni datum je obavezan.")]
        [DisplayName("Od datuma")]
        [DataType(DataType.Date)]
        [ValidacijaRasponaDvaDatuma]
        public DateTime datumOd { get; set; }

        [Required(ErrorMessage = "Krajnji datum je obavezan.")]
        [DisplayName("Do datuma")]
        [DataType(DataType.Date)]
        public DateTime datumDo { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Broj rezervacija ne može biti negativan.")]
        [DisplayName("Ukupno rezervacija")]
        public int ukupnoRezervacija { get; set; }

        [Required]
        [Range(0.0, double.MaxValue, ErrorMessage = "Ukupna zarada ne može biti negativna.")]
        [DisplayName("Ukupna zarada")]
        public double ukupnaZarada { get; set; }
        public Korisnik? Korisnik { get; set; }
    }
}