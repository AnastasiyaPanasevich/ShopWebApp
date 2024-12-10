using BuisnessLogic.Services.Interfaces;
using ShopWebApp;

namespace BuisnessLogic.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly ApplicationContext _context;

        public UserService(ApplicationContext context)
        {
            _context = context;
        }

        public User? GetUserByEmail(string email) =>
            _context.Users.FirstOrDefault(u => u.Email == email);

        public User? GetUserById(int id) =>
            _context.Users.FirstOrDefault(u => u.UserId == id);

        public void UpdateUser(User user)
        {
            _context.Users.Update(user);
            _context.SaveChanges();
        }

        public void DeleteUser(int userId)
        {
            var user = GetUserById(userId);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
            }
        }

        public List<User> GetAllUsers() =>
            _context.Users.ToList();
    }

}