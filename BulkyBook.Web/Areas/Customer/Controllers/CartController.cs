using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models.ViewModels;
using System.Security.Claims;
using BulkyBook.Models;
using BulkyBook.Utility;

namespace BulkyBook.Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        public readonly IUnitOfWork _unitOfWork;
		public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var shoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _unitOfWork.ShoppingCart.GetAll(
                    filter:x => x.ApplicationUserId == claims.Value,
                    includeProperties:$"{nameof(Product)}"),
                OrderHeader = new OrderHeader()
            };
            foreach(ShoppingCart cart in shoppingCartVM.ListCart)
            {
                cart.Price = GetPriceBasedQuantity(cart.Count, cart.Product);
				shoppingCartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
			}
			return View(shoppingCartVM);
        }
		public IActionResult Summary()
		{
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var shoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _unitOfWork.ShoppingCart.GetAll(
                    filter: x => x.ApplicationUserId == claims.Value,
                    includeProperties: $"{nameof(Product)}"),
                OrderHeader = new OrderHeader()
            };
			shoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(
                x => x.Id == claims.Value);
			shoppingCartVM.OrderHeader.Name = shoppingCartVM.OrderHeader.ApplicationUser.Name;
			shoppingCartVM.OrderHeader.PhoneNumber = shoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
			shoppingCartVM.OrderHeader.StreetAddress = shoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
			shoppingCartVM.OrderHeader.City = shoppingCartVM.OrderHeader.ApplicationUser.City;
			shoppingCartVM.OrderHeader.State = shoppingCartVM.OrderHeader.ApplicationUser.State;
			shoppingCartVM.OrderHeader.PostalCode = shoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

			foreach (ShoppingCart cart in shoppingCartVM.ListCart)
            {
                cart.Price = GetPriceBasedQuantity(cart.Count, cart.Product);
				shoppingCartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
            }
            return View(shoppingCartVM);
		}

        [HttpPost]
        [ValidateAntiForgeryToken]
		public IActionResult Summary(ShoppingCartVM shoppingCartVM)
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

			shoppingCartVM.ListCart = _unitOfWork.ShoppingCart.GetAll(
                    filter: x => x.ApplicationUserId == claims.Value,
                    includeProperties: $"{nameof(Product)}");
            shoppingCartVM.OrderHeader.OrderStatus = SD.OrderStatusPending;
            shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            shoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
            shoppingCartVM.OrderHeader.ApplicationUserId = claims.Value;

			foreach (ShoppingCart cart in shoppingCartVM.ListCart)
			{
				cart.Price = GetPriceBasedQuantity(cart.Count, cart.Product);
				shoppingCartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
			}
            _unitOfWork.OrderHeader.Add(shoppingCartVM.OrderHeader);
            _unitOfWork.Save();

			foreach (ShoppingCart cart in shoppingCartVM.ListCart)
			{
                OrderDetail od = new OrderDetail()
                {
                    OrderId = shoppingCartVM.OrderHeader.Id,
                    ProductId = cart.ProductId,
                    Count = cart.Count,
                    Price = cart.Price
                };
                _unitOfWork.OrderDetail.Add(od);
                _unitOfWork.Save();
			}
            _unitOfWork.ShoppingCart.RemoveRange(shoppingCartVM.ListCart);
            _unitOfWork.Save();
            return RedirectToAction("Index", "Home");
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
