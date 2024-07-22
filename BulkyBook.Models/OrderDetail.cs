using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BulkyBook.Models
{
	public class OrderDetail
	{
		[Key]
		public int Id { get; set; }
        [Required]
        public int OrderId { get; set; }
		[ForeignKey(nameof(OrderId))]
		[ValidateNever]
		public OrderHeader OrderHeader { get; set; }
		[Required]
		public int ProductId { get; set; }
		[ForeignKey(nameof(ProductId))]
		[ValidateNever]
		public Product Product { get; set; }
        public int Count { get; set; }
		public double Price { get; set; }

    }
}
