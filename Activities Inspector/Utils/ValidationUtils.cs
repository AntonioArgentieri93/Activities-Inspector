namespace Activities_Inspector.Utils
{
    public static class ValidationUtils
    {
        internal static bool IsValidStringInput(string input)
            => string.IsNullOrEmpty(input) == false;
    }
}
