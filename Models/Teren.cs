using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ESportskiCentar.Models
{
    public class Teren
    {
        [Key]
        public int id { get; set; }

        [Required(ErrorMessage = "Naziv terena je obavezan.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Naziv terena mora imati između 3 i 100 karaktera.")]
        [DisplayName("Naziv terena")]
        public string naziv { get; set; }

        [Required(ErrorMessage = "Morate izabrati sport za ovaj teren.")]
        [DisplayName("Sport")]
        public Sport sport { get; set; }
    }
}