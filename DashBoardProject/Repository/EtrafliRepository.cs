using System;
using System.Data;
using Microsoft.Extensions.Configuration;

namespace DashBoardProject.Repository
{
    public class EtrafliRepository
    {
        private readonly IConfiguration _config;
        public EtrafliRepository(IConfiguration configuration)
        {
            _config = configuration;
        }

        public DataTable EtrafliMetod(DateTime startDate, DateTime endDate)
        {
            var result = new DataTable();

            // Define columns
            result.Columns.Add("name", typeof(string));
            result.Columns.Add("Initial_Balance", typeof(decimal));
            result.Columns.Add("Debet", typeof(decimal));
            result.Columns.Add("Credit", typeof(decimal));
            result.Columns.Add("SonQaliq", typeof(decimal));

            // Add mock rows for IT Company
            result.Rows.Add("Kassa", 12000.00m, 41000.50m, 15000.00m, 38000.50m);
            result.Rows.Add("Bank Hesabı", 155000.50m, 84000.00m, 32000.25m, 207000.25m);
            result.Rows.Add("Server İnfrastrukturu", 84000.00m, 32000.00m, 4100.00m, 111900.00m);
            result.Rows.Add("Şəbəkə Avadanlıqları", 12000.00m, 3000.00m, 4500.00m, 10500.00m);
            result.Rows.Add("Lisenziyalar və Proqramlar", 45000.00m, 12000.00m, 8700.00m, 48300.00m);

            return result;
        }
    }
}
