using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository
{
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
	{
        private readonly AppDbContext _context;
        public OrderHeaderRepository(AppDbContext context)
            : base(context)
        {
            _context = context;
        }

        public void Update(OrderHeader orderHeader)
        {
            _context.OrderHeaders.Update(orderHeader);
        }

		public void UpdateStatus(int id, string orderStatus, string? paymentStatus = null)
		{
            var order = _context.OrderHeaders.FirstOrDefault(x => x.Id == id);
            if (order != null)
            {
                order.OrderStatus = orderStatus;
                if (paymentStatus != null)
                {
                    order.PaymentStatus = paymentStatus;
                }
            }
		}
		public void UpdateStripePaymentId(int id, string sessionId, string paymentIntentId)
		{
			var order = _context.OrderHeaders.FirstOrDefault(x => x.Id == id);
			order.sessionId = sessionId;
            order.PaymentIntentId = paymentIntentId;
            order.PaymentDate = DateTime.Now;
		}
	}
}
