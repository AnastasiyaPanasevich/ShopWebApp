using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using ShopWebApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Moq;
// типа Add Edit Delete
namespace ShopWebTests.ControllersTests
{
    [TestFixture, NonParallelizable]
    public class AdminControllerAEDTests
    {
        private ShopDatabase _db;
        private AdminController _controller;

        [SetUp]
        public void Setup()
        {
            // Настройка in-memory базы данных для тестов
            var options = new DbContextOptionsBuilder<ShopDatabase>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Уникальное имя базы данных для каждого теста
                .Options;

            _db = new ShopDatabase(options);
            _db.Database.EnsureDeleted();
            _db.Database.EnsureCreated();

            // Добавляем данные для пользователя и категорий
            var mockUser = new User
            {
                UserId = 1,
                Email = "admin@example.com",
                Name = "Admin",
                Surname = "User",
                Role = "admin"
            };
            _db.Users.Add(mockUser);

            var mockCategory = new Category
            {
                CategoryId = 1,
                Name = "Electronics",
                Code = "E1",
                Enabled = true
            };
            _db.Categories.Add(mockCategory);

            _db.SaveChanges();

            // Мокируем данные пользователя для контроллера
            _controller = new AdminController()
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                        {
                            new Claim(ClaimTypes.Name, "admin@example.com"),
                            new Claim(ClaimTypes.Role, "admin")
                        }, "mock"))
                    }
                }
            };
        }

        [TearDown]
        public void Teardown()
        {
            _db?.Dispose();
            _controller?.Dispose();
        }

        [Test]
        public void Index_WhenCategoriesExist_ReturnsViewWithCategories()
        {
            // Act
            var result = _controller.Index() as ViewResult;

            // Assert
            Assert.IsNotNull(result); // Проверяем, что результат не null
            Assert.IsNotNull(result.ViewData); // Проверяем, что ViewData не null
            Assert.AreEqual(true, result.ViewData["categoriesExist"]); // Проверяем, что категория существует
            var categories = result.ViewData["categories"] as Dictionary<string, string>;
            Assert.IsNotNull(categories); // Проверяем, что категории существуют
            Assert.IsTrue(categories.ContainsKey("Electronics")); // Проверяем, что категория "Electronics" присутствует
        }
    }
}
