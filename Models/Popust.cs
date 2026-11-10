using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace ESportskiCentar.Models
{
    public class Popust
    {
        [Key]
        public int id { get; set; }

        [Required(ErrorMessage = "Potreban broj rezervacija je obavezan.")]
        [Range(0, int.MaxValue, ErrorMessage = "Potreban broj rezervacija mora biti najmanje 0.")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Broj rezervacija mora biti broj!")]
        [DisplayName("Potreban broj rezervacija")]
        public int potrebanBrojRezervacija { get; set; }

        [Required(ErrorMessage = "Procenat popusta je obavezan.")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Procenat mora biti broj!")]
        [Range(0.1, 100.00, ErrorMessage = "Procenat popusta mora biti između 0.1 i 100.")]
        [DisplayName("Procenat popusta")]
        public double procenat { get; set; }
    }
}
