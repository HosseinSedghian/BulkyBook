using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyBook.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _UnitOfWork;
        private readonly IWebHostEnvironment _WebHostEnvironment;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _UnitOfWork = unitOfWork;
            _WebHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            return View();
        }
        // Get
        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new()
            {
                Product = null,
                categoryList = _UnitOfWork.Category.GetAll().Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString()
                }),
                coverTypeList = _UnitOfWork.CoverType.GetAll().Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString()
                })
            };
            if (id == null || id == 0)
            {
                // Create
                productVM.Product = new();
            }
            else
            {
                // Update
                productVM.Product = _UnitOfWork.Product.GetFirstOrDefault(x => x.Id == id);
            }
            return View(productVM);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductVM input, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _WebHostEnvironment.WebRootPath;
                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString();
                    var uploads = Path.Combine(wwwRootPath, @"images\products");
                    var extension = Path.GetExtension(file.FileName);
                    var finalFullPath = Path.Combine(uploads, fileName + extension);

                    if(input.Product.ImageUrl != null)
                    {
                        var oldImagePath = Path.Combine(wwwRootPath, input.Product.ImageUrl.TrimStart('\\'));
                        if(System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    using (var fileStream = new FileStream(finalFullPath, FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                    input.Product.ImageUrl = @"\images\products\" + fileName + extension;
                }
                if(input.Product.Id == 0)
                {
                    _UnitOfWork.Product.Add(input.Product);
                    TempData["success"] = "Product created successfully.";
                }
                else
                {
                    _UnitOfWork.Product.Update(input.Product);
                    TempData["success"] = "Product updated successfully.";
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
            var productList = _UnitOfWork.Product.GetAll(includeProperties:$"{nameof(Product.Category)},{nameof(Product.CoverType)}");
            return Json(new { data = productList});
        }
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var product = _UnitOfWork.Product.GetFirstOrDefault(cat => cat.Id == id);
            if (product == null)
            {
                return Json(new {success=false, message="Error while deleting."});
            }
            string wwwRootPath = _WebHostEnvironment.WebRootPath;
            var oldImagePath = Path.Combine(wwwRootPath, product.ImageUrl.TrimStart('\\'));
            if (System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Delete(oldImagePath);
            }
            _UnitOfWork.Product.Remove(product);
            _UnitOfWork.Save();
            return Json(new {success=true, message="Delete successful."});
        }
        #endregion
    }
}
