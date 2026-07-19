using InternetBanking.Core.Application.Enums;
using InternetBanking.Core.Application.Interfaces.Services;
using InternetBanking.Core.Application.ViewModels.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternetBanking.WebApp.Controllers
{
    [Authorize(Roles = "Admin")]
    [AutoValidateAntiforgeryToken]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        public async Task<IActionResult> Index(string userId)
        {
            var products = await _productService.GetProductsByUserViewModel(userId);
            ViewBag.UserId = userId;
            return View("Index", products);
        }

        public IActionResult Create(string userId)
        {
            SaveProductViewModel productVm = new()
            {
                UserId = userId
            };

            return View("SaveProduct", productVm);
        }

        [HttpPost]
        public async Task<IActionResult> Create(SaveProductViewModel vm)
        {
            if (!Enum.IsDefined(typeof(ProductType), vm.ProductType))
            {
                ModelState.AddModelError(nameof(vm.ProductType), "Please select a valid product type.");
            }

            if (!ModelState.IsValid)
            {
                return View("SaveProduct", vm);
            }

            var createdProduct = await _productService.Add(vm);
            if (createdProduct.HasError)
            {
                ModelState.AddModelError(string.Empty, createdProduct.Error ?? "The product could not be created.");
                return View("SaveProduct", vm);
            }

            return RedirectToRoute(new { controller = "Product", action = "Index", userId = vm.UserId });
        }

        public async Task<ActionResult> Delete(int id)
        {
            var product = await _productService.GetByIdSaveViewModel(id);

            if (!product.IsPrincipal && product.Debt == 0)
            {
                return View("Delete", product);
            }

            return RedirectToRoute(new { controller = "Product", action = "Index", userId = product.UserId });
        }

        [HttpPost]
        public async Task<ActionResult> DeleteProduct(int id)
        {
            var product = await _productService.GetByIdSaveViewModel(id);
            if (product.IsPrincipal || product.Debt != 0)
            {
                return RedirectToRoute(new { controller = "Product", action = "Index", userId = product.UserId });
            }

            await _productService.Delete(id);
            return RedirectToRoute(new { controller = "Product", action = "Index", userId = product.UserId });
        }
    }
}
