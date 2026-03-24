using System;
using System.Collections.Generic;
using DashBoardProject.Models;
using Microsoft.Extensions.Configuration;

namespace DashBoardProject.Repository
{
    public class DashBoardRepo
    {
        private readonly IConfiguration _configuration;
        // Mocking behavior, we can keep the fields even if not used, or remove them.
        private readonly int _SoftwareID;
        private readonly int _AssetID;
        private readonly int _ExpenseID;
        private readonly int _DepartmentID;

        public DashBoardRepo(IConfiguration configuration)
        {
            _configuration = configuration;
            _SoftwareID = int.TryParse(_configuration["AppSettings:Software_ID"], out var sw) ? sw : 0;
            _AssetID = int.TryParse(_configuration["AppSettings:Asset_ID"], out var ast) ? ast : 0;
            _ExpenseID = int.TryParse(_configuration["AppSettings:Expense_ID"], out var exp) ? exp : 0;
            _DepartmentID = int.TryParse(_configuration["AppSettings:Department_ID"], out var dep) ? dep : 0;
        }

        public FullDashBoardModel FullDashBoardMetod(DateTime? startDate = null, DateTime? endDate = null, int? softwareID = null, int? assetID = null, int? expenseID = null)
        {
            var start = startDate ?? DateTime.Today.AddMonths(-1);
            var end = endDate ?? DateTime.Today;
    
            var balances = GetAccountBalance(start, end);
            var dovriyye = GetTurnoverBalance(start, end, softwareID);
            var material = GetMalMaterialBalance(start, end, assetID, expenseID);

            return new FullDashBoardModel
            {
                MalMulk = balances,
                Dovriyye = dovriyye,
                MalMaterialHereketleri = material
            };
        }

        private AccountBalance GetAccountBalance(DateTime startDate, DateTime endDate)
        {
            // IT Company Financials
            return new AccountBalance
            {
                IlkinQaliq = new MalMulkHereketleri { Bank = 155000.50m, Kassa = 12000.00m },
                Medaxil = new MalMulkHereketleri { Bank = 84000.00m, Kassa = 41000.50m },
                Mexaric = new MalMulkHereketleri { Bank = 32000.25m, Kassa = 15000.00m },
                SonQaliq = new MalMulkHereketleri { Bank = 207000.25m, Kassa = 38000.50m }
            };
        }

        private TurnoverStatistics GetTurnoverBalance(DateTime startDate, DateTime endDate, int? softwareID)
        {
            // IT Company Revenue Breakdown
            return new TurnoverStatistics
            {
                TeskilatUzre = new ByOrganization 
                {
                    Proqram_Teminati = 1200,
                    Konsaltinq_Xidmetleri = 450,
                    Texniki_Destek = 300,
                    Abuna_Yazilislari = 850
                },
                XidmetTipi = new Byservicetype
                {
                    Layihe_Isleri = 145000.00m,
                    Autsorsinq = 98000.50m
                },
                Xidmet_Categoryasi = new List<Xidmet_Categoryasi>
                {
                    new Xidmet_Categoryasi { CategoryName = "Veb İnkişaf", TotalPrice = 65000.00m },
                    new Xidmet_Categoryasi { CategoryName = "Bulud İnfrastrukturu", TotalPrice = 42500.00m },
                    new Xidmet_Categoryasi { CategoryName = "Mobil Tətbiqlər", TotalPrice = 55000.00m },
                    new Xidmet_Categoryasi { CategoryName = "Süni İntellekt", TotalPrice = 28500.00m },
                    new Xidmet_Categoryasi { CategoryName = "Kiber Təhlükəsizlik", TotalPrice = 36400.00m },
                    new Xidmet_Categoryasi { CategoryName = "UI/UX Dizayn", TotalPrice = 15600.50m }
                }
            };
        }

        private Inventory_Movement GetMalMaterialBalance(DateTime startDate, DateTime endDate, int? assetID, int? expenseID)
        {
            // IT Asset Management
            return new Inventory_Movement
            {
                IlkinQaliq = new Goods_and_materials { Laptops = 45000.00m, Serverler = 84000.00m, Digerleri = 12000.00m },
                Medaxil = new Goods_and_materials { Laptops = 12000.00m, Serverler = 32000.00m, Digerleri = 3000.00m },
                Mexaric = new Goods_and_materials { Laptops = 8200.00m, Serverler = 4100.00m, Digerleri = 4500.00m },
                Silinme = new Goods_and_materials { Laptops = 500.00m, Serverler = 0.00m, Digerleri = 150.00m },
                SonQaliq = new Goods_and_materials { Laptops = 48300.00m, Serverler = 111900.00m, Digerleri = 10350.00m }
            };
        }
    }
}
