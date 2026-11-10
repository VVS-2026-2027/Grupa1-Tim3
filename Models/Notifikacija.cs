using ESportskiCentar.Attributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ESportskiCentar.Models
{
    public class Notifikacija
    {
        [Key]
        public int id { get; set; }

        [Required]
        [DisplayName("Status slanja")]
        public bool poslana { get; set; } = false;  // default nije poslana

        [Required(ErrorMessage = "Vrijeme slanja je obavezno.")]
        [DisplayName("Predviđeno vrijeme slanja")]
        [SamoBuducnost(ErrorMessage = "Vrijeme slanja mora biti unaprijed, ne možete izabrati prošlost!")]
        public DateTime vrijemeSlanja { get; set; }

        [Required(ErrorMessage = "Rezervacija je obavezna.")]
        [ForeignKey("Rezervacija")]
        [DisplayName("Rezervacija")]
        public int rezervacijaID { get; set; }

        public Rezervacija? Rezervacija { get; set; }

    }
}
