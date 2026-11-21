using System;

namespace ESportskiCentar.Helpers
{
    public static class VrijemeHelper
    {
        // napravljeno da bi se vrijeme servera moglo prebaciti na nasu vremensku zonu
        public static DateTime SadaLokalno()
        {
            TimeZoneInfo zona;

            zona = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"); 

            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zona);
        }
    }
}