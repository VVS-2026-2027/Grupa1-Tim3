using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ESportskiCentar.Models
{
    public class Rezervacija
    {
        [Key]
        public int id { get; set; }

        [Required(ErrorMessage = "Status rezervacije je obavezan.")]
        [DisplayName("Status")]
        public Status status { get; set; }

        [Required(ErrorMessage = "Termin je obavezan.")]
        [ForeignKey("Termin")]
        [DisplayName("Termin")]
        public int terminID { get; set; }

        [Required(ErrorMessage = "Korisnik je obavezan.")]
        [ForeignKey("Korisnik")]
        [DisplayName("Korisnik")]
        public string korisnikID { get; set; } 

        [Required]
        [DisplayName("Datum i vrijeme kreiranja rezervacije")]
        [DataType(DataType.DateTime)]
        public DateTime vrijemeRezervacije { get; set; } = DateTime.Now; 

        [Required]
        [DisplayName("Popust")]
        public bool primjenjenPopust { get; set; } = false;

        [Required(ErrorMessage = "Konačna cijena je obavezna.")]
        [Range(0.00, double.MaxValue, ErrorMessage = "Cijena ne može biti negativna.")]
        [DisplayName("Ukupna cijena")]
        public double konacnaCijena { get; set; }

        public Termin? Termin { get; set; }
        public Korisnik? Korisnik { get; set; }
    }
}