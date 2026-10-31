using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Crm;
using Omnichannel.Application.Interfaces.Crm;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly ICustomerCrmService _customerService;
        private readonly ApplicationDbContext _context;

        public CustomerController(
            ICustomerCrmService customerService,
            ApplicationDbContext context)
        {
            _customerService = customerService;
            _context = context;
        }

        [HttpGet]
        [HasPermission("CUSTOMER_VIEW")]
        public async Task<IActionResult> Index([FromQuery] CustomerFilterRequest filter)
        {
            filter ??= new CustomerFilterRequest();
            var model = await _customerService.GetCustomersPagedAsync(filter);

            if (Request.Headers.ContainsKey("HX-Request"))
            {
                return PartialView("_CustomerList", model);
            }

            return View(model);
        }

        [HttpGet]
        [HasPermission("CUSTOMER_PROFILE_VIEW")]
        public async Task<IActionResult> Profile(int id)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "ID khách hàng không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var model = await _customerService.GetCustomer360ProfileAsync(id);
            if (model == null)
            {
                TempData["ErrorMessage"] = $"Không tìm thấy khách hàng với ID #{id}.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpGet]
        [HasPermission("CUSTOMER_EDIT")]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "ID khách hàng không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var editDto = await _customerService.GetCustomerForEditAsync(id);
            if (editDto == null)
            {
                TempData["ErrorMessage"] = $"Không tìm thấy khách hàng với ID #{id}.";
                return RedirectToAction(nameof(Index));
            }

            // Load consultants list for dropdown
            var consultants = await _context.Users
                .AsNoTracking()
                .Where(u => u.IsActive && (u.UserType == 2 || u.UserRoles.Any(ur => ur.Role.Name == "Manager" || ur.Role.Name == "Admin" || ur.Role.Name == "SuperAdmin")))
                .OrderBy(u => u.FullName)
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = $"{u.FullName} ({u.Username})",
                    Selected = u.Id == editDto.ConsultantUserId
                })
                .ToListAsync();

            ViewBag.Consultants = consultants;

            return View(editDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("CUSTOMER_EDIT")]
        public async Task<IActionResult> Edit(int id, CustomerEditDto dto)
        {
            if (id != dto.UserId)
            {
                TempData["ErrorMessage"] = "Dữ liệu không khớp.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                var consultants = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.IsActive && (u.UserType == 2 || u.UserRoles.Any(ur => ur.Role.Name == "Manager" || ur.Role.Name == "Admin" || ur.Role.Name == "SuperAdmin")))
                    .OrderBy(u => u.FullName)
                    .Select(u => new SelectListItem
                    {
                        Value = u.Id.ToString(),
                        Text = $"{u.FullName} ({u.Username})",
                        Selected = u.Id == dto.ConsultantUserId
                    })
                    .ToListAsync();
                ViewBag.Consultants = consultants;
                return View(dto);
            }

            var success = await _customerService.UpdateCustomerAsync(dto);
            if (!success)
            {
                TempData["ErrorMessage"] = "Cập nhật hồ sơ khách hàng thất bại.";
                return View(dto);
            }

            TempData["SuccessMessage"] = $"Đã cập nhật hồ sơ khách hàng {dto.FullName} thành công!";
            return RedirectToAction(nameof(Profile), new { id = dto.UserId });
        }
    }
}
