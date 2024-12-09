using BuisnessLogic.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using ShopWebApp;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


namespace BuisnessLogic.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(ApplicationContext dbContext, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> RegisterAsync(AccountDTO input)
        {
            if (IsEmailFree(input.User.Email))
            {
                if (input.Password != input.Password2)
                {
                    input.Message = "Passwords do not match";
                    return false;
                }

                if (input.User.Email == null || !input.User.Email.Contains('@') || input.User.Email.Length > 128 ||
                    input.User.Name == null || input.User.Name.Length > 64 || input.User.Surname == null || input.User.Surname.Length > 64 ||
                    input.Password == null || input.Password.Length > 128)
                {
                    input.Message = "Invalid data";
                    return false;
                }

                var user = new User
                {
                    Email = input.User.Email,
                    Name = input.User.Name,
                    Surname = input.User.Surname,
                    Password = Sha256Hash(input.Password),
                    Role = "user",
                    Address = input.User.Address ?? "",
                    Phone = input.User.Phone ?? ""
                };

                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync();

                // Автоматический вход после регистрации
                var claims = new[]
                {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
            };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Email, ClaimTypes.Role);
                await _httpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                return true;
            }

            input.Message = $"Email {input.User.Email} is already taken";
            return false;
        }

        public async Task<bool> LoginAsync(AccountDTO input, bool rememberMe, string returnUrl)
        {
            bool status = AreCredentialsValid(input.Login, input.Password);
            if (!status)
            {
                input.Message = "Incorrect email or password";
                return false;
            }

            var user = _dbContext.Users.FirstOrDefault(c => c.Email == input.Login);

            var claims = new[]
            {
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
        };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Email, ClaimTypes.Role);

            if (rememberMe)
            {
                await _httpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddMonths(1)
                });
            }
            else
            {
                await _httpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            }

            return true;
        }

        public async Task LogoutAsync()
        {
            await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public bool AreCredentialsValid(string login, string password)
        {
            var user = _dbContext.Users.FirstOrDefault(c => c.Email == login);
            return user != null && user.Password == Sha256Hash(password);
        }

        public bool IsEmailFree(string email)
        {
            return !_dbContext.Users.Any(c => c.Email == email);
        }

        public string Sha256Hash(string password)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(password);
            byte[] hashed;
            string hashedString = String.Empty;

            using (SHA256 hasher = SHA256.Create())
            {
                hashed = hasher.ComputeHash(bytes);
            }

            foreach (byte b in hashed)
            {
                hashedString += b.ToString("X");
            }

            return hashedString;
        }
    }
}
