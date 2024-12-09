using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopWebApp
{
    public class AccountDTO : BaseViewDTO
    {
        public AccountDTO(string title) : base(title) { Message = ""; }
        public AccountDTO() : base("") { Message = ""; }

        public string Login { get; set; }
        public string Password { get; set; }
        public string Password2 { get; set; }
        public string OldPassword { get; set; }
        public string Message { get; set; }
        public bool RememberMe { get; set; }
    }
}
