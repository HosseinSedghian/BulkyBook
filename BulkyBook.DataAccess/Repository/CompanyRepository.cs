using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository
{
    public class CompanyRepository : Repository<Company>, ICompanyRepository
    {
        private readonly AppDbContext _Context;
        public CompanyRepository(AppDbContext context)
            : base(context)
        {
            _Context = context;
        }
        public void Update(Company company)
        {
            _Context.Companies.Update(company);
        }
    }
}
