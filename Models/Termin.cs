using ESportskiCentar.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ESportskiCentar.Models
{
    public class Termin
    {
        [Key]
        public int id { get; set; }

        [Required(ErrorMessage = "Datum i vrijeme termina su obavezni.")]
        [DisplayName("Datum termina")]
        [SamoBuducnost(ErrorMessage = "Termin se ne može kreirati u prošlosti! Izaberite buduće vrijeme.")]
        public DateTime datum { get; set; }

        [Required(ErrorMessage = "Cijena termina je obavezna.")]
        [Range(0.00, double.MaxValue, ErrorMessage = "Cijena ne može biti negativna.")]
        [DisplayName("Cijena termina")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Cijena mora biti broj (npr. 15 ili 15.50)!")]
        public double cijena { get; set; }

        [Required]
        [DisplayName("Status termina")]
        public bool rezervisan { get; set; } = false; // default je slobodan

        [Required(ErrorMessage = "Izbor terena je obavezan.")]
        [DisplayName("Teren")]
        [ForeignKey("Teren")]
        public int terenID { get; set; }

        public Teren? Teren { get; set; }
    }
}