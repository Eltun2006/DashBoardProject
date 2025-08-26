using DashBoardProject.Repository;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace DashBoardProject.Controllers
{
    public class DashBoardController : Controller
    {
        private DashBoardRepo _repo;
        private IConfiguration _config;

        public DashBoardController(IConfiguration configuration) 
        {
            _repo = new DashBoardRepo(configuration);
            _config = configuration;
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

        public IActionResult Details()
        {
            var result = new DataTable();
            using (var conn = new OracleConnection(_config.GetConnectionString("SqlConnection")))
            {
                conn.Open();
                string sql = @"
                SELECT t.name,
                       SUM(t.Initial_Balance) AS Initial_Balance,
                       SUM(t.credit) AS credit,
                       SUM(t.debet) AS debet
                  FROM (
                          SELECT pa.name,
                                 NVL(SUM(-p.Amount), 0) AS Initial_Balance,
                                 SUM(p.amount) credit,
                                 0 debet
                            FROM Payment_Accounts pa
                                 INNER JOIN Payments p
                                    ON pa.Id = p.Credit_Id AND p.Payment_Date < :startDate
                        GROUP BY pa.name
                        UNION ALL
                          SELECT pa.name,
                                 NVL(SUM(p.Amount), 0) AS Initial_Balance,
                                 0 credit,
                                 SUM(p.amount) debet
                            FROM Payment_Accounts pa
                                 INNER JOIN Payments p
                                    ON pa.Id = p.Debit_Id AND p.Payment_Date < :startDate
                        GROUP BY pa.name
                ) t
                GROUP BY t.name";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add(new OracleParameter("startDate", DateTime.Now)); // burda startDate ötürürsən
                    using (var adapter = new OracleDataAdapter(cmd))
                    {
                        adapter.Fill(result);
                    }
                }
            }

            return View(result);
        }
    }
}
