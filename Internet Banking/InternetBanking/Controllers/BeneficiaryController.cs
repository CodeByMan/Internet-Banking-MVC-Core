using InternetBanking.Core.Application.Interfaces.Services;
using InternetBanking.Core.Application.ViewModels.Beneficiary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternetBanking.WebApp.Controllers
{
    [Authorize(Roles = "Client")]
    [AutoValidateAntiforgeryToken]
    public class BeneficiaryController : Controller
    {
        private readonly IBeneficiaryService _beneficiaryService;

        public BeneficiaryController(IBeneficiaryService beneficiaryService)
        {
            _beneficiaryService = beneficiaryService;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Beneficiaries = await _beneficiaryService.GetAllBeneficiaryViewModel();
            return View(new SaveBeneficiaryViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> AddBeneficiary(SaveBeneficiaryViewModel vm)
        {
            ViewBag.Beneficiaries = await _beneficiaryService.GetAllBeneficiaryViewModel();
            if (!ModelState.IsValid)
            {
                return View("Index", vm);
            }

            SaveBeneficiaryViewModel beneficiaryVm = await _beneficiaryService.Add(vm);
            if (beneficiaryVm.HasError)
            {
                vm.HasError = beneficiaryVm.HasError;
                vm.Error = beneficiaryVm.Error;
                return View("Index", vm);
            }

            return RedirectToRoute(new { controller = "Beneficiary", action = "Index" });
        }

        public async Task<ActionResult> Delete(int id)
        {
            var beneficiary = await _beneficiaryService.GetOwnedByIdAsync(id);
            if (beneficiary == null)
            {
                return Forbid();
            }

            return View("Delete", beneficiary);
        }

        [HttpPost]
        public async Task<ActionResult> DeleteBeneficiary(int id)
        {
            if (!await _beneficiaryService.DeleteOwnedAsync(id))
            {
                return Forbid();
            }

            return RedirectToRoute(new { controller = "Beneficiary", action = "Index" });
        }
    }
}
