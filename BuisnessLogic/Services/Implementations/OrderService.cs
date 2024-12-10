using BuisnessLogic.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using ShopWebApp;


namespace BuisnessLogic.Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationContext _context;

        public OrderService(ApplicationContext context)
        {
            _context = context;
        }

        public Order? GetOrderById(int id) =>
            _context.Orders
                .Include(o => o.ProductOrders)
                .FirstOrDefault(o => o.OrderId == id);

        public Order? GetOrderByCode(string code) =>
            _context.Orders
                .Include(o => o.ProductOrders)
                .FirstOrDefault(o => o.Code == code);

        public void AddOrder(Order order)
        {
            _context.Orders.Add(order);
            _context.SaveChanges();
        }

        public void UpdateOrder(Order order)
        {
            _context.Orders.Update(order);
            _context.SaveChanges();
        }

        public void DeleteOrder(int orderId)
        {
            var order = GetOrderById(orderId);
            if (order != null)
            {
                _context.Orders.Remove(order);
                _context.SaveChanges();
            }
        }

        public List<Order> GetAllOrders() =>
            _context.Orders
                .Include(o => o.ProductOrders)
                .OrderByDescending(o => o.DateOfOrder)
                .ToList();

        public List<Order> GetOrdersByUserId(int userId) =>
            _context.Orders
                .Include(o => o.ProductOrders)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.DateOfOrder)
                .ToList();
    }

}
