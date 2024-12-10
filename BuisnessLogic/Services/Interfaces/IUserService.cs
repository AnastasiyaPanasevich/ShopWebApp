using ShopWebApp;


namespace BuisnessLogic.Services.Interfaces
{
    public interface IUserService
    {
        User? GetUserByEmail(string email);
        User? GetUserById(int id);
        void UpdateUser(User user);
        void DeleteUser(int userId);
        List<User> GetAllUsers();
    }
}
