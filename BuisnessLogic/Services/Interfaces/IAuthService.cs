using ShopWebApp;

namespace BuisnessLogic.Services.Interfaces
{
    public interface IAuthService
    {
        Task<bool> RegisterAsync(AccountDTO input);
        Task<bool> LoginAsync(AccountDTO input, bool rememberMe, string returnUrl);
        Task LogoutAsync();
        bool AreCredentialsValid(string login, string password);
        bool IsEmailFree(string email);
        string Sha256Hash(string password);
    }
}
