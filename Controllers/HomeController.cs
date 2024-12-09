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
            var model = new BaseViewDTO("Shop");
            using (var db = new ApplicationContext())
            {
                if (User.Identity.IsAuthenticated)
                {
                    var user = (from c in db.Users
                                where c.Email == User.Identity.Name
                                select c).FirstOrDefault();
                    if (user == null) return View(model);
                    model.User.Name = user.Name;
                    model.User.Surname = user.Surname;
                    model.User.Email = user.Email;
                }
                if (db.Products.Where(p => p.Enabled && p.Stock != 0).Count() > 0)
                {
                    int maxId = db.Products.Max(p => p.ProductId), id;
                    Random random = new Random();
                    do
                    {
                        id = random.Next(maxId + 1);
                    }
                    while (db.Products.Where(p => p.ProductId == id && p.Enabled && p.Stock != 0).Count() == 0);
                    ViewBag.product = db.Products.Where(p => p.ProductId == id).FirstOrDefault();
                    ViewBag.productExists = true;
                }
                else
                {
                    ViewBag.productExists = false;
                }
                Dictionary<string, string> Categories = new Dictionary<string, string>();
                foreach (Category category in db.Categories.Where(c => c.Enabled))
                {
                    Categories.Add(category.Name, category.Code);
                }
                ViewBag.categoriesExist = Categories.Count > 0 ? true : false;
                ViewBag.categories = Categories;
            }

            return View(model);
        }

        [Route("/Error/{code:int}")]
        public IActionResult Error(int code)
        {
            var model = new BaseViewDTO("Shop");
            if (User.Identity.IsAuthenticated)
            {
                using (var db = new ApplicationContext())
                {
                    var user = (from c in db.Users
                                where c.Email == User.Identity.Name
                                select c).FirstOrDefault();
                    model.User.Name = user.Name;
                    model.User.Surname = user.Surname;
                    model.User.Email = user.Email;
                }
            }
            ViewData["errorCode"] = code;
            ViewData["aboutError"] = "";
            if (code == 404)
            {
                ViewData["aboutError"] = "Sorry, but the page with the given address does not exist";
            }
            model.Title = "An error occurred";
            return View(model);
        }
    }
}
