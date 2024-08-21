using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBook.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _UnitOfWork;
        public CategoryController(IUnitOfWork unitOfWork)
        {
            _UnitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            IEnumerable<Category> categories = _UnitOfWork.Category.GetAll();
            return View(categories);
        }
        // Get
        public IActionResult Create()
        {
            return View();
        }
        // Post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category input)
        {
            if (ModelState.IsValid)
            {
                _UnitOfWork.Category.Add(input);
                _UnitOfWork.Save();
                TempData["success"] = "Category created successfully.";
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
            var category = _UnitOfWork.Category.GetFirstOrDefault(cat => cat.Id == id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category input)
        {
            if (ModelState.IsValid)
            {
                _UnitOfWork.Category.Update(input);
                _UnitOfWork.Save();
                TempData["success"] = "Category updated successfully.";
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
            var category = _UnitOfWork.Category.GetFirstOrDefault(cat => cat.Id == id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePost(int? id)
        {
            var category = _UnitOfWork.Category.GetFirstOrDefault(cat => cat.Id == id);
            if (category == null)
            {
                return NotFound();
            }
            _UnitOfWork.Category.Remove(category);
            _UnitOfWork.Save();
            TempData["success"] = "Category deleted successfully.";
            return RedirectToAction("index");
        }
    }
}
