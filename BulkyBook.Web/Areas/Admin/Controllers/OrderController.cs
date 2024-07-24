using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.AspNetCore.Mvc;
using BulkyBook.Utility;

namespace BulkyBook.Web.Areas.Admin.Controllers
{
	[Area("Admin")]
	public class OrderController : Controller
	{
		private readonly IUnitOfWork _UnitOfWork;
        public OrderController(IUnitOfWork unitOfWork)
        {
			_UnitOfWork = unitOfWork;
        }
        public IActionResult Index()
		{
			return View();
		}

		#region API Calls
		[HttpGet]
		public IActionResult GetAll(string status)
		{
            IEnumerable<OrderHeader> orderHeaders;

            switch (status)
            {
                case "pending":
                    orderHeaders = _UnitOfWork.OrderHeader.GetAll(
                    includeProperties: $"{nameof(ApplicationUser)}",
                    filter: x => x.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    orderHeaders = _UnitOfWork.OrderHeader.GetAll(
                    includeProperties: $"{nameof(ApplicationUser)}",
                    filter: x => x.OrderStatus == SD.OrderStatusInProcess);
                    break;
                case "completed":
                    orderHeaders = _UnitOfWork.OrderHeader.GetAll(
                    includeProperties: $"{nameof(ApplicationUser)}",
                    filter: x => x.OrderStatus == SD.OrderStatusShipped);
                    break;
                case "approved":
                    orderHeaders = _UnitOfWork.OrderHeader.GetAll(
                    includeProperties: $"{nameof(ApplicationUser)}",
                    filter: x => x.OrderStatus == SD.OrderStatusApproved);
                    break;
                default:
                    orderHeaders = _UnitOfWork.OrderHeader.GetAll(
                    includeProperties: $"{nameof(ApplicationUser)}");
                    break;
            }
            return Json(new { data = orderHeaders });
		}
		#endregion
	}
}
