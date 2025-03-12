using System.Text.RegularExpressions;

namespace BackendAPI.Utils
{
    public static class EmailValidator
    {
        private static readonly List<string> AllowedDomainPrefixes = new()
        {
            "gmail.", "outlook.", "hotmail.", "yahoo.", "icloud.", "live.", "msn.", "aol."
        };

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            email = email.Trim().ToLower();
            var parts = email.Split('@');
            if (parts.Length != 2) return false; // 檢查是否有 "@" 符號

            var domain = parts[1];
            return AllowedDomainPrefixes.Any(prefix => domain.StartsWith(prefix));
        }
    }
}
