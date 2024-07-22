using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using System;

namespace BulkyBook.DataAccess.Repository
{
    public class CoverTypeRepository : Repository<CoverType>, ICoverTypeRepository
    {
        private readonly AppDbContext _Context;
        public CoverTypeRepository(AppDbContext context)
            : base(context)
        {
            _Context = context;
        }
        public void Update(CoverType coverType)
        {
            _Context.CoverTypes.Update(coverType);
        }
    }
}
