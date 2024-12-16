using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopWebApp
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var model = new BaseViewModel("Shop");
            using (var db = new ShopDatabase())
            {
                if (User.Identity.IsAuthenticated)
                {
                    var user = db.Users.FirstOrDefault(c => c.Email == User.Identity.Name);
                    if (user != null)
                    {
                        model.User.Name = user.Name;
                        model.User.Surname = user.Surname;
                        model.User.Email = user.Email;
                    }
                }

                var products = db.Products.Where(p => p.Enabled && p.Stock > 0).ToList();
                if (products.Any())
                {
                    var random = new Random();
                    var randomProduct = products[random.Next(products.Count)];
                    ViewBag.product = randomProduct;
                    ViewBag.productExists = true;
                }
                else
                {
                    ViewBag.productExists = false;
                }

                var categories = db.Categories.Where(c => c.Enabled)
                    .ToDictionary(c => c.Name, c => c.Code);
                ViewBag.categoriesExist = categories.Any();
                ViewBag.categories = categories;
            }

            return View(model);
        }
        [Route("/Error/{code:int}")]
        public IActionResult Error(int code)
        {
            var model = new ErrorViewModel
            {
                ErrorCode = code,
                AboutError = code == 404
                    ? "Sorry, but the page with the given address does not exist"
                    : "An unexpected error occurred."
            };

            if (User.Identity.IsAuthenticated)
            {
                using (var db = new ShopDatabase())
                {
                    var user = db.Users.FirstOrDefault(c => c.Email == User.Identity.Name);
                    if (user != null)
                    {
                        model.User.Name = user.Name;
                        model.User.Surname = user.Surname;
                        model.User.Email = user.Email;
                    }
                }
            }

            return View(model);
        }

        //[Route("/Error/{code:int}")]
        //public IActionResult Error(int code)
        //{
        //    var model = new BaseViewModel("Shop");
        //    if (User.Identity.IsAuthenticated)
        //    {
        //        using (var db = new ShopDatabase())
        //        {
        //            var user = (from c in db.Users
        //                        where c.Email == User.Identity.Name
        //                        select c).FirstOrDefault();
        //            model.User.Name = user.Name;
        //            model.User.Surname = user.Surname;
        //            model.User.Email = user.Email;
        //        }
        //    }
        //    ViewData["errorCode"] = code;
        //    ViewData["aboutError"] = "";
        //    if (code == 404)
        //    {
        //        ViewData["aboutError"] = "Sorry, but the page with the given address does not exist";
        //    }
        //    model.Title = "An error occurred";
        //    return View(model);
        //}
    }
}
