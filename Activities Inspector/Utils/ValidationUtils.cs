namespace ProgettoInformaticaForense_Argentieri.Utils
{
    public static class ValidationUtils
    {
        internal static bool IsValidStringInput(string input)
            => string.IsNullOrEmpty(input) == false;
    }
}
