namespace ShopWebApp.Services.Interfaces
{
    public interface ICategoryService
    {
        Category? GetCategoryById(int id);
        Category? GetCategoryByName(string name);
        void AddCategory(Category category);
        void UpdateCategory(Category category);
        void DeleteCategory(int categoryId);
        List<Category> GetAllCategories();
    }
}
