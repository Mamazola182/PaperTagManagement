using BE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BE.Controllers
{
    public class ReportController : Controller
    {
        private readonly FunewsManagementContext _context = new FunewsManagementContext();
        public ReportController(FunewsManagementContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet("api/Report")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetSimpleReport(DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.NewsArticles
                .Include(n => n.Category)
                .Include(n => n.CreatedBy)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(n => n.CreatedDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(n => n.CreatedDate <= toDate.Value);

            query = query.OrderByDescending(n => n.CreatedDate);

            var data = query.ToList();

            var byCategory = data
                .GroupBy(n => n.Category?.CategoryName)
                .Select(g => new {
                    GroupByName = g.Key,
                    TotalArticles = g.Count(),
                    ActiveArticles = g.Count(x => x.NewsStatus == true),
                    InactiveArticles = g.Count(x => x.NewsStatus != true)
                });

            var byAuthor = data
                .GroupBy(n => n.CreatedBy?.AccountName)
                .Select(g => new {
                    GroupByName = g.Key,
                    TotalArticles = g.Count(),
                    ActiveArticles = g.Count(x => x.NewsStatus == true),
                    InactiveArticles = g.Count(x => x.NewsStatus != true)
                });

            var byStatus = data
                .GroupBy(n => n.NewsStatus)
                .Select(g => new {
                    GroupByName = g.Key == true ? "Active" : "Inactive",
                    TotalArticles = g.Count()
                });

            var result = new
            {
                ByCategory = byCategory,
                ByAuthor = byAuthor,
                ByStatus = byStatus,
                TotalActive = data.Count(x => x.NewsStatus == true),
                TotalInactive = data.Count(x => x.NewsStatus != true),
                TotalAll = data.Count
            };

            return Ok(result);
        }
    }
}
