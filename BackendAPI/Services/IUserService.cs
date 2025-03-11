namespace BackendAPI.Services
{
    public interface IUserService
    {
        Task<bool> ChangePasswordAsync(int userId, string newPassword);
    }
}
