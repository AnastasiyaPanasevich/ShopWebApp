using ShopWebApp;

namespace BuisnessLogic.Services.Interfaces
{
    public interface IOrderService
    {
        Order? GetOrderById(int id);
        Order? GetOrderByCode(string code);
        void AddOrder(Order order);
        void UpdateOrder(Order order);
        void DeleteOrder(int orderId);
        List<Order> GetAllOrders();
    }
}