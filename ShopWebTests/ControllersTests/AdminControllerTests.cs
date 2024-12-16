using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using ShopWebApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Authentication;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Mvc.Routing;

namespace ShopWebTests.ControllersTests
{
    [TestFixture, NonParallelizable] // Prevent tests from running in parallel
    public class AdminControllerTests
    {
        private ShopDatabase _db;
        private AdminController _controller;

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
            _controller = new AdminController
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                            new Claim(ClaimTypes.Name, "adminuser@example.com"),
                            new Claim(ClaimTypes.Role, "admin")
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
                Email = "adminuser@example.com",
                Role = "admin",
                Enabled = true
            };

            _db.Users.Add(mockUser);
            _db.SaveChanges();

            // Act
            var result = _controller.Index();

            // Assert
            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = result as ViewResult;
            Assert.IsNotNull(viewResult);
            Assert.IsInstanceOf<AdminModel>(viewResult.Model);
        }

        [Test]
        public void Photos_WhenUserIsAuthenticated_ReturnsViewResult()
        {
            // Arrange
            var mockUser = new User
            {
                Email = "adminuser@example.com",
                Role = "admin",
                Enabled = true
            };

            _db.Users.Add(mockUser);
            _db.SaveChanges();

            // Act
            var result = _controller.Photos();

            // Assert
            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = result as ViewResult;
            Assert.IsNotNull(viewResult);
            Assert.IsInstanceOf<BaseViewModel>(viewResult.Model);
        }

        [Test]
        public async Task Upload_WhenUserIsAuthenticatedAndHasPermission_ReturnsRedirectResult()
        {
            // Arrange
            var mockUser = new User
            {
                Email = "adminuser@example.com",
                Role = "admin",
                Enabled = true
            };

            _db.Users.Add(mockUser);
            _db.SaveChanges();

            var formFiles = new List<IFormFile>
    {
        new FormFile(new MemoryStream(new byte[100]), 0, 100, "files", "test.jpg")
    };

            // Mock IUrlHelper to return a proper redirect URL
            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper
                .Setup(helper => helper.Action(It.IsAny<UrlActionContext>()))
                .Returns("/admin/photos");

            _controller.Url = mockUrlHelper.Object;

            // Act
            var result = await _controller.Upload(formFiles);

            // Assert
            Assert.IsInstanceOf<RedirectResult>(result); // Expecting RedirectResult instead of RedirectToActionResult
            var redirectResult = result as RedirectResult;
            Assert.AreEqual("/admin/photos", redirectResult.Url); // Check the URL of the redirect
        }


        [Test]
        public void RemovePhoto_WhenUserIsAuthenticatedAndHasPermission_ReturnsRedirectResult()
        {
            // Arrange
            var mockUser = new User
            {
                Email = "adminuser@example.com",
                Role = "admin",
                Enabled = true
            };

            _db.Users.Add(mockUser);
            _db.SaveChanges();

            // Create a test file in the wwwroot/images directory
            var path = @"wwwroot/images/test.jpg";
            File.WriteAllText(path, "test content");

            // Mock IUrlHelper to return a proper redirect URL
            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper
                .Setup(helper => helper.Action(It.IsAny<UrlActionContext>()))
                .Returns("/admin/photos");

            _controller.Url = mockUrlHelper.Object;

            // Act
            var result = _controller.RemovePhoto("test.jpg");

            // Assert
            Assert.IsInstanceOf<RedirectResult>(result); // Change to RedirectResult
            var redirectResult = result as RedirectResult;
            Assert.AreEqual("/admin/photos", redirectResult.Url); // Check the URL of the redirect

            // Verify the file was deleted
            Assert.IsFalse(File.Exists(path));
        }

        [Test]
        public void RemovePhoto_WhenUserDoesNotHavePermission_ReturnsForbidResult()
        {
            // Arrange
            var mockUser = new User
            {
                Email = "employee@example.com",
                Role = "employee", // User with insufficient permission
                Enabled = true
            };

            _db.Users.Add(mockUser);
            _db.SaveChanges();

            // Ensure the directory and photo file exist for the test to operate on.
            var directoryPath = @"wwwroot/images";
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var photoPath = Path.Combine(directoryPath, "test.jpg");

            // Simulate adding a photo file for the controller to remove.
            File.WriteAllText(photoPath, "test content");

            // Create a mock HttpContext and set the user with "employee" role
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, mockUser.Email),
        new Claim(ClaimTypes.Role, "employee")
    };
            var claimsIdentity = new ClaimsIdentity(claims, "mock");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            var httpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = _controller.RemovePhoto("test.jpg");

            // Assert
            Assert.IsInstanceOf<ForbidResult>(result);

            // Clean up after the test
            if (File.Exists(photoPath))
            {
                File.Delete(photoPath); // Delete the file after test completes
            }
        }

    }
}
