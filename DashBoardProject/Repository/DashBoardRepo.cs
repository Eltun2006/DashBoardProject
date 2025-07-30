using DashBoardProject.Models;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace DashBoardProject.Repository
{
    public class DashBoardRepo
    {
        private readonly IConfiguration _configuration;
        private readonly int _InsuranceID;
        private readonly int _DermanID;
        private readonly int _SerfiyyatID;
        private readonly int _DepartmentID;

        public DashBoardRepo(IConfiguration configuration)
        {
            _configuration = configuration;
            _InsuranceID = int.Parse(_configuration["AppSettings:Insurance_ID"]);
            _DermanID = int.Parse(_configuration["AppSettings:Derman_ID"]);
            _SerfiyyatID = int.Parse(_configuration["AppSettings:Serfiyyat_ID"]);
            _DepartmentID = int.Parse(_configuration["AppSettings:Department_ID"]);
        }

        public FullDashBoardModel FullDashBoardMetod(DateTime? startDate = null, DateTime? endDate = null, int? insuranceID = null, int? DermanID = null, int? SerfiyyatID = null)
        {
            var start = startDate ?? DateTime.Today.AddYears(-5);
            var end = endDate ?? DateTime.Today;
    

            var balances = GetAccountBalance(start, end);
            var dovriyye = GetDovriyyeBalance(start, end, insuranceID);
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

            var IlkinQaliqSql = @"SELECT SUM (CASE WHEN account_type = 1146 THEN Initial_Balance END) AS bank,
       SUM (CASE WHEN account_type = 1147 THEN Initial_Balance END) AS kassa 
  FROM (  SELECT pa.Account_Type,
                pa.name,
                 nvl (SUM (-p.Amount), 0) AS Initial_Balance,
                 sum(p.amount) credit,
                 0 debet
            FROM Payment_Accounts pa
                 INNER JOIN Payments p
                    ON pa.Id = p.Credit_Id AND p.Payment_Date < :startDate
        GROUP BY pa.Account_Type,pa.name
        UNION ALL
          SELECT pa.Account_Type,pa.name,
                 nvl (SUM (p.Amount), 0) AS Initial_Balance,
                 0 credit,
                 sum(p.amount) debet
            FROM Payment_Accounts pa
                 INNER JOIN Payments p
                    ON pa.Id = p.Debit_Id AND p.Payment_Date < :startDate
        GROUP BY pa.Account_Type , pa.name)";


            var MedaxilSql = @"SELECT 
                                    SUM(CASE WHEN account_type = 1146 THEN Initial_Balance END) AS bank,
                                    SUM(CASE WHEN account_type = 1147 THEN Initial_Balance END) AS kassa
                                FROM (
                                    SELECT 
                                        pa.Account_Type,
                                        COALESCE(SUM(p.Amount), 0) AS Initial_Balance
                                    FROM Payment_Accounts pa
                                    INNER JOIN Payments p 
                                        ON pa.Id = p.Debit_Id 
                                        AND p.Payment_Date BETWEEN :startDate AND :endDate+1 
                                        AND p.RELATION_DOCUMENT_TYPE_CODE <> 18
                                    GROUP BY pa.Account_Type
                                )";


            var MexaricSql = @" SELECT 
                                SUM(CASE WHEN account_type = 1146 THEN Initial_Balance END) AS bank,
                                SUM(CASE WHEN account_type = 1147 THEN Initial_Balance END) AS kassa
                                FROM (
                                    SELECT 
                                        pa.Account_Type,
                                        COALESCE(SUM(p.Amount), 0) AS Initial_Balance
                                    FROM Payment_Accounts pa
                                    INNER JOIN Payments p 
                                        ON pa.Id = p.Credit_Id 
                                        AND p.Payment_Date BETWEEN :startDate AND :endDate+1
                                        AND p.RELATION_DOCUMENT_TYPE_CODE <> 18
                                    GROUP BY pa.Account_Type
                                )";


            var SonQaliqSql = @"SELECT 
                                    SUM(CASE WHEN account_type = 1146 THEN amount ELSE 0 END) AS bank,
                                    SUM(CASE WHEN account_type = 1147 THEN amount ELSE 0 END) AS kassa
                                FROM (
                                    SELECT 
                                        pa.account_type, 
                                        SUM(p.amount) AS amount
                                    FROM payments p
                                    INNER JOIN payment_accounts pa ON p.debit_id = pa.id
                                    WHERE p.payment_date < :endDatePlusOne+1
                                    GROUP BY pa.account_type

                                    UNION ALL

                                    SELECT 
                                        pa.account_type, 
                                        -SUM(p.amount) AS amount
                                    FROM payments p
                                    INNER JOIN payment_accounts pa ON p.credit_id = pa.id
                                    WHERE p.payment_date < :endDatePlusOne
                                    GROUP BY pa.account_type
                                )";


            var endDatePlusOne = endDate.AddDays(1);

            var IlkinQaliq = ExecuteQuery(IlkinQaliqSql, startDate: startDate);
            var Medaxil = ExecuteQuery(MedaxilSql, startDate: startDate, endDate: endDate);
            var Mexaric = ExecuteQuery(MexaricSql, startDate: startDate, endDate: endDate);
            var SonQaliq = ExecuteQuery(SonQaliqSql, endDatePlusOne: endDatePlusOne);


            return new AccountBalance
            {
                IlkinQaliq = IlkinQaliq ?? new MalMulkHereketleri(),
                Medaxil = Medaxil ?? new MalMulkHereketleri(),
                Mexaric = Mexaric ?? new MalMulkHereketleri(),
                SonQaliq = SonQaliq ?? new MalMulkHereketleri()
            };
        }

        private MalMulkHereketleri ExecuteQuery(string sql, DateTime? startDate = null, DateTime? endDate = null, DateTime? endDatePlusOne = null)
        {
            var connectionstring = _configuration.GetConnectionString("SqlConnection");

            using var conn = new OracleConnection(connectionstring);
            conn.Open();

            using var cmd = new OracleCommand(sql, conn);

            if (startDate.HasValue)
                cmd.Parameters.Add(new OracleParameter("startDate", startDate.Value));
            if (endDate.HasValue)
                cmd.Parameters.Add(new OracleParameter("endDate", endDate.Value));
            if (endDatePlusOne.HasValue)
                cmd.Parameters.Add(new OracleParameter("endDatePlusOne", endDatePlusOne.Value));

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return new MalMulkHereketleri
                {
                    Bank = reader.IsDBNull(0) ? 0 : reader.GetDecimal(0),
                    Kassa = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1),
                };
            }

            return new MalMulkHereketleri();

        }

        private Dovriyye_Statistikasi GetDovriyyeBalance(DateTime startDate, DateTime endDate, int? InsuranceID)
        {
            var TeskilatUzreSql = String.Format(@"SELECT SUM (CASE WHEN Qrup = 'Icbari_Sigorta' THEN Pasiyent_Sayi ELSE 0 END)
          AS Icbari_Sigorta,
       SUM (
          CASE WHEN Qrup = 'Diger_Sigortalar' THEN Pasiyent_Sayi ELSE 0 END)
          AS Diger_Sigorta,
       SUM (CASE WHEN Qrup = 'Endirimler' THEN Pasiyent_Sayi ELSE 0 END)
          AS Endirimler,
       SUM (CASE WHEN Qrup = 'Oz_Hesabina' THEN Pasiyent_Sayi ELSE 0 END)
          AS Oz_Hesabina
  FROM (  SELECT Qrup,
                 SUM (AMOUNT
                    )
                    AS Pasiyent_Sayi
            FROM (SELECT CASE
                            WHEN g.group_type = 2 AND g.id = {0}
                            THEN
                               'Icbari_Sigorta'
                            WHEN g.group_type = 2 AND g.id <> {0}
                            THEN
                               'Diger_Sigortalar'
                            WHEN g.group_type = 3
                            THEN
                               'Endirimler'
                            ELSE
                               'Oz_Hesabina'
                         END
                            AS Qrup,
                         CASE
                       WHEN g.group_type = 1 THEN od.paid_amount
                       ELSE od.paid_amount + od.group_amount
                    END AMOUNT
                    FROM patients p
                         JOIN
                         operations o
                            ON     p.id = o.patient_id
                               AND o.deleted = 0
                               AND o.is_operation = 0
                         JOIN
                         operation_details od
                            ON     od.operation_id = o.id
                               AND od.status > 2
                               AND od.status <> 60
                               AND od.deleted = 0
                               AND o.document_date >= :startDate
                               AND o.document_date < :endDatePlusOne
                         JOIN groups g ON od.GROUP_ID = g.id
                  UNION ALL
                  SELECT 
                         CASE
                            WHEN g.group_type = 2 AND g.id = {1}
                            THEN
                               'Icbari_Sigorta'
                            WHEN g.group_type = 2 AND g.id <> {1}
                            THEN
                               'Diger_Sigortalar'
                            WHEN g.group_type = 3
                            THEN
                               'Endirimler'
                            ELSE
                               'Oz_Hesabina'
                         END
                            AS Qrup, 
                         CASE
                       WHEN g.group_type = 1 THEN od.paid_amount
                       ELSE od.paid_amount + od.group_amount END AMOUNT
                    FROM patients p
                         JOIN
                         operations o
                            ON     p.id = o.patient_id
                               AND o.deleted = 0
                               AND o.is_operation = 1
                         JOIN
                         operation_details od
                            ON     od.operation_id = o.id
                               AND od.tab_index = 1
                               AND od.deleted = 0
                               AND od.operation_date >= :startDate
                               AND od.operation_date < :endDatePlusOne
                         JOIN groups g ON od.GROUP_ID = g.id

                  UNION ALL
                  SELECT 
                         CASE
                            WHEN g.group_type = 2 AND g.id = {1}
                            THEN
                               'Icbari_Sigorta'
                            WHEN g.group_type = 2 AND g.id <> {1}
                            THEN
                               'Diger_Sigortalar'
                            WHEN g.group_type = 3
                            THEN
                               'Endirimler'
                            ELSE
                               'Oz_Hesabina'
                         END
                            AS Qrup, 
                         CASE
                       WHEN g.group_type = 1 THEN od.paid_amount
                       ELSE od.paid_amount + od.group_amount END AMOUNT
                    FROM patients p
                         JOIN
                         operations o
                            ON     p.id = o.patient_id
                               AND o.deleted = 0
                               AND o.is_operation = 1
                         JOIN
                         operation_details od
                            ON     od.operation_id = o.id
                               AND od.tab_index = 2
                               AND od.deleted = 0
                               AND od.operation_date >= :startDate
                               AND od.operation_date < :endDatePlusOne
                         JOIN groups g ON od.GROUP_ID = g.id
and od.SERVICE_PRICE_INCLUDED=0) sub
        GROUP BY Qrup) final", _InsuranceID, _InsuranceID);

            var XidmetTipiSql = @"SELECT  
    'Poliklinik' AS service_type,
    SUM(
        CASE
            WHEN g.group_type = 1 THEN od.paid_amount
            ELSE od.paid_amount + od.group_amount
        END
    ) AS total_price
FROM operations o
JOIN operation_details od ON od.operation_id = o.id
JOIN groups g ON od.GROUP_ID = g.id
WHERE od.status > 2
  AND od.status <> 60
  AND od.deleted = 0
  AND o.deleted = 0
  AND o.is_operation = 0
  AND o.document_date >= :startDate
  AND o.document_date < :endDatePlusOne

UNION ALL

SELECT
    'Emeliyyat' AS service_type,
    SUM(
        CASE
            WHEN g.group_type = 1 THEN od.paid_amount
            ELSE od.paid_amount + od.group_amount
        END
    ) AS total_price
FROM operations o
JOIN operation_details od ON od.operation_id = o.id
JOIN groups g ON od.GROUP_ID = g.id
WHERE od.tab_index IN (1, 2)
  AND od.deleted = 0
  AND (od.SERVICE_PRICE_INCLUDED = 0 OR od.SERVICE_PRICE_INCLUDED IS NULL)  -- Əgər şərt lazımdırsa
  AND o.deleted = 0
  AND o.is_operation = 1
  AND od.operation_date >= :startDate
  AND od.operation_date < :endDatePlusOne";


            var Xidmet_CategoryasiSql = @"SELECT category_name, total_amount_sum
FROM (
    SELECT category_name, SUM(total_amount) total_amount_sum
    FROM (
        -- Poliklinik
        SELECT CAST(sc.name AS NVARCHAR2(100)) category_name,
               SUM(CASE WHEN gr.group_type = 1 THEN op.paid_amount ELSE op.paid_amount + op.group_amount END) total_amount
        FROM services s
        JOIN operation_details op ON s.id = op.service_id
        JOIN operations o ON o.id = op.operation_id
        JOIN service_categories sc ON sc.id = s.category_id
        JOIN groups gr ON op.group_id = gr.id
        WHERE o.deleted = 0
          AND op.deleted = 0
          AND op.status > 2
          AND op.status <> 60
          AND o.is_operation = 0
          AND o.document_date >= :startDate
          AND o.document_date < :endDatePlusOne
        GROUP BY sc.name

        UNION ALL

        -- Emeliyyat
        SELECT CAST(sc.name AS NVARCHAR2(100)) category_name,
               SUM(CASE WHEN gr.group_type = 1 THEN op.paid_amount ELSE op.paid_amount + op.group_amount END) total_amount
        FROM services s
        JOIN operation_details op ON s.id = op.service_id
        JOIN operations o ON o.id = op.operation_id
        JOIN service_categories sc ON sc.id = s.category_id
        JOIN groups gr ON op.group_id = gr.id
        WHERE o.deleted = 0
          AND op.deleted = 0
          AND o.is_operation = 1
          AND op.tab_index IN (1, 2)
          AND op.operation_date >= :startDate
          AND op.operation_date < :endDatePlusOne
          AND (op.SERVICE_PRICE_INCLUDED = 0 OR op.SERVICE_PRICE_INCLUDED IS NULL)
        GROUP BY sc.name
    )
    GROUP BY category_name
)
WHERE category_name IN (
    SELECT category_name FROM (
        SELECT category_name, SUM(total_amount) AS total_amount_sum
        FROM (
            -- Poliklinik
            SELECT CAST(sc.name AS NVARCHAR2(100)) category_name,
                   SUM(CASE WHEN gr.group_type = 1 THEN op.paid_amount ELSE op.paid_amount + op.group_amount END) total_amount
            FROM services s
            JOIN operation_details op ON s.id = op.service_id
            JOIN operations o ON o.id = op.operation_id
            JOIN service_categories sc ON sc.id = s.category_id
            JOIN groups gr ON op.group_id = gr.id
            WHERE o.deleted = 0
              AND op.deleted = 0
              AND op.status > 2
              AND op.status <> 60
              AND o.is_operation = 0
              AND o.document_date >= :startDate
              AND o.document_date < :endDatePlusOne
            GROUP BY sc.name

            UNION ALL

            -- Emeliyyat
            SELECT CAST(sc.name AS NVARCHAR2(100)) category_name,
                   SUM(CASE WHEN gr.group_type = 1 THEN op.paid_amount ELSE op.paid_amount + op.group_amount END) total_amount
            FROM services s
            JOIN operation_details op ON s.id = op.service_id
            JOIN operations o ON o.id = op.operation_id
            JOIN service_categories sc ON sc.id = s.category_id
            JOIN groups gr ON op.group_id = gr.id
            WHERE o.deleted = 0
              AND op.deleted = 0
              AND o.is_operation = 1
              AND op.tab_index IN (1, 2)
              AND op.operation_date >= :startDate
              AND op.operation_date < :endDatePlusOne
              AND (op.SERVICE_PRICE_INCLUDED = 0 OR op.SERVICE_PRICE_INCLUDED IS NULL)
            GROUP BY sc.name
        )
        GROUP BY category_name
        ORDER BY SUM(total_amount) DESC
    )
    WHERE ROWNUM <= 5
)
UNION ALL
SELECT CAST('Digərləri' AS NVARCHAR2(100)) category_name, SUM(total_amount_sum)
FROM (
    SELECT category_name, SUM(total_amount) total_amount_sum
    FROM (
        -- Poliklinik
        SELECT CAST(sc.name AS NVARCHAR2(100)) category_name,
               SUM(CASE WHEN gr.group_type = 1 THEN op.paid_amount ELSE op.paid_amount + op.group_amount END) total_amount
        FROM services s
        JOIN operation_details op ON s.id = op.service_id
        JOIN operations o ON o.id = op.operation_id
        JOIN service_categories sc ON sc.id = s.category_id
        JOIN groups gr ON op.group_id = gr.id
        WHERE o.deleted = 0
          AND op.deleted = 0
          AND op.status > 2
          AND op.status <> 60
          AND o.is_operation = 0
          AND o.document_date >= :startDate
          AND o.document_date < :endDatePlusOne
        GROUP BY sc.name

        UNION ALL

        -- Emeliyyat
        SELECT CAST(sc.name AS NVARCHAR2(100)) category_name,
               SUM(CASE WHEN gr.group_type = 1 THEN op.paid_amount ELSE op.paid_amount + op.group_amount END) total_amount
        FROM services s
        JOIN operation_details op ON s.id = op.service_id
        JOIN operations o ON o.id = op.operation_id
        JOIN service_categories sc ON sc.id = s.category_id
        JOIN groups gr ON op.group_id = gr.id
        WHERE o.deleted = 0
          AND op.deleted = 0
          AND o.is_operation = 1
          AND op.tab_index IN (1, 2)
          AND op.operation_date >= :startDate
          AND op.operation_date < :endDatePlusOne
          AND (op.SERVICE_PRICE_INCLUDED = 0 OR op.SERVICE_PRICE_INCLUDED IS NULL)
        GROUP BY sc.name
    )
    GROUP BY category_name
    HAVING category_name NOT IN (
        SELECT category_name FROM (
            SELECT category_name
            FROM (
                SELECT CAST(sc.name AS NVARCHAR2(100)) category_name,
                       SUM(CASE WHEN gr.group_type = 1 THEN op.paid_amount ELSE op.paid_amount + op.group_amount END) total_amount
                FROM services s
                JOIN operation_details op ON s.id = op.service_id
                JOIN operations o ON o.id = op.operation_id
                JOIN service_categories sc ON sc.id = s.category_id
                JOIN groups gr ON op.group_id = gr.id
                WHERE o.deleted = 0
                  AND op.deleted = 0
                  AND op.status > 2
                  AND op.status <> 60
                  AND o.is_operation = 0
                  AND o.document_date >= :startDate
                  AND o.document_date < :endDatePlusOne
                GROUP BY sc.name

                UNION ALL

                SELECT CAST(sc.name AS NVARCHAR2(100)) category_name,
                       SUM(CASE WHEN gr.group_type = 1 THEN op.paid_amount ELSE op.paid_amount + op.group_amount END) total_amount
                FROM services s
                JOIN operation_details op ON s.id = op.service_id
                JOIN operations o ON o.id = op.operation_id
                JOIN service_categories sc ON sc.id = s.category_id
                JOIN groups gr ON op.group_id = gr.id
                WHERE o.deleted = 0
                  AND op.deleted = 0
                  AND o.is_operation = 1
                  AND op.tab_index IN (1, 2)
                  AND op.operation_date >= :startDate
                  AND op.operation_date < :endDatePlusOne
                  AND (op.SERVICE_PRICE_INCLUDED = 0 OR op.SERVICE_PRICE_INCLUDED IS NULL)
                GROUP BY sc.name
            )
            GROUP BY category_name
            ORDER BY SUM(total_amount) DESC
        )
        WHERE ROWNUM <= 5
    )
)
ORDER BY total_amount_sum DESC
";

            var endDatePlusOne = endDate.AddDays(1);

            var Teskilatuzre = ExecuteQuery(TeskilatUzreSql, reader => new Teskilat_Uzre
            {
                Icbari_Sigorta = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0)),
                Diger_Sigorta = reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetValue(1)),
                Endirimler = reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader.GetValue(2)),
                Oz_Hesabina = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader.GetValue(3)),
            }, startDate, endDate, endDatePlusOne );

            var XidmetTipi = ExecuteQuery(XidmetTipiSql, reader =>
            {
                decimal emeliyyat = 0;
                decimal poliklinik = 0;


                do
                {
                    var serviceType = reader.GetString(0);
                    var totalPrice = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);

                    if (serviceType == "Emeliyyat")
                        emeliyyat = totalPrice;
                    else if (serviceType == "Poliklinik")
                        poliklinik = totalPrice;

                } while (reader.Read());

                return new Xidmet_Tipi_Uzre
                {
                    Emeliyyat = emeliyyat,
                    Poliklinik = poliklinik
                };
            }, startDate, endDate, endDatePlusOne);

            var xidmetCategoryList = ExecuteQueryList(
                Xidmet_CategoryasiSql,
                reader => new Xidmet_Categoryasi
                {
                    CategoryName = reader["category_name"].ToString(),
                    TotalPrice = reader.GetDecimal(reader.GetOrdinal("total_amount_sum"))
                },
                startDate,
                endDate
            );



            return new Dovriyye_Statistikasi
            {
                TeskilatUzre = Teskilatuzre ?? new Teskilat_Uzre(),
                XidmetTipi = XidmetTipi ?? new Xidmet_Tipi_Uzre(),
                Xidmet_Categoryasi = xidmetCategoryList,
            };

        }

        private T ExecuteQuery<T>(
            string sql,
            Func<IDataReader, T> mapFunc,
            DateTime? startDate = null,
            DateTime? endDate = null,
            DateTime? endDatePlusOne = null
            )
        {
            var connectionstring = _configuration.GetConnectionString("SqlConnection");

            using (var conn = new OracleConnection(connectionstring))
            {
                conn.Open();

                using (var cmd = new OracleCommand(sql, conn))
                {
                    if (sql.Contains(":startDate") && startDate.HasValue)
                        cmd.Parameters.Add(new OracleParameter("startDate", OracleDbType.Date)).Value = startDate.Value;

                    if (!sql.Contains(":endDatePlusOne")&&sql.Contains(":endDate") && endDate.HasValue)
                        cmd.Parameters.Add(new OracleParameter("endDate", OracleDbType.Date)).Value = endDate.Value;

                    if (sql.Contains(":endDatePlusOne") && endDatePlusOne.HasValue)
                        cmd.Parameters.Add(new OracleParameter("endDatePlusOne", OracleDbType.Date)).Value = endDatePlusOne.Value;


                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        return mapFunc(reader);
                    }

                    return default;
                }
            }
        }


        private List<T> ExecuteQueryList<T>(
                                            string sql,
                                            Func<IDataReader, T> mapFunc,
                                            DateTime? startDate = null,
                                            DateTime? endDate = null)
        {
            var connectionString = _configuration.GetConnectionString("SqlConnection");

            using var conn = new OracleConnection(connectionString);
            conn.Open();

            using var cmd = new OracleCommand(sql, conn);

            if (startDate.HasValue)
                cmd.Parameters.Add(new OracleParameter("startDate", OracleDbType.Date)).Value = startDate.Value;

            if (endDate.HasValue)
                cmd.Parameters.Add(new OracleParameter("endDate", OracleDbType.Date)).Value = endDate.Value;

            var result = new List<T>();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                result.Add(mapFunc(reader));
            }

            return result;
        }

        private Mal_Material_Hereketleri GetMalMaterialBalance(DateTime startDate, DateTime endDate, int? DermanID, int? SerfiyyayID)
        {

            var IlinkQaliqSql = String.Format(@"
    WITH DP AS (
        SELECT ID, NAME
        FROM DEPARTMENTS
        WHERE DELETED = 0 
        CONNECT BY PRIOR ID = PARENT_ID
        START WITH id = {2}
    )
    SELECT
        ProductGroupType,
        SUM(TotalAmount) AS TotalAmount,
        SUM(TotalQuantity) AS TotalQuantity
    FROM (
        SELECT
            CASE 
                WHEN G.ID = {0} THEN 'Derman'
                WHEN G.ID = {1} THEN 'Serfiyyat'
                ELSE 'Other'
            END AS ProductGroupType,
            NVL(SUM(GD.BOX_QUANTITY * P.QUANTITY_IN_BOX * GD.SUPPLIER_PRICE + GD.QUANTITY * GD.SUPPLIER_PRICE), 0) AS TotalAmount,
            NVL(SUM(GD.BOX_QUANTITY * P.QUANTITY_IN_BOX + GD.QUANTITY), 0) AS TotalQuantity
        FROM GOODS_MOTION GM
        INNER JOIN GOODS_DETAILS GD ON GM.ID = GD.GOODS_MOTION_ID
        INNER JOIN PRODUCTS P ON GD.PRODUCT_ID = P.ID
        INNER JOIN PRODUCT_GROUPS G ON P.PRODUCT_GROUP_ID = G.ID
        INNER JOIN DP ON GM.TO_ID = DP.ID
        WHERE GM.DELETED = 0
          AND GD.DELETED = 0
          AND GM.STATUS = 3
          AND GM.INVOICE_DATE < :startDate
          AND GM.DOCUMENT_TYPE_CODE = 1
          AND G.ID IN ({0}, {1})
        GROUP BY G.ID

        UNION ALL

        SELECT
            CASE 
                WHEN G.ID = {0} THEN 'Derman'
                WHEN G.ID = {1} THEN 'Serfiyyat'
                ELSE 'Other'
            END AS ProductGroupType,
            NVL(SUM(-1 * (GD.BOX_QUANTITY * P.QUANTITY_IN_BOX * INCOME_GD.SUPPLIER_PRICE + GD.QUANTITY * INCOME_GD.SUPPLIER_PRICE)), 0) AS TotalAmount,
            NVL(SUM(-1 * (GD.BOX_QUANTITY * P.QUANTITY_IN_BOX + GD.QUANTITY)), 0) AS TotalQuantity
        FROM GOODS_MOTION GM
        INNER JOIN GOODS_DETAILS GD ON GM.ID = GD.GOODS_MOTION_ID
        INNER JOIN GOODS_DETAILS INCOME_GD ON GD.INCOME_GOODS_DETAIL_ID = INCOME_GD.ID
        INNER JOIN PRODUCTS P ON GD.PRODUCT_ID = P.ID
        INNER JOIN PRODUCT_GROUPS G ON P.PRODUCT_GROUP_ID = G.ID
        INNER JOIN DP ON GM.FROM_ID = DP.ID
        WHERE GM.DELETED = 0
          AND GD.DELETED = 0
          AND GM.STATUS = 3
          AND GM.INVOICE_DATE < :startDate
          AND GM.DOCUMENT_TYPE_CODE IN (3, 19)
          AND G.ID IN ({0}, {1})
        GROUP BY G.ID

        UNION ALL

        SELECT
            CASE
                WHEN G.ID = {0} THEN 'Derman'
                WHEN G.ID = {1} THEN 'Serfiyyat'
                ELSE 'Other'
            END AS ProductGroupType,
            NVL(SUM(-1 * (UP.BOX_QUANTITY * INCOME_GD.SUPPLIER_PRICE * P.QUANTITY_IN_BOX + UP.QUANTITY * INCOME_GD.SUPPLIER_PRICE)), 0) AS TotalAmount,
            NVL(SUM(-1 * (UP.BOX_QUANTITY * P.QUANTITY_IN_BOX + UP.QUANTITY)), 0) AS TotalQuantity
        FROM USED_PRODUCTS UP
        INNER JOIN GOODS_DETAILS INCOME_GD ON UP.INCOME_GOODS_DETAIL_ID = INCOME_GD.ID
        INNER JOIN PRODUCTS P ON UP.PRODUCT_ID = P.ID
        INNER JOIN PRODUCT_GROUPS G ON P.PRODUCT_GROUP_ID = G.ID
        INNER JOIN DP ON UP.DEPARTMENT_ID = DP.ID
        WHERE UP.DELETED = 0
          AND UP.USE_DATE < :startDate
          AND G.ID IN ({0}, {1})
        GROUP BY G.ID
    ) t
    GROUP BY ProductGroupType
", _DermanID, _SerfiyyatID, _DepartmentID);

            var MedaxilSql = String.Format(@"SELECT
                                    ProductGroupType,
                                    SUM(TotalAmount) AS TotalAmount
                                FROM (
                                    SELECT
                                        CASE 
                                            WHEN G.ID = {0} THEN 'Derman'
                                            WHEN G.ID = {1} THEN 'Serfiyyat'
                                            ELSE 'Other'
                                        END AS ProductGroupType,
                                        NVL(SUM(GD.BOX_QUANTITY * GD.BOX_PRICE + GD.QUANTITY * GD.SUPPLIER_PRICE), 0) AS TotalAmount
                                    FROM GOODS_MOTION GM
                                    INNER JOIN GOODS_DETAILS GD ON GM.ID = GD.GOODS_MOTION_ID
                                    INNER JOIN PRODUCTS P ON GD.PRODUCT_ID = P.ID
                                    INNER JOIN PRODUCT_GROUPS G ON P.PRODUCT_GROUP_ID = G.ID
                                    WHERE GM.DELETED = 0
                                      AND GM.STATUS = 3
                                      AND GD.DELETED = 0
                                      AND GM.DOCUMENT_TYPE_CODE = 1
                                      AND GM.INVOICE_DATE BETWEEN :startDate AND :endDate
                                      AND G.ID IN ({0}, {1})
                                    GROUP BY G.ID
                                ) t
                                GROUP BY ProductGroupType", _DermanID, _SerfiyyatID);

            var MexaricSql = String.Format(@"
                                            SELECT
                                                ProductGroupType,
                                                NVL(SUM(TotalAmount), 0) AS TotalAmount
                                            FROM (
                                                SELECT
                                                    CASE 
                                                        WHEN G.ID = {0} THEN 'Derman'
                                                        WHEN G.ID = {1} THEN 'Serfiyyat'
                                                        ELSE 'Other'
                                                    END AS ProductGroupType,
                                                    (GD.BOX_QUANTITY * income_gd.BOX_PRICE + GD.QUANTITY * income_gd.SUPPLIER_PRICE) AS TotalAmount
                                                FROM GOODS_MOTION GM
                                                INNER JOIN GOODS_DETAILS GD ON GM.ID = GD.GOODS_MOTION_ID
                                                INNER JOIN GOODS_DETAILS income_GD ON GD.income_goods_detail_id = income_gd.id
                                                INNER JOIN PRODUCTS P ON GD.PRODUCT_ID = P.ID
                                                INNER JOIN PRODUCT_GROUPS G ON P.PRODUCT_GROUP_ID = G.ID
                                                WHERE GM.DELETED = 0
                                                  AND GM.STATUS = 3
                                                  AND GD.DELETED = 0
                                                  AND GM.DOCUMENT_TYPE_CODE in (19)
                                                  AND GM.INVOICE_DATE BETWEEN :startDate AND :endDate
                                                  AND G.ID IN ({0}, {1})
                                            ) t
                                            GROUP BY ProductGroupType", _DermanID, _SerfiyyatID);


            var SilinmeSql = String.Format(@"SELECT
                        ProductGroupType,
                        SUM(TotalAmount) AS TotalAmount
                    FROM (
                        SELECT
                            CASE 
                                WHEN G.ID = {0} THEN 'Derman'
                                WHEN G.ID = {1} THEN 'Serfiyyat'
                                ELSE 'Other'
                            END AS ProductGroupType,
                            NVL(SUM(GD.BOX_QUANTITY * income_gd.BOX_PRICE + GD.QUANTITY * income_gd.SUPPLIER_PRICE), 0) AS TotalAmount
                        FROM GOODS_MOTION GM
                        INNER JOIN GOODS_DETAILS GD ON GM.ID = GD.GOODS_MOTION_ID
                        INNER JOIN GOODS_DETAILS income_GD ON GD.income_goods_detail_id = income_gd.id
                        INNER JOIN PRODUCTS P ON GD.PRODUCT_ID = P.ID
                        INNER JOIN PRODUCT_GROUPS G ON P.PRODUCT_GROUP_ID = G.ID
                        WHERE GM.DELETED = 0
                          AND GM.STATUS = 3
                          AND GD.DELETED = 0
                          AND GM.DOCUMENT_TYPE_CODE IN (3)
                          AND GM.INVOICE_DATE BETWEEN :startDate AND :endDate
                          AND G.ID IN ({0}, {1})
                        GROUP BY G.ID
                    ) t
                    GROUP BY ProductGroupType", _DermanID, _SerfiyyatID);


            var SonQaliqSql = String.Format(@"
    WITH DP AS (
        SELECT ID, NAME
        FROM DEPARTMENTS
        WHERE DELETED = 0 
        CONNECT BY PRIOR ID = PARENT_ID
        START WITH id = {2}
    )
    SELECT
        ProductGroupType,
        SUM(TotalAmount) AS TotalAmount,
        SUM(TotalQuantity) AS TotalQuantity
    FROM (
        SELECT
            CASE 
                WHEN G.ID = {0} THEN 'Derman'
                WHEN G.ID = {1} THEN 'Serfiyyat'
                ELSE 'Other'
            END AS ProductGroupType,
            NVL(SUM(GD.BOX_QUANTITY * P.QUANTITY_IN_BOX * GD.SUPPLIER_PRICE + GD.QUANTITY * GD.SUPPLIER_PRICE), 0) AS TotalAmount,
            NVL(SUM(GD.BOX_QUANTITY * P.QUANTITY_IN_BOX + GD.QUANTITY), 0) AS TotalQuantity
        FROM GOODS_MOTION GM
        INNER JOIN GOODS_DETAILS GD ON GM.ID = GD.GOODS_MOTION_ID
        INNER JOIN PRODUCTS P ON GD.PRODUCT_ID = P.ID
        INNER JOIN PRODUCT_GROUPS G ON P.PRODUCT_GROUP_ID = G.ID
        INNER JOIN DP ON GM.TO_ID = DP.ID
        WHERE GM.DELETED = 0
          AND GD.DELETED = 0
          AND GM.STATUS = 3
          AND GM.INVOICE_DATE < :endDatePlusOne
          AND GM.DOCUMENT_TYPE_CODE = 1
          AND G.ID IN ({0}, {1})
        GROUP BY G.ID

        UNION ALL

        SELECT
            CASE 
                WHEN G.ID = {0} THEN 'Derman'
                WHEN G.ID = {1} THEN 'Serfiyyat'
                ELSE 'Other'
            END AS ProductGroupType,
            NVL(SUM(-1 * (GD.BOX_QUANTITY * P.QUANTITY_IN_BOX * INCOME_GD.SUPPLIER_PRICE + GD.QUANTITY * INCOME_GD.SUPPLIER_PRICE)), 0) AS TotalAmount,
            NVL(SUM(-1 * (GD.BOX_QUANTITY * P.QUANTITY_IN_BOX + GD.QUANTITY)), 0) AS TotalQuantity
        FROM GOODS_MOTION GM
        INNER JOIN GOODS_DETAILS GD ON GM.ID = GD.GOODS_MOTION_ID
        INNER JOIN GOODS_DETAILS INCOME_GD ON GD.INCOME_GOODS_DETAIL_ID = INCOME_GD.ID
        INNER JOIN PRODUCTS P ON GD.PRODUCT_ID = P.ID
        INNER JOIN PRODUCT_GROUPS G ON P.PRODUCT_GROUP_ID = G.ID
        INNER JOIN DP ON GM.FROM_ID = DP.ID
        WHERE GM.DELETED = 0
          AND GD.DELETED = 0
          AND GM.STATUS = 3
          AND GM.INVOICE_DATE < :endDatePlusOne
          AND GM.DOCUMENT_TYPE_CODE IN (3, 19)
          AND G.ID IN ({0}, {1})
        GROUP BY G.ID

        UNION ALL

        SELECT
            CASE
                WHEN G.ID = {0} THEN 'Derman'
                WHEN G.ID = {1} THEN 'Serfiyyat'
                ELSE 'Other'
            END AS ProductGroupType,
            NVL(SUM(-1 * (UP.BOX_QUANTITY * INCOME_GD.SUPPLIER_PRICE * P.QUANTITY_IN_BOX + UP.QUANTITY * INCOME_GD.SUPPLIER_PRICE)), 0) AS TotalAmount,
            NVL(SUM(-1 * (UP.BOX_QUANTITY * P.QUANTITY_IN_BOX + UP.QUANTITY)), 0) AS TotalQuantity
        FROM USED_PRODUCTS UP
        INNER JOIN GOODS_DETAILS INCOME_GD ON UP.INCOME_GOODS_DETAIL_ID = INCOME_GD.ID
        INNER JOIN PRODUCTS P ON UP.PRODUCT_ID = P.ID
        INNER JOIN PRODUCT_GROUPS G ON P.PRODUCT_GROUP_ID = G.ID
        INNER JOIN DP ON UP.DEPARTMENT_ID = DP.ID
        WHERE UP.DELETED = 0
          AND UP.USE_DATE < :endDatePlusOne
          AND G.ID IN ({0}, {1})
        GROUP BY G.ID
    ) t
    GROUP BY ProductGroupType
", _DermanID, _SerfiyyatID, _DepartmentID);

            var endDatePlusOne = endDate.AddDays(1);

            var IlkinQaliq = ExecuteMaterialQuery(IlinkQaliqSql, startDate: startDate);
            var Medaxil = ExecuteMaterialQuery(MedaxilSql, startDate: startDate, endDatePlusOne: endDatePlusOne);
            var Mexaric = ExecuteMaterialQuery(MexaricSql, startDate: startDate, endDatePlusOne: endDatePlusOne);
            var Silinme = ExecuteMaterialQuery(SilinmeSql, startDate: startDate, endDatePlusOne: endDatePlusOne);
            var SonQaliq = ExecuteMaterialQuery(SonQaliqSql, endDatePlusOne: endDatePlusOne);

            return new Mal_Material_Hereketleri
            {
                IlkinQaliq = IlkinQaliq ?? new Mal_Material(),
                Medaxil = Medaxil ?? new Mal_Material(),
                Mexaric = Mexaric ?? new Mal_Material(),
                Silinme = Silinme ?? new Mal_Material(),
                SonQaliq = SonQaliq ?? new Mal_Material(),
            };
        }   

        private Mal_Material ExecuteMaterialQuery(string sql, DateTime? startDate = null, DateTime? endDate = null, DateTime? endDatePlusOne = null, int? dermanID = null, int? serfiyyatID = null)
        {
            var connectionString = _configuration.GetConnectionString("SqlConnection");

            using var conn = new OracleConnection(connectionString);
            conn.Open();

            using var cmd = new OracleCommand(sql, conn);

            if (startDate.HasValue)
                cmd.Parameters.Add(new OracleParameter("startDate", OracleDbType.Date)).Value = startDate.Value.Date;

            if (endDate.HasValue)
                cmd.Parameters.Add(new OracleParameter("endDate", OracleDbType.Date)).Value = endDate.Value.Date;

            if (endDatePlusOne.HasValue)
                cmd.Parameters.Add(new OracleParameter("endDatePlusOne", OracleDbType.Date)).Value = endDatePlusOne.Value.Date;

            //if (dermanID.HasValue)
            //    cmd.Parameters.Add(new OracleParameter("DermanID", OracleDbType.Int32)).Value = dermanID.Value;

            //if (serfiyyatID.HasValue)
            //    cmd.Parameters.Add(new OracleParameter("SerfiyyatID", OracleDbType.Int32)).Value = serfiyyatID.Value;

            using var reader = cmd.ExecuteReader();

            var result = new Mal_Material();

            while (reader.Read())
            {
                string type = reader["ProductGroupType"]?.ToString()?.Trim();
                decimal value = reader["TotalAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TotalAmount"]);

                if (!string.IsNullOrEmpty(type))
                {
                    if (type.Contains("Derman", StringComparison.OrdinalIgnoreCase))
                        result.Derman = value;
                    else if (type.Contains("Serfiyyat", StringComparison.OrdinalIgnoreCase))
                        result.Serfiyyat = value;
                    else if (type.Contains("Other", StringComparison.OrdinalIgnoreCase))
                        result.Digerleri = value;
                }
            }

            return result;
        }



    }

}
