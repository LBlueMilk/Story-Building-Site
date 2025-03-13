using System.Net.Mail;
using System.Text.RegularExpressions;

namespace BackendAPI.Utils
{
    public static class EmailValidator
    {
        private static readonly List<string> AllowedDomains = new()
        {
            "gmail.com", "outlook.com", "hotmail.com", "yahoo.com", "icloud.com", "live.com", "msn.com", "aol.com"
        };

        private static readonly Regex EmailRegex = new(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            email = email.Trim().ToLower();

            // 1️ 正則表達式檢查基本格式
            if (!EmailRegex.IsMatch(email)) return false;

            // 2️ 使用 MailAddress 進一步驗證
            try
            {
                var mail = new MailAddress(email);
                string domain = mail.Host;

                // 3️ 檢查域名是否在允許清單內
                return AllowedDomains.Contains(domain);
            }
            catch
            {
                return false; // 若拋出錯誤，表示不是合法 Email
            }
        }
    }
}
