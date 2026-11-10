using System;

namespace ESportskiCentar.Helpers
{
    public static class VrijemeHelper
    {
        // napravljeno da bi se vrijeme servera moglo prebaciti na nasu vremensku zonu
        public static DateTime SadaLokalno()
        {
            TimeZoneInfo zona;
            try
            {
                // za windows
                zona = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"); 
            }
            catch
            {
                // za linux
                zona = TimeZoneInfo.FindSystemTimeZoneById("Europe/Belgrade");
            }

            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zona);
        }
    }
}