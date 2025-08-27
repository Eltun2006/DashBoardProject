using DashBoardProject.Repository;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace DashBoardProject.Controllers
{
    public class DashBoardController : Controller
    {
        private DashBoardRepo _repo;
        private EtrafliRepository _repos;
        private IConfiguration _config;

        public DashBoardController(IConfiguration configuration) 
        {
            _repo = new DashBoardRepo(configuration);
            _config = configuration;
            _repos=new EtrafliRepository(configuration);
        }

        [HttpGet]
        public IActionResult FullDashBoard(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today.AddMonths(-1);
            var end = endDate ?? DateTime.Today;


            var model = _repo.FullDashBoardMetod(start, end);

            ViewBag.StartDate = start.ToString("yyyy-MM-dd");
            ViewBag.EndDate = end.ToString("yyyy-MM-dd");

            return View(model);
        }

        [HttpGet]
        public IActionResult Details(DateTime? startDate, DateTime? endDate)
        {

            var start = startDate ?? DateTime.Today.AddMonths(-1);
            var end = endDate ?? DateTime.Today;


            var model = _repos.EtrafliMetod(start,end);
            ViewBag.StartDate = start.ToString("yyyy-MM-dd");
            ViewBag.EndDate = end.ToString("yyyy-MM-dd");



            return View(model);
        }
    }
}
