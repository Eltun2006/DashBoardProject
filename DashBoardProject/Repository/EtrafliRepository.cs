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
                string IlkinQaliqsql = @"
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
                using (var cmd = new Oracle.ManagedDataAccess.Client.OracleCommand(IlkinQaliqsql, conn))
                {
                    cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("startDate", startDate));
                    using (var adapter = new Oracle.ManagedDataAccess.Client.OracleDataAdapter(cmd))
                    {
                        adapter.Fill(result);
                    }
                }

                var Medaxilsql = @"  SELECT t.name,
                                           SUM(t.Debet) AS Debet
                                    FROM (
                                        SELECT pa.name,
                                               NVL(SUM(p.Amount), 0) AS Debet,
                                               0 AS Credit   
                                        FROM Payment_Accounts pa
                                        INNER JOIN Payments p
                                            ON pa.Id = p.Debit_Id
                                           AND p.Payment_Date BETWEEN :startDate AND :endDate + 1
                                           AND p.RELATION_DOCUMENT_TYPE_CODE <> 18
                                        GROUP BY pa.name
                                    ) t
                                    GROUP BY t.name
                                    ORDER BY t.name";

                using (var cmd = new Oracle.ManagedDataAccess.Client.OracleCommand(Medaxilsql, conn))
                {
                    cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("startDate", startDate));
                    cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("endDate", endDate));

                    using (var adapter = new Oracle.ManagedDataAccess.Client.OracleDataAdapter(cmd))
                    {
                        adapter.Fill(result);
                    }
                }


                var Mexaricsql = @"SELECT t.name,
                                           SUM(t.Credit) AS Credit
                                    FROM (
                                        SELECT pa.name,
                                               NVL(SUM(p.Amount), 0) AS Credit,
                                               SUM(p.Amount) AS Debet  -- Debet lazımdırsa saxla, yoxsa sil
                                        FROM Payment_Accounts pa
                                        INNER JOIN Payments p
                                            ON pa.Id = p.Credit_Id
                                           AND p.Payment_Date BETWEEN :startDate AND :endDate + 1
                                           AND p.RELATION_DOCUMENT_TYPE_CODE <> 18
                                        GROUP BY pa.name
                                    ) t
                                    GROUP BY t.name
                                    ORDER BY t.name";

                using (var cmd = new Oracle.ManagedDataAccess.Client.OracleCommand(Mexaricsql, conn))
                {
                    cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("startDate", startDate));
                    cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("endDate", endDate));

                    using (var adapter = new Oracle.ManagedDataAccess.Client.OracleDataAdapter(cmd))
                    {
                        adapter.Fill(result);
                    }
                }

                var SonQaliqsql = @"  SELECT t.name,
                                             SUM(t.SonQaliq) AS SonQaliq
                                        FROM (
                                                SELECT pa.name,
                                                       NVL(SUM(-p.Amount), 0) AS SonQaliq,
                                                       SUM(p.amount) credit,
                                                       0 debet
                                                  FROM Payment_Accounts pa
                                                       INNER JOIN Payments p
                                                          ON pa.Id = p.Credit_Id AND p.Payment_Date < :endDate+1
                                              GROUP BY pa.name
                                              UNION ALL
                                                SELECT pa.name,
                                                       NVL(SUM(p.Amount), 0) AS SonQaliq,
                                                       0 credit,
                                                       SUM(p.amount) debet
                                                  FROM Payment_Accounts pa
                                                       INNER JOIN Payments p
                                                          ON pa.Id = p.Debit_Id AND p.Payment_Date < :endDate+1
                                              GROUP BY pa.name
                                            ) t
                                    GROUP BY t.name
                                    ORDER BY t.name";


                using (var cmd = new Oracle.ManagedDataAccess.Client.OracleCommand(SonQaliqsql, conn))
                {
                    cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("endDate", endDate));
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
