using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Infrastructure.Data;
using System.Linq;

namespace Omnichannel.Web.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("Product/QuickView/{id}")]
        public async Task<IActionResult> QuickView(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Batches)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound("Sản phẩm không tồn tại.");
            }

            return PartialView("_QuickView", product);
        }

        [HttpGet("Product/Search")]
        public async Task<IActionResult> Search(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Content(""); // Empty response to clear results
            }

            var query = _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive && (p.Name.Contains(q) || p.Id.ToString().Contains(q)));

            var products = await query.Take(5).ToListAsync();
            return PartialView("_SearchResults", products);
        }
    }
}
