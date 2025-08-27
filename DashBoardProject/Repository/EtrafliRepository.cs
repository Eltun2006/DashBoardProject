using System.Data;

namespace DashBoardProject.Repository
{
    public class EtrafliRepository
    {
        private IConfiguration _config;
        public EtrafliRepository(IConfiguration configuration)
        {
            _config = configuration;
        }

        public DataTable EtrafliMetod(DateTime startDate, DateTime endDate)
        {
            var result = new DataTable();
            using (var conn = new Oracle.ManagedDataAccess.Client.OracleConnection(_config.GetConnectionString("SqlConnection")))
            {
                conn.Open();
                string sql = @"
                SELECT t.name,
                       SUM(t.Initial_Balance) AS Initial_Balance
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
              GROUP BY t.name
              ORDER BY t.name";
                using (var cmd = new Oracle.ManagedDataAccess.Client.OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("startDate", startDate));
                    using (var adapter = new Oracle.ManagedDataAccess.Client.OracleDataAdapter(cmd))
                    {
                        adapter.Fill(result);
                    }
                }
            }
            return result;
        }

    }
}
