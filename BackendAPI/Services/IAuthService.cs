namespace BackendAPI.Services
{
    public interface IAuthService
    {
        Task<bool> RevokeRefreshTokenAsync(int userId, string refreshToken);
    }

}
