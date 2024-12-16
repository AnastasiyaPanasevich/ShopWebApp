using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using ShopWebApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Authentication;
using Moq;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace ShopWebTests.ControllersTests
{
    [TestFixture, NonParallelizable] // Prevent tests from running in parallel
    public class AccountControllerTests
    {
        private ShopDatabase _db;
        private AccountController _controller;

        [SetUp]
        public void Setup()
        {
            // Set the environment variable to 'Testing' so OnConfiguring uses InMemoryDatabase
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

            var options = new DbContextOptionsBuilder<ShopDatabase>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Use a unique database name for each test
                .Options;

            _db = new ShopDatabase(options); // Initialize ShopDatabase with in-memory options
            _db.Database.EnsureDeleted();  // Ensure any old database is deleted
            _db.Database.EnsureCreated(); // Create a fresh database for each test

            // Mock TempDataDictionaryFactory
            var tempDataMock = new Mock<ITempDataDictionaryFactory>();
            var tempDataDictionaryMock = new Mock<ITempDataDictionary>();
            tempDataMock
                .Setup(factory => factory.GetTempData(It.IsAny<HttpContext>()))
                .Returns(tempDataDictionaryMock.Object);

            // Create controller with mocks
            _controller = new AccountController
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
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

        [Test]
        public void Index_WhenUserIsAuthenticated_ReturnsViewResult()
        {
            // Arrange
            var mockUser = new User
            {
                Email = "testuser@example.com",
                Name = "John",
                Surname = "Doe",
                Address = "123 Test St",
                Phone = "123456789",
                UserId = 1
            };

            _db.Users.Add(mockUser);
            _db.SaveChanges();

            // Act
            var result = _controller.Index();

            // Assert
            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = result as ViewResult;
            Assert.IsNotNull(viewResult);
            Assert.IsInstanceOf<AccountModel>(viewResult.Model);
        }

        [Test]
        public async Task TryLogin_WithValidCredentials_RedirectsToHomePage()
        {
            // Arrange
            var loginModel = new AccountModel { Login = "testuser@example.com", Password = "ValidPassword" };

            _db.Users.Add(new User
            {
                Email = loginModel.Login,
                Password = AccountController.Sha256Hash(loginModel.Password),
                Role = "user"
            });
            _db.SaveChanges();

            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper
                .Setup(m => m.Action(It.Is<UrlActionContext>(u => u.Action == "index" && u.Controller == "home")))
                .Returns("/home/index");

            _controller.Url = mockUrlHelper.Object;

            var mockAuthService = new Mock<IAuthenticationService>();
            var serviceProviderMock = new Mock<IServiceProvider>();

            serviceProviderMock
                .Setup(sp => sp.GetService(typeof(IAuthenticationService)))
                .Returns(mockAuthService.Object);

            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProviderMock.Object
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = await _controller.TryLogin(loginModel, null);

            // Assert
            Assert.IsInstanceOf<RedirectResult>(result);
            var redirectResult = result as RedirectResult;
            Assert.AreEqual("/home/index", redirectResult.Url);
        }

        [Test]
        public async Task TryLogin_WithInvalidCredentials_ReturnsViewWithMessage()
        {
            // Arrange
            var loginModel = new AccountModel { Login = "testuser@example.com", Password = "WrongPassword" };

            _db.Users.Add(new User
            {
                Email = "testuser@example.com",
                Password = AccountController.Sha256Hash("ValidPassword"),
                Role = "user"
            });
            _db.SaveChanges();

            // Act
            var result = await _controller.TryLogin(loginModel, null);

            // Assert
            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = result as ViewResult;
            Assert.IsNotNull(viewResult.Model);
            var model = viewResult.Model as AccountModel;
            Assert.AreEqual("Incorrect email or password", model.Message);
        }

        [Test]
        public void Register_WhenUserAlreadyAuthenticated_RedirectsToHomePage()
        {
            // Arrange
            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper
                .Setup(m => m.Action(It.IsAny<UrlActionContext>()))
                .Returns("/home/index");

            _controller.Url = mockUrlHelper.Object;

            // Act
            var result = _controller.Register(null);

            // Assert
            Assert.IsInstanceOf<RedirectResult>(result);
            var redirectResult = result as RedirectResult;
            Assert.AreEqual("/home/index", redirectResult.Url);
        }

        [Test]
        public async Task TryRegister_WithInvalidEmail_ReturnsViewWithErrorMessage()
        {
            // Arrange
            var registerModel = new AccountModel
            {
                User = new UserData { Email = "invalidemail" },
                Password = "ValidPassword",
                Password2 = "ValidPassword"
            };

            // Act
            var result = await _controller.TryRegister(registerModel);

            // Assert
            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = result as ViewResult;
            Assert.IsNotNull(viewResult.Model);
            var model = viewResult.Model as AccountModel;
            Assert.AreEqual("Invalid data", model.Message);
        }

        [Test]
        public async Task TryRegister_WithValidData_RedirectsToHomePage()
        {
            // Arrange
            var registerModel = new AccountModel
            {
                User = new UserData
                {
                    Email = "newuser@example.com",
                    Name = "John",
                    Surname = "Doe"
                },
                Password = "12345678",
                Password2 = "12345678"
            };

            // Mock services
            var authenticationServiceMock = new Mock<IAuthenticationService>();
            var serviceProviderMock = new Mock<IServiceProvider>();

            serviceProviderMock
                .Setup(sp => sp.GetService(typeof(IAuthenticationService)))
                .Returns(authenticationServiceMock.Object);

            // Mock HttpContext with IServiceProvider
            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProviderMock.Object
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, registerModel.User.Email),
                new Claim(ClaimTypes.Role, "user")
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            httpContext.User = claimsPrincipal;

            // Setup controller with mocked HttpContext
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Mock ActionContext with ActionDescriptor
            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ControllerActionDescriptor()
            );

            // Mock IUrlHelper
            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper
                .Setup(m => m.Action(It.IsAny<UrlActionContext>()))
                .Returns("/home/index");

            _controller.Url = mockUrlHelper.Object;

            // Act
            var result = await _controller.TryRegister(registerModel);

            // Assert
            Assert.IsInstanceOf<RedirectResult>(result);
            var redirectResult = result as RedirectResult;
            Assert.AreEqual("/home/index", redirectResult.Url);
        }
    }
}
