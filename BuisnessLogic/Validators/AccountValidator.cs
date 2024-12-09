using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BuisnessLogic.Validators
{
    public static class AccountValidator
    {
        public static bool ValidateName(string name) =>
            !string.IsNullOrEmpty(name) && name.Length >= 3 && name.Length <= 128;

        public static bool ValidateSurname(string surname) =>
            !string.IsNullOrEmpty(surname) && surname.Length >= 3 && surname.Length <= 128;

        public static bool ValidateEmail(string email) =>
            !string.IsNullOrEmpty(email) && email.Contains("@") && email.Length <= 128;

        public static bool ValidatePhone(string phone) =>
            string.IsNullOrEmpty(phone) || (phone.Length >= 9 && phone.Length <= 16 && Regex.IsMatch(phone, @"^\+?[0-9]*$"));

        public static bool ValidatePassword(string password) =>
            !string.IsNullOrEmpty(password) && password.Length >= 8 && password.Length <= 128;

        public static bool ValidateAddress(string address) =>
            string.IsNullOrEmpty(address) || address.Length <= 256;

        public static bool ValidatePasswordsMatch(string password, string password2) =>
            password == password2;
    }
}
