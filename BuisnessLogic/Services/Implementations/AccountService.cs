using BuisnessLogic.Services.Interfaces;
using ShopWebApp;
using System.Text;

namespace BuisnessLogic.Services.Implementations
{
    public class AccountService : IAccountService
    {
        private readonly ApplicationContext _db;

        public AccountService(ApplicationContext db)
        {
            _db = db;
        }

        public User? FindUserByEmail(string email) =>
            _db.Users.FirstOrDefault(u => u.Email == email);
        public List<Order> GetUserOrders(int userId)
        {
            var user = _db.Users.FirstOrDefault(u => u.UserId == userId);
            if (user != null)
            {
                return _db.Orders.Where(o => o.UserId == user.UserId).OrderByDescending(o => o.DateOfOrder).ToList();
            }
            return new List<Order>();
        }
        public bool IsEmailFree(string email) =>
        _db.Users.All(u => u.Email != email);

        public void UpdateUser(User user)
        {
            _db.Users.Update(user);
            _db.SaveChanges();
        }

        public void AddUser(User user)
        {
            _db.Users.Add(user);
            _db.SaveChanges();
        }

        public string HashPassword(string password)
        {
            using var hasher = System.Security.Cryptography.SHA256.Create();
            var hashed = hasher.ComputeHash(Encoding.UTF8.GetBytes(password));
            return string.Concat(hashed.Select(b => b.ToString("X2")));
        }

        public bool ValidateCredentials(string email, string password)
        {
            var user = FindUserByEmail(email);
            return user != null && user.Password == HashPassword(password);
        }
    }
}
