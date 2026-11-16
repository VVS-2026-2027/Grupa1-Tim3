using ESportskiCentar.Models;
using System.ComponentModel.DataAnnotations;

namespace ESportskiCentar.Attributes
{
    public class ValidacijaRasponaDvaDatuma : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var model = validationContext.ObjectInstance as Izvjestaj;

            if (model == null)
            {
                return ValidationResult.Success;
            }
            if (model.datumOd <= model.datumDo)
            {
                return ValidationResult.Success;
            }
            return new ValidationResult(ErrorMessage ?? "Datum kraja ne može biti prije datuma početka!");
        }
    }
}