using Microsoft.AspNetCore.Identity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace ESportskiCentar.Models
{
    public class Korisnik : IdentityUser
    {
        [Required(ErrorMessage = "Ime je obavezno polje.")]
        [StringLength(30, MinimumLength = 2, ErrorMessage = "Ime mora imati između 2 i 30 karaktera.")]
        [RegularExpression(@"^[A-Za-zČčĆćŽžŠšĐđ]{2,30}$", ErrorMessage = "Ime može sadržavati samo slova.")]
        [DisplayName("Ime")]
        public string ime { get; set; }

        [Required(ErrorMessage = "Prezime je obavezno polje.")]
        [StringLength(30, MinimumLength = 2, ErrorMessage = "Prezime mora imati između 2 i 30 karaktera.")]
        [RegularExpression(@"^[A-Za-zČčĆćŽžŠšĐđ]{2,30}$", ErrorMessage = "Prezime može sadržavati samo slova.")]
        [DisplayName("Prezime")]
        public string prezime { get; set; }
    }
}
