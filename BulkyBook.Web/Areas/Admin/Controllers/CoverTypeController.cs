using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBook.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CoverTypeController : Controller
    {
        private readonly IUnitOfWork _UnitOfWork;
        public CoverTypeController(IUnitOfWork unitOfWork)
        {
            _UnitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            IEnumerable<CoverType> coverTypes = _UnitOfWork.CoverType.GetAll();
            return View(coverTypes);
        }
        // Get
        public IActionResult Create()
        {
            return View();
        }
        // Post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CoverType input)
        {
            if (ModelState.IsValid)
            {
                _UnitOfWork.CoverType.Add(input);
                _UnitOfWork.Save();
                TempData["success"] = "CoverType created successfully.";
                return RedirectToAction("index");
            }
            return View(input);
        }
        // Get
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            var coverType = _UnitOfWork.CoverType.GetFirstOrDefault(cat => cat.Id == id);
            if (coverType == null)
            {
                return NotFound();
            }
            return View(coverType);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(CoverType input)
        {
            if (ModelState.IsValid)
            {
                _UnitOfWork.CoverType.Update(input);
                _UnitOfWork.Save();
                TempData["success"] = "CoverType updated successfully.";
                return RedirectToAction("index");
            }
            return View(input);
        }
        // Get
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            var coverType = _UnitOfWork.CoverType.GetFirstOrDefault(cat => cat.Id == id);
            if (coverType == null)
            {
                return NotFound();
            }
            return View(coverType);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePost(int? id)
        {
            var coverType = _UnitOfWork.CoverType.GetFirstOrDefault(cat => cat.Id == id);
            if (coverType == null)
            {
                return NotFound();
            }
            _UnitOfWork.CoverType.Remove(coverType);
            _UnitOfWork.Save();
            TempData["success"] = "CoverType deleted successfully.";
            return RedirectToAction("index");
        }
    }
}
