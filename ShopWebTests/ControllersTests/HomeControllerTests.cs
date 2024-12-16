using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using ShopWebApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Moq;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace ShopWebTests.ControllersTests
{
    [TestFixture, NonParallelizable]
    public class HomeControllerTests
    {
        private ShopDatabase _db;
        private HomeController _controller;

        [SetUp]
        public void Setup()
        {
            // Устанавливаем тестовую среду
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

            var options = new DbContextOptionsBuilder<ShopDatabase>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Уникальное имя базы данных для каждого теста
                .Options;

            _db = new ShopDatabase(options);
            _db.Database.EnsureDeleted();
            _db.Database.EnsureCreated();

            // Мок TempData
            var tempDataMock = new Mock<ITempDataDictionaryFactory>();
            var tempDataDictionaryMock = new Mock<ITempDataDictionary>();
            tempDataMock
                .Setup(factory => factory.GetTempData(It.IsAny<HttpContext>()))
                .Returns(tempDataDictionaryMock.Object);

            _controller = new HomeController
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                        {
                            new Claim(ClaimTypes.Name, "testuser@example.com"),
                            new Claim(ClaimTypes.Role, "user")
                        }, "mock"))
                    }
                },
                TempData = tempDataMock.Object.GetTempData(new DefaultHttpContext())
            };
        }

        [TearDown]
        public void Teardown()
        {
            _db.Dispose();
            _controller?.Dispose();
        }
        private void MockAuthenticatedUser(string email)
        {
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, email)
    };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }


        [Test]
        public void Error_WithValidCode_ReturnsErrorPage()
        {
            // Arrange
            var errorCode = 404;

            // Act
            var result = _controller.Error(errorCode) as ViewResult;

            // Assert
            Assert.IsNotNull(result, "Result should not be null.");
            var model = result.Model as ErrorViewModel;
            Assert.IsNotNull(model, "Model should not be null.");
            Assert.AreEqual("An error occurred", model.Title, "Title mismatch.");
            Assert.AreEqual(errorCode, model.ErrorCode, "Error code mismatch.");
            Assert.AreEqual("Sorry, but the page with the given address does not exist", model.AboutError, "Error description mismatch.");
        }
        [Test]
        public void Index_WhenNoProductsAvailable_ReturnsViewWithNoProductMessage()
        {
            // Arrange
            _db.Products.RemoveRange(_db.Products);
            _db.Users.Add(new User { Email = "test@example.com", Name = "Test", Surname = "User" });
            _db.SaveChanges();

            MockAuthenticatedUser("test@example.com");

            // Act
            var result = _controller.Index() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(false, result.ViewData["productExists"]);
        }


        [Test]
        public void Index_WhenProductsExist_ReturnsViewWithRandomProduct()
        {
            // Arrange
            var mockProduct = new Product
            {
                ProductId = 1,
                Enabled = true,
                Stock = 5,
                Name = "Test Product",
                Brand = "Test Brand"
            };
            _db.Products.Add(mockProduct);
            _db.SaveChanges();

            // Act
            var result = _controller.Index() as ViewResult;

            // Assert
            Assert.IsNotNull(result, "Result should not be null.");
            Assert.IsNotNull(result.ViewData, "ViewData should not be null.");
            Assert.AreEqual(true, result.ViewData["productExists"], "Expected productExists to be true.");

            // Check ViewBag.product (random product)
            Assert.IsTrue(result.ViewData.ContainsKey("product"), "ViewData should contain 'product'.");
            var product = result.ViewData["product"] as Product;
            Assert.IsNotNull(product, "Random product should not be null.");
            Assert.AreEqual(mockProduct.ProductId, product.ProductId, "Random product ID mismatch.");
        }


        [Test]
        public void Index_WhenCategoriesExist_ReturnsViewWithCategories()
        {
            // Arrange
            var mockCategory = new Category
            {
                CategoryId = 1,
                Name = "Electronics",
                Code = "E1",
                Enabled = true
            };
            _db.Categories.Add(mockCategory);
            _db.SaveChanges();

            // Act
            var result = _controller.Index() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ViewData);
            Assert.AreEqual(true, result.ViewData["categoriesExist"]);
            var categories = result.ViewData["categories"] as Dictionary<string, string>;
            Assert.IsNotNull(categories);
            Assert.IsTrue(categories.ContainsKey("Electronics"));
        }


        [Test]
        public void Index_WhenUserIsAuthenticated_ReturnsViewWithUserData()
        {
            // Arrange
            var mockUser = new User
            {
                Email = "testuser@example.com",
                Name = "John",
                Surname = "Doe",
                UserId = 1
            };

            _db.Users.Add(mockUser);
            _db.SaveChanges();

            // Act
            var result = _controller.Index() as ViewResult;

            // Assert
            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = result as ViewResult;
            Assert.IsNotNull(viewResult);
            var model = viewResult.Model as BaseViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual("Shop", model?.Title);
            Assert.AreEqual("John", model?.User.Name);
            Assert.AreEqual("Doe", model?.User.Surname);
            Assert.AreEqual("testuser@example.com", model?.User.Email);
        }
    }
}
