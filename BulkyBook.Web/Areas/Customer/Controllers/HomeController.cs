using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BulkyBook.Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> products = _unitOfWork.Product.GetAll(
                includeProperties:$"{nameof(Product.Category)},{nameof(Product.CoverType)}");
            return View(products);
        }
        public IActionResult Details(int productId)
        {
            ShoppingCart cartObject = new()
            {
                Product = _unitOfWork.Product.GetFirstOrDefault(
                x => x.Id == productId,
                includeProperties: $"{nameof(Product.Category)},{nameof(Product.CoverType)}"),
                Count = 1,
                ProductId = productId
            };
            return View(cartObject);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            shoppingCart.ApplicationUserId = claims.Value;

            ShoppingCart cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(
                x => x.ProductId == shoppingCart.ProductId &&
                x.ApplicationUserId == shoppingCart.ApplicationUserId);
            if (cart != null)
            {
                _unitOfWork.ShoppingCart.IncrementCount(cart, shoppingCart.Count);
                _unitOfWork.ShoppingCart.Update(cart);
            }
            else
            {
                _unitOfWork.ShoppingCart.Add(shoppingCart);
            }
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
