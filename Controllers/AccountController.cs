using BuisnessLogic.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShopWebApp;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ShopWebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly IAuthService _authService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Inject the services
        public AccountController(IAccountService accountService, IAuthService authService, IHttpContextAccessor httpContextAccessor)
        {
            _accountService = accountService;
            _authService = authService;
            _httpContextAccessor = httpContextAccessor;
        }

        [Authorize]
        public IActionResult Index()
        {
            var model = new AccountDTO("My Account");
            if (User.Identity.IsAuthenticated)
            {
                var user = _accountService.FindUserByEmail(User.Identity.Name);
                if (user != null)
                {
                    model.User.Name = user.Name;
                    model.User.Surname = user.Surname;
                    model.User.Email = user.Email;
                    model.User.Address = user.Address;
                    model.User.Phone = user.Phone;
                    model.User.Role = user.Role;

                    var orders = _accountService.GetUserOrders(user.UserId);
                    ViewBag.orders = orders;
                }
            }

            return View(model);
        }

        [HttpGet]
        [Authorize]
        public IActionResult Edit(AccountDTO input)
        {
            if (input == null)
                input = new AccountDTO();
            input.Title = "Edit Account";

            if (User.Identity.IsAuthenticated)
            {
                var user = _accountService.FindUserByEmail(User.Identity.Name);
                if (user != null)
                {
                    input.User.Name = user.Name;
                    input.User.Surname = user.Surname;
                    input.User.Email = user.Email;
                    input.User.Address = user.Address;
                    input.User.Phone = user.Phone;
                }
            }

            return View(input);
        }

        [HttpPost]
        [Authorize]
        [ActionName("Edit")]
        public async Task<IActionResult> TryEdit(AccountDTO input)
        {
            var currentUser = _accountService.FindUserByEmail(User.Identity.Name);
            if (currentUser == null)
                return RedirectToAction("Login");

            bool needRelog = false;
            bool changedPass = false;
            bool changedEmail = false;

            // Validate and update user properties
            if (!string.IsNullOrEmpty(input.User.Name) && input.User.Name.Length >= 3 && input.User.Name.Length <= 128)
                currentUser.Name = input.User.Name;
            if (!string.IsNullOrEmpty(input.User.Surname) && input.User.Surname.Length >= 3 && input.User.Surname.Length <= 128)
                currentUser.Surname = input.User.Surname;
            if (!string.IsNullOrEmpty(input.User.Phone) && input.User.Phone.Length >= 9 && input.User.Phone.Length <= 16)
                currentUser.Phone = input.User.Phone;
            if (!string.IsNullOrEmpty(input.User.Address) && input.User.Address.Length <= 256)
                currentUser.Address = input.User.Address;

            // Email change validation
            if (!string.IsNullOrEmpty(input.User.Email) && input.User.Email.Contains('@') && input.User.Email.Length < 128 && _accountService.IsEmailFree(input.User.Email))
            {
                currentUser.Email = input.User.Email;
                needRelog = true;
                changedEmail = true;
            }

            // Password change validation
            if (!string.IsNullOrEmpty(input.Password) && input.Password == input.Password2 && input.Password.Length >= 8 && input.Password.Length <= 128)
            {
                if (_accountService.ValidateCredentials(currentUser.Email, input.OldPassword))
                {
                    currentUser.Password = _accountService.HashPassword(input.Password);
                    needRelog = true;
                    changedPass = true;
                }
            }

            _accountService.UpdateUser(currentUser);

            if (needRelog)
            {
                await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                var claims = new[]
                {
                    new Claim(ClaimTypes.Email, currentUser.Email),
                    new Claim(ClaimTypes.Role, currentUser.Role),
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Email, ClaimTypes.Role);
                await _httpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                input.Title = "Edit Account";
                input.Message = changedEmail ? "Successfully changed email" : "Successfully changed password";
                return View(input);
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Login(AccountDTO input)
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            input.Title = "Log in";
            return View(input);
        }

        [HttpPost]
        [ActionName("Login")]
        public async Task<IActionResult> TryLogin(AccountDTO input, [FromQuery] string ReturnUrl)
        {
            if (await _authService.LoginAsync(input, input.RememberMe, ReturnUrl))
            {
                if (ReturnUrl == null)
                    return RedirectToAction("Index", "Home");

                return Redirect(ReturnUrl);
            }

            input.Message = "Incorrect email or password";
            input.Title = "Log in";
            return View(input);
        }

        [HttpGet]
        public IActionResult Register(AccountDTO input)
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            input.Title = "Register";
            return View(input);
        }

        [HttpPost]
        [ActionName("Register")]
        public async Task<IActionResult> TryRegister(AccountDTO input)
        {
            if (await _authService.RegisterAsync(input))
            {
                return RedirectToAction("Index", "Home");
            }

            input.Title = "Register";
            return View(input);
        }

        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
