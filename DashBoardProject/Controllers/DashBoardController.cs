using DashBoardProject.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DashBoardProject.Controllers
{
    public class DashBoardController : Controller
    {
        private DashBoardRepo _repo;

        public DashBoardController(IConfiguration configuration) 
        {
            _repo = new DashBoardRepo(configuration);
        }

        [HttpGet]
        public IActionResult FullDashBoard(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today.AddYears(-5);
            var end = endDate ?? DateTime.Today;

            var model = _repo.FullDashBoardMetod(start, end);

            ViewBag.StartDate = start.ToString("yyyy-MM-dd");
            ViewBag.EndDate = end.ToString("yyyy-MM-dd");

            return View(model);
        }
    }
}
