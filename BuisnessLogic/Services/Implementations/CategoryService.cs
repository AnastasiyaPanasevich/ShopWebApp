using ShopWebApp.Services.Interfaces;

namespace ShopWebApp.Services.Implementations
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationContext _context;

        public CategoryService(ApplicationContext context)
        {
            _context = context;
        }

        public Category? GetCategoryById(int id) =>
            _context.Categories.FirstOrDefault(c => c.CategoryId == id);

        public Category? GetCategoryByName(string name) =>
            _context.Categories.FirstOrDefault(c => c.Name == name);

        public void AddCategory(Category category)
        {
            _context.Categories.Add(category);
            _context.SaveChanges();
        }

        public void UpdateCategory(Category category)
        {
            _context.Categories.Update(category);
            _context.SaveChanges();
        }

        public void DeleteCategory(int categoryId)
        {
            var category = GetCategoryById(categoryId);
            if (category != null)
            {
                _context.Categories.Remove(category);
                _context.SaveChanges();
            }
        }

        public List<Category> GetAllCategories() =>
            _context.Categories.ToList();
    }

}
