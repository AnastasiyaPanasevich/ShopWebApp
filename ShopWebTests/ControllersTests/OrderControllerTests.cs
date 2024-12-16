using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using ShopWebApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Moq;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using ShopWebApp.Controllers;
using System.Text.Json;

namespace ShopWebTests.ControllersTests
{
    [TestFixture, NonParallelizable]
    public class OrderControllerTests
    {
        private ShopDatabase _db;
        private OrderController _controller;

        [SetUp]
        public void Setup()
        {
            // Set the environment variable to 'Testing' so OnConfiguring uses InMemoryDatabase
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

            var options = new DbContextOptionsBuilder<ShopDatabase>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Use a unique database name for each test
                .Options;

            _db = new ShopDatabase(options);
            _db.Database.EnsureDeleted();  // Ensure any old database is deleted
            _db.Database.EnsureCreated(); // Create a fresh database for each test

            // Mock TempDataDictionaryFactory
            var tempDataMock = new Mock<ITempDataDictionaryFactory>();
            var tempDataDictionaryMock = new Mock<ITempDataDictionary>();
            tempDataMock
                .Setup(factory => factory.GetTempData(It.IsAny<HttpContext>()))
                .Returns(tempDataDictionaryMock.Object);

            // Create a mock for ISession
            var sessionMock = new Mock<ISession>();

            // Mocking GetString method directly
            sessionMock.Setup(s => s.GetString(It.IsAny<string>())).Returns<string>(key => key == "_Cart" ? "{}" : null); // simulate empty cart for "_Cart"
            sessionMock.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>())).Verifiable(); // setup Set to be verifiable

            _controller = new OrderController(_db)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                        {
                            new Claim(ClaimTypes.Name, "testuser@example.com"),
                            new Claim(ClaimTypes.Role, "user")
                        }, "mock")),
                        Session = sessionMock.Object // Assign the mock session
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

        [Test]
        public void Index_WhenNoCartExists_CreatesEmptyCartInSession()
        {
            // Arrange
            var input = new OrderModel(); // Empty model to simulate no cart

            // Act
            var result = _controller.Index(input) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as OrderModel;
            Assert.IsNotNull(model);
            Assert.AreEqual("{}", _controller.HttpContext.Session.GetString("_Cart")); // Access Session directly from the instance
        }

        [Test]
        public void Index_WhenUserIsAuthenticated_PopulatesUserInfo()
        {
            // Arrange
            var mockUser = new User
            {
                UserId = 1,
                Email = "testuser@example.com",
                Name = "John",
                Surname = "Doe",
                Address = "123 Main St",
                Phone = "1234567890"
            };
            _db.Users.Add(mockUser);
            _db.SaveChanges();

            // Simulate user being logged in
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, mockUser.Email)
            }));

            // Act
            var result = _controller.Index(new OrderModel()) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as OrderModel;
            Assert.AreEqual(mockUser.Name, model.User.Name);
            Assert.AreEqual(mockUser.Surname, model.User.Surname);
        }

        [Test]
        public void SubmitOrder_WhenValidOrder_SubmitsOrderSuccessfully()
        {
            // Arrange
            var mockUser = new User
            {
                UserId = 1,
                Email = "testuser@example.com",
                Name = "John",
                Surname = "Doe",
                Address = "123 Main St",
                Phone = "1234567890"
            };
            _db.Users.Add(mockUser);
            _db.SaveChanges();

            var cartDict = new Dictionary<string, int> { { "product1", 2 } };
            _controller.HttpContext.Session.SetString("_Cart", JsonSerializer.Serialize(cartDict));

            var orderModel = new OrderModel
            {
                Order = new Order
                {
                    ShippingType = 1,
                    PaymentMethod = 1,
                    ClientName = "John",
                    ClientSurname = "Doe",
                    ClientEmail = "john.doe@example.com",
                    ClientPhone = "1234567890",
                    Address = "123 Main St"
                }
            };

            // Simulate user being logged in
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, mockUser.Email)
            }));

            // Act
            var result = _controller.SubmitOrder(orderModel) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Status", result.ActionName);
            Assert.AreEqual("order/" + orderModel.Order.Code, result.RouteValues["code"]);
        }

        [Test]
        public void SubmitOrder_WhenInvalidOrder_ReturnsValidationErrors()
        {
            // Arrange
            var orderModel = new OrderModel
            {
                Order = new Order
                {
                    ShippingType = 0, // Invalid shipping type
                    PaymentMethod = 4 // Invalid payment method
                }
            };

            // Act
            var result = _controller.SubmitOrder(orderModel) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as OrderModel;
            Assert.IsNotNull(model);
            Assert.IsTrue(model.Message.Contains("Incorrect delivery method"));
            Assert.IsTrue(model.Message.Contains("Incorrect payment method"));
        }

        [Test]
        public void Status_WhenOrderExists_ReturnsOrderDetails()
        {
            // Arrange
            var mockUser = new User
            {
                UserId = 1,
                Email = "testuser@example.com",
                Name = "John",
                Surname = "Doe"
            };
            _db.Users.Add(mockUser);
            _db.SaveChanges();

            var order = new Order
            {
                Code = "ORDER123",
                UserId = mockUser.UserId
            };
            _db.Orders.Add(order);
            _db.SaveChanges();

            // Simulate user being logged in
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, mockUser.Email)
            }));

            // Act
            var result = _controller.Status("ORDER123") as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as OrderModel;
            Assert.AreEqual(order.Code, model.Order.Code);
        }

        [Test]
        public void Status_WhenOrderDoesNotExist_Returns404()
        {
            // Act
            var result = _controller.Status("INVALIDCODE") as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Error", result.ControllerName);
            Assert.AreEqual("404", result.ActionName);
        }
    }
}
