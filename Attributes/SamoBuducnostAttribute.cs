using System;
using System.ComponentModel.DataAnnotations;
using ESportskiCentar.Helpers;

namespace ESportskiCentar.Attributes
{
    public class SamoBuducnostAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is DateTime uneseniDatum)
            {
                if (uneseniDatum < VrijemeHelper.SadaLokalno())
                {
                    return new ValidationResult(ErrorMessage ?? "Datum i vrijeme moraju biti u budućnosti.");
                }
            }
            return ValidationResult.Success;
        }
    }
}