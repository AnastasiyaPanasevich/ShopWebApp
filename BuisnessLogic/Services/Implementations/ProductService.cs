using BuisnessLogic.Services.Interfaces;
using ShopWebApp;

namespace BuisnessLogic.Services.Implementations
{
    public class ProductService : IProductService
    {
        private readonly ApplicationContext _context;

        public ProductService(ApplicationContext context)
        {
            _context = context;
        }

        public Product? GetProductById(int id) =>
            _context.Products.FirstOrDefault(p => p.ProductId == id);

        public Product? GetProductByCode(string code) =>
            _context.Products.FirstOrDefault(p => p.Code == code);

        public void AddProduct(Product product)
        {
            _context.Products.Add(product);
            _context.SaveChanges();
        }

        public void UpdateProduct(Product product)
        {
            _context.Products.Update(product);
            _context.SaveChanges();
        }

        public void DeleteProduct(int productId)
        {
            var product = GetProductById(productId);
            if (product != null)
            {
                _context.Products.Remove(product);
                _context.SaveChanges();
            }
        }

        public List<Product> GetAllProducts() =>
            _context.Products.ToList();

        // TODO
        public List<Product> GetProductsByCategoryId(int categoryId) =>
            _context.Products.Where(p => p.SubcategoryId == categoryId).ToList();
    }

}
