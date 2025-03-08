namespace BackendAPI.Services
{
    public interface IEmailService
    {
        // 定義發送郵件的方法
        Task SendAsync(string to, string subject, string body);
    }
}
