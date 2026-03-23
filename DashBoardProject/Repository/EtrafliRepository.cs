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

            // Add mock rows
            result.Rows.Add("Kassa", 3200.00m, 4100.50m, 1500.00m, 5800.50m);
            result.Rows.Add("Bank", 15500.50m, 8400.00m, 3200.25m, 20700.25m);
            result.Rows.Add("Əsas vəsaitlər", 45000.00m, 0.00m, 5000.00m, 40000.00m);
            result.Rows.Add("Ehtiyatlar", 12000.00m, 2500.00m, 3000.00m, 11500.00m);
            result.Rows.Add("Digər qısamüddətli aktivlər", 850.00m, 300.00m, 100.00m, 1050.00m);

            return result;
        }
    }
}
