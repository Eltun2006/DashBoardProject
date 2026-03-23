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
        private readonly int _InsuranceID;
        private readonly int _DermanID;
        private readonly int _SerfiyyatID;
        private readonly int _DepartmentID;

        public DashBoardRepo(IConfiguration configuration)
        {
            _configuration = configuration;
            _InsuranceID = int.TryParse(_configuration["AppSettings:Insurance_ID"], out var ins) ? ins : 0;
            _DermanID = int.TryParse(_configuration["AppSettings:Derman_ID"], out var der) ? der : 0;
            _SerfiyyatID = int.TryParse(_configuration["AppSettings:Serfiyyat_ID"], out var ser) ? ser : 0;
            _DepartmentID = int.TryParse(_configuration["AppSettings:Department_ID"], out var dep) ? dep : 0;
        }

        public FullDashBoardModel FullDashBoardMetod(DateTime? startDate = null, DateTime? endDate = null, int? insuranceID = null, int? DermanID = null, int? SerfiyyatID = null)
        {
            var start = startDate ?? DateTime.Today.AddMonths(-1);
            var end = endDate ?? DateTime.Today;
    
            var balances = GetAccountBalance(start, end);
            var dovriyye = GetTurnoverBalance(start, end, insuranceID);
            var material = GetMalMaterialBalance(start, end, DermanID, SerfiyyatID);

            return new FullDashBoardModel
            {
                MalMulk = balances,
                Dovriyye = dovriyye,
                MalMaterialHereketleri = material
            };
        }

        private AccountBalance GetAccountBalance(DateTime startDate, DateTime endDate)
        {
            // Providing realistic mock mock data
            return new AccountBalance
            {
                IlkinQaliq = new MalMulkHereketleri { Bank = 15500.50m, Kassa = 3200.00m },
                Medaxil = new MalMulkHereketleri { Bank = 8400.00m, Kassa = 4100.50m },
                Mexaric = new MalMulkHereketleri { Bank = 3200.25m, Kassa = 1500.00m },
                SonQaliq = new MalMulkHereketleri { Bank = 20700.25m, Kassa = 5800.50m }
            };
        }

        private TurnoverStatistics GetTurnoverBalance(DateTime startDate, DateTime endDate, int? InsuranceID)
        {
            // Providing realistic mock mock data
            return new TurnoverStatistics
            {
                TeskilatUzre = new ByOrganization 
                {
                    Icbari_Sigorta = 450,
                    Diger_Sigorta = 120,
                    Endirimler = 45,
                    Oz_Hesabina = 680
                },
                XidmetTipi = new Byservicetype
                {
                    Emeliyyat = 45000.00m,
                    Poliklinik = 28000.50m
                },
                Xidmet_Categoryasi = new List<Xidmet_Categoryasi>
                {
                    new Xidmet_Categoryasi { CategoryName = "Kardiologiya", TotalPrice = 15000.00m },
                    new Xidmet_Categoryasi { CategoryName = "Nevrologiya", TotalPrice = 12500.00m },
                    new Xidmet_Categoryasi { CategoryName = "Cərrahiyyə", TotalPrice = 25000.00m },
                    new Xidmet_Categoryasi { CategoryName = "Terapiya", TotalPrice = 8500.00m },
                    new Xidmet_Categoryasi { CategoryName = "Oftalmologiya", TotalPrice = 6400.00m },
                    new Xidmet_Categoryasi { CategoryName = "Digərləri", TotalPrice = 5600.50m }
                }
            };
        }

        private Inventory_Movement GetMalMaterialBalance(DateTime startDate, DateTime endDate, int? DermanID, int? SerfiyyatID)
        {
            // Providing realistic mock mock data
            return new Inventory_Movement
            {
                IlkinQaliq = new Goods_and_materials { Derman = 12500.00m, Serfiyyat = 8400.00m, Digerleri = 1200.00m },
                Medaxil = new Goods_and_materials { Derman = 4500.00m, Serfiyyat = 3200.00m, Digerleri = 300.00m },
                Mexaric = new Goods_and_materials { Derman = 5200.00m, Serfiyyat = 4100.00m, Digerleri = 450.00m },
                Silinme = new Goods_and_materials { Derman = 150.00m, Serfiyyat = 80.00m, Digerleri = 0.00m },
                SonQaliq = new Goods_and_materials { Derman = 11650.00m, Serfiyyat = 7420.00m, Digerleri = 1050.00m }
            };
        }
    }
}
