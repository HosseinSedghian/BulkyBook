using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBook.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _UnitOfWork;
        public CompanyController(IUnitOfWork unitOfWork)
        {
            _UnitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }
        // Get
        public IActionResult Upsert(int? id)
        {
            Company company = null;
            if (id == null || id == 0)
            {
                // Create
                company = new();
            }
            else
            {
                // Update
                company = _UnitOfWork.Company.GetFirstOrDefault(x => x.Id == id);
            }
            return View(company);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Company input)
        {
            if (ModelState.IsValid)
            {
                if (input.Id == 0)
                {
                    _UnitOfWork.Company.Add(input);
                    TempData["success"] = "Company created successfully.";
                }
                else
                {
                    _UnitOfWork.Company.Update(input);
                    TempData["success"] = "Company updated successfully.";
                }
                _UnitOfWork.Save();
                return RedirectToAction("index");
            }
            return View(input);
        }

        #region API Calls
        [HttpGet]
        public IActionResult GetAll()
        {
            var companylist = _UnitOfWork.Company.GetAll();
            return Json(new { data = companylist });
        }
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var company = _UnitOfWork.Company.GetFirstOrDefault(cat => cat.Id == id);
            if (company == null)
            {
                return Json(new { success = false, message = "Error while deleting." });
            }
            _UnitOfWork.Company.Remove(company);
            _UnitOfWork.Save();
            return Json(new { success = true, message = "Delete successful." });
        }
        #endregion
    }
}
