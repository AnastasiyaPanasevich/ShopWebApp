using ShopWebApp;

namespace BuisnessLogic.Services.Interfaces
{
    public interface IProductService
    {
        Product? GetProductById(int id);
        Product? GetProductByCode(string code);
        void AddProduct(Product product);
        void UpdateProduct(Product product);
        void DeleteProduct(int productId);
        List<Product> GetAllProducts();
    }
}