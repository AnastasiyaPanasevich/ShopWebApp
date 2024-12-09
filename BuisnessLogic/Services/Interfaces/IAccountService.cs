using ShopWebApp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuisnessLogic.Services.Interfaces
{
    public interface IAccountService
    {
        User FindUserByEmail(string email); 
        List<Order> GetUserOrders(int userId);
        bool IsEmailFree(string email);
        void UpdateUser(User user);
        void AddUser(User user);
        string HashPassword(string password);
        bool ValidateCredentials(string email, string password);
    }
}
