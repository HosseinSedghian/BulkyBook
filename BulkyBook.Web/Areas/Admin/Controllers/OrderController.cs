using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.AspNetCore.Mvc;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using System.Linq.Expressions;
using System.Security.Claims;
using BulkyBook.Models.ViewModels;
using Stripe;
using Stripe.Checkout;

namespace BulkyBook.Web.Areas.Admin.Controllers
{
	[Area("Admin")]
    [Authorize]
	public class OrderController : Controller
	{
		private readonly IUnitOfWork _UnitOfWork;
        [BindProperty]
        public OrderVM OrderVM { get; set; }
        public OrderController(IUnitOfWork unitOfWork)
        {
			_UnitOfWork = unitOfWork;
        }
        public IActionResult Index()
		{
			return View();
		}
        public IActionResult Details(int orderId)
        {
            OrderVM = new OrderVM()
            {
                OrderHeader = _UnitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == orderId,
                                includeProperties: nameof(ApplicationUser)),
                OrderDetails = _UnitOfWork.OrderDetail.GetAll(x => x.OrderId == orderId,
                                includeProperties: nameof(Models.Product))
            };
            return View(OrderVM);
        }
        [ActionName("Details")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Details_PayNow()
        {
            OrderVM.OrderHeader = _UnitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == OrderVM.OrderHeader.Id,
                                includeProperties: nameof(ApplicationUser));
            OrderVM.OrderDetails = _UnitOfWork.OrderDetail.GetAll(x => x.OrderId == OrderVM.OrderHeader.Id,
                                includeProperties: nameof(Models.Product));

            // Start Stripe
            var domain = "https://localhost:7290/";
            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderid={OrderVM.OrderHeader.Id}",
                CancelUrl = domain + $"admin/order/Details?orderId={OrderVM.OrderHeader.Id}"
            };
            foreach (var item in OrderVM.OrderDetails)
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
            _UnitOfWork.OrderHeader.UpdateStripePaymentId(
                OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _UnitOfWork.Save();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
            // End Stripe
        }
        public IActionResult PaymentConfirmation(int orderHeaderid)
        {
            OrderHeader orderHeader = _UnitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == orderHeaderid);
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                var service = new SessionService();
                Session session = service.Get(orderHeader.sessionId);
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _UnitOfWork.OrderHeader.UpdateStripePaymentId(orderHeader.Id, orderHeader.sessionId, session.PaymentIntentId);
                    _UnitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.OrderStatusApproved, SD.PaymentStatusApproved);
                    _UnitOfWork.Save();
                }
            }
            return View(orderHeaderid);
        }



        [HttpPost]
        [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateOrderDetail()
        {
            var orderHeaderFromDb = _UnitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id);
            orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = OrderVM.OrderHeader.City;
            orderHeaderFromDb.State = OrderVM.OrderHeader.State;
            orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;
            if(OrderVM.OrderHeader.Carrier != null)
            {
                orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            }
            if (OrderVM.OrderHeader.TrackingNumber != null)
            {
                orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            }
            _UnitOfWork.OrderHeader.Update(orderHeaderFromDb);
            _UnitOfWork.Save();
            TempData["Success"] = "Order Details Updated Successfully.";
            return RedirectToAction(nameof(Details), "Order", new { orderId = orderHeaderFromDb.Id});
        }
        [HttpPost]
        [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
        [ValidateAntiForgeryToken]
        public IActionResult StartProcessing()
        {
            _UnitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.OrderStatusInProcess);
            _UnitOfWork.Save();
            TempData["Success"] = "Order Status Updated Successfully.";
            return RedirectToAction(nameof(Details), "Order", new { orderId = OrderVM.OrderHeader.Id });
        }
        [HttpPost]
        [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
        [ValidateAntiForgeryToken]
        public IActionResult ShipOrder()
        {
            var orderHeaderFromDb = _UnitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id);
            orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            orderHeaderFromDb.OrderStatus = SD.OrderStatusShipped;
            orderHeaderFromDb.ShippingDate = DateTime.Now;
            if (orderHeaderFromDb.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeaderFromDb.PaymentDueDate = DateTime.Now.AddDays(30);
            }
            _UnitOfWork.OrderHeader.Update(orderHeaderFromDb);
            _UnitOfWork.Save();
            TempData["Success"] = "Order Shipped Successfully.";
            return RedirectToAction(nameof(Details), "Order", new { orderId = orderHeaderFromDb.Id });
        }
        [HttpPost]
        [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
        [ValidateAntiForgeryToken]
        public IActionResult CancelOrder()
        {
            var orderHeaderFromDb = _UnitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id);
            if (orderHeaderFromDb.PaymentStatus == SD.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeaderFromDb.PaymentIntentId
                };
                var service = new RefundService();
                Refund refund = service.Create(options);
                _UnitOfWork.OrderHeader.UpdateStatus(orderHeaderFromDb.Id, SD.OrderStatusCancelled,
                    SD.OrderStatusRefunded);
            }
            else
            {
                _UnitOfWork.OrderHeader.UpdateStatus(orderHeaderFromDb.Id, SD.OrderStatusCancelled,
                    SD.OrderStatusCancelled);
            }
            _UnitOfWork.OrderHeader.Update(orderHeaderFromDb);
            _UnitOfWork.Save();
            TempData["Success"] = "Order Cancelled Successfully.";
            return RedirectToAction(nameof(Details), "Order", new { orderId = orderHeaderFromDb.Id });
        }
        #region API Calls
        [HttpGet]
		public IActionResult GetAll(string status)
		{
            Expression<Func<OrderHeader, bool>> conditionalFilter;
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            switch (status)
            {
                case "pending":
                    if(User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
                    {
                        conditionalFilter = x => x.PaymentStatus == SD.PaymentStatusDelayedPayment;
                    }
                    else
                    {
                        conditionalFilter = 
                            x => x.PaymentStatus == SD.PaymentStatusDelayedPayment &&
                            x.ApplicationUserId == claim.Value;
                    }
                    break;
                case "inprocess":
                    if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
                    {
                        conditionalFilter = x => x.OrderStatus == SD.OrderStatusInProcess;
                    }
                    else
                    {
                        conditionalFilter = 
                            x => x.OrderStatus == SD.OrderStatusInProcess &&
                            x.ApplicationUserId == claim.Value;
                    }
                    break;
                case "completed":
                    if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
                    {
                        conditionalFilter = x => x.OrderStatus == SD.OrderStatusShipped;
                    }
                    else
                    {
                        conditionalFilter = x => x.OrderStatus == SD.OrderStatusShipped &&
                        x.ApplicationUserId == claim.Value;
                    }
                    break;
                case "approved":
                    if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
                    {
                        conditionalFilter = x => x.OrderStatus == SD.OrderStatusApproved;
                    }
                    else
                    {
                        conditionalFilter = x => x.OrderStatus == SD.OrderStatusApproved &&
                        x.ApplicationUserId == claim.Value;
                    }
                    break;
                default:
                    if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
                    {
                        conditionalFilter = x => true;
                    }
                    else
                    {
                        conditionalFilter = x => x.ApplicationUserId == claim.Value;
                    }
                    break;
            }
            IEnumerable<OrderHeader> orderHeaders = _UnitOfWork.OrderHeader.GetAll(
                    includeProperties: $"{nameof(ApplicationUser)}",
                    filter: conditionalFilter);
            return Json(new { data = orderHeaders });
		}
		#endregion
	}
}
