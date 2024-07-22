using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
namespace BulkyBook.DataAccess.Repository
{
    internal class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly AppDbContext _context;
        public ProductRepository(AppDbContext context)
            : base(context)
        {
            _context = context;
        }
        public void Update(Product input)
        {
            var objfromDb = _context.Products.FirstOrDefault(prod => prod.Id == input.Id);
            if (objfromDb != null)
            {
                objfromDb.Title = input.Title;
                objfromDb.ISBN = input.ISBN;
                objfromDb.Price = input.Price;
                objfromDb.Price50 = input.Price50;
                objfromDb.ListPrice = input.ListPrice;
                objfromDb.Price100 = input.Price100;
                objfromDb.Description = input.Description;
                objfromDb.Author = input.Author;
                objfromDb.CategoryId = input.CategoryId;
                objfromDb.CoverTypeId = input.CoverTypeId;
                if(input.ImageUrl != null)
                {
                    objfromDb.ImageUrl = input.ImageUrl;
                }
            }
        }
    }
}
