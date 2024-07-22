using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models.ViewModels;
using System.Security.Claims;
using BulkyBook.Models;

namespace BulkyBook.Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        public readonly IUnitOfWork _unitOfWork;
        public ShoppingCartVM _shoppingCartVM { get; set; }
		public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            _shoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _unitOfWork.ShoppingCart.GetAll(
                    filter:x => x.ApplicationUserId == claims.Value,
                    includeProperties:$"{nameof(Product)}")
            };
            foreach(ShoppingCart cart in _shoppingCartVM.ListCart)
            {
                cart.Price = GetPriceBasedQuantity(cart.Count, cart.Product);
                _shoppingCartVM.CartTotal += cart.Price * cart.Count;
			}
			return View(_shoppingCartVM);
        }
		public IActionResult Summary()
		{
			//var claimsIdentity = (ClaimsIdentity)User.Identity;
			//var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
			//_shoppingCartVM = new ShoppingCartVM()
			//{
			//	ListCart = _unitOfWork.ShoppingCart.GetAll(
			//		filter: x => x.ApplicationUserId == claims.Value,
			//		includeProperties: $"{nameof(Product)}")
			//};
			//foreach (ShoppingCart cart in _shoppingCartVM.ListCart)
			//{
			//	cart.Price = GetPriceBasedQuantity(cart.Count, cart.Product);
			//	_shoppingCartVM.CartTotal += cart.Price * cart.Count;
			//}
			//return View(_shoppingCartVM);
            return View();
		}
		public IActionResult Plus(int cartId)
        {
            ShoppingCart cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(filter:x => x.Id == cartId);
            _unitOfWork.ShoppingCart.IncrementCount(cart, 1);
            _unitOfWork.ShoppingCart.Update(cart);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }
        public IActionResult Minus(int cartId)
        {
			ShoppingCart cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(filter: x => x.Id == cartId);
            if(cart.Count <= 1)
            {
				_unitOfWork.ShoppingCart.Remove(cart);
			}
            else
            {
				_unitOfWork.ShoppingCart.DecrementCount(cart, 1);
				_unitOfWork.ShoppingCart.Update(cart);
			}
			_unitOfWork.Save();
			return RedirectToAction(nameof(Index));
		}
		public IActionResult Remove(int cartId)
		{
			ShoppingCart cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(filter: x => x.Id == cartId);
            _unitOfWork.ShoppingCart.Remove(cart);
			_unitOfWork.Save();
			return RedirectToAction(nameof(Index));
		}
		private double GetPriceBasedQuantity(int quantity, Product product)
            => quantity switch
            {
                <=50 => product.Price,
                >50 and <=100 => product.Price50,
                >100 => product.Price100
            };
    }
}
