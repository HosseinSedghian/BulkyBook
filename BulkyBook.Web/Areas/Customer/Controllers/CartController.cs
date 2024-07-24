using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models.ViewModels;
using System.Security.Claims;
using BulkyBook.Models;
using BulkyBook.Utility;
using Stripe.Checkout;
using Microsoft.Extensions.Options;

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
					filter: x => x.ApplicationUserId == claims.Value,
					includeProperties: $"{nameof(Product)}"),
				OrderHeader = new OrderHeader()
			};
			foreach (ShoppingCart cart in shoppingCartVM.ListCart)
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
			shoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
			shoppingCartVM.OrderHeader.ApplicationUserId = claims.Value;

			foreach (ShoppingCart cart in shoppingCartVM.ListCart)
			{
				cart.Price = GetPriceBasedQuantity(cart.Count, cart.Product);
				shoppingCartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
			}
			ApplicationUser appUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(x => x.Id == claims.Value);
			if (appUser.CompanyId.GetValueOrDefault() == 0)
			{
				shoppingCartVM.OrderHeader.OrderStatus = SD.OrderStatusPending;
				shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
			}
			else
			{
				shoppingCartVM.OrderHeader.OrderStatus = SD.OrderStatusApproved;
				shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
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
			if (appUser.CompanyId.GetValueOrDefault() == 0)
			{
				// Start Stripe
				var domain = "https://localhost:7290/";
				var options = new SessionCreateOptions
				{
					LineItems = new List<SessionLineItemOptions>(),
					Mode = "payment",
					SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={shoppingCartVM.OrderHeader.Id}",
					CancelUrl = domain + "customer/cart/Index"
				};
				foreach (var item in shoppingCartVM.ListCart)
				{
					var sessionLineItem = new SessionLineItemOptions
					{
						PriceData = new SessionLineItemPriceDataOptions
						{
							UnitAmount = (long)(item.Price * 100),
							Currency = "usd",
							ProductData = new SessionLineItemPriceDataProductDataOptions
							{
								Name = item.Product.Title,
							},
						},
						Quantity = item.Count,
					};
					options.LineItems.Add(sessionLineItem);
				}
				var service = new SessionService();
				Session session = service.Create(options);
				_unitOfWork.OrderHeader.UpdateStripePaymentId(
					shoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
				_unitOfWork.Save();
				Response.Headers.Add("Location", session.Url);
				return new StatusCodeResult(303);
				// End Stripe
			}
			else
			{
				return RedirectToAction(
					actionName:nameof(OrderConfirmation), 
					routeValues:new { id=shoppingCartVM.OrderHeader.Id});
			}
		}

		public IActionResult OrderConfirmation(int id)
		{
			OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == id);
			if(orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
			{
				var service = new SessionService();
				Session session = service.Get(orderHeader.sessionId);
				if (session.PaymentStatus.ToLower() == "paid")
				{
					_unitOfWork.OrderHeader.UpdateStripePaymentId(orderHeader.Id, orderHeader.sessionId, session.PaymentIntentId);
					_unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.OrderStatusApproved, SD.PaymentStatusApproved);
					_unitOfWork.Save();
				}
			}
			List<ShoppingCart> shopCarts = _unitOfWork.ShoppingCart.GetAll(
				x => x.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
			_unitOfWork.ShoppingCart.RemoveRange(shopCarts);
			_unitOfWork.Save();
			return View(id);
		}

		public IActionResult Plus(int cartId)
		{
			ShoppingCart cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(filter: x => x.Id == cartId);
			_unitOfWork.ShoppingCart.IncrementCount(cart, 1);
			_unitOfWork.ShoppingCart.Update(cart);
			_unitOfWork.Save();
			return RedirectToAction(nameof(Index));
		}
		public IActionResult Minus(int cartId)
		{
			ShoppingCart cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(filter: x => x.Id == cartId);
			if (cart.Count <= 1)
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
				<= 50 => product.Price,
				> 50 and <= 100 => product.Price50,
				> 100 => product.Price100
			};
	}
}
