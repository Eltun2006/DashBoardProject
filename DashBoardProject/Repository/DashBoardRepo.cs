using DashBoardProject.Models;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace DashBoardProject.Repository
{
    public class DashBoardRepo
    {
        private readonly IConfiguration _configuration;

        public DashBoardRepo(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public FullDashBoardModel FullDashBoardMetod(DateTime? startDate = null, DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.Today.AddYears(-5);
            var end = endDate ?? DateTime.Today;

            var balances = GetAccountBalance(start, end);
            var dovriyye = GetDovriyyeBalance(start, end);
            var material = GetMalMaterialBalance(start, end);

            return new FullDashBoardModel
            {
                MalMulk = balances,
                Dovriyye = dovriyye,
                MalMaterialHereketleri = material
            };
        }


        private AccountBalance GetAccountBalance(DateTime startDate, DateTime endDate)
        {

            var IlkinQaliqSql = @"SELECT 
                                    SUM(CASE WHEN account_type = 1146 THEN Initial_Balance END) AS bank,
                                    SUM(CASE WHEN account_type = 1147 THEN Initial_Balance END) AS kassa
                                FROM (
                                    SELECT 
                                        pa.Account_Type,
                                        COALESCE(SUM(CASE 
                                            WHEN p.Credit_Id = pa.Id AND p.Payment_Date < :startDate THEN -p.Amount
                                            WHEN p.Debit_Id  = pa.Id AND p.Payment_Date < :startDate THEN  p.Amount
                                            ELSE 0
                                        END), 0) AS Initial_Balance
                                    FROM Payment_Accounts pa
                                    INNER JOIN Payments p ON pa.Id = p.Credit_Id
                                    GROUP BY pa.Account_Type

                                    UNION ALL

                                    SELECT 
                                        pa.Account_Type,
                                        COALESCE(SUM(CASE 
                                            WHEN p.Credit_Id = pa.Id AND p.Payment_Date < :startDate THEN -p.Amount
                                            WHEN p.Debit_Id  = pa.Id AND p.Payment_Date < :startDate THEN  p.Amount
                                            ELSE 0
                                        END), 0) AS Initial_Balance
                                    FROM Payment_Accounts pa
                                    INNER JOIN Payments p ON pa.Id = p.Debit_Id
                                    GROUP BY pa.Account_Type
                                )";


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
                                        AND p.Payment_Date BETWEEN :startDate AND :endDate
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
                                    AND p.Payment_Date BETWEEN :startDate AND :endDate
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
                                    WHERE p.payment_date >= :endDatePlusOne
                                    GROUP BY pa.account_type

                                    UNION ALL

                                    SELECT 
                                        pa.account_type, 
                                        -SUM(p.amount) AS amount
                                    FROM payments p
                                    INNER JOIN payment_accounts pa ON p.credit_id = pa.id
                                    WHERE p.payment_date >= :endDatePlusOne
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

        private Dovriyye_Statistikasi GetDovriyyeBalance(DateTime startDate, DateTime endDate)
        {
            var TeskilatUzreSql = @"SELECT 
                                        SUM(CASE WHEN Qrup = 'Icbari_Sigorta' THEN Pasiyent_Sayi ELSE 0 END) AS Icbari_Sigorta,
                                        SUM(CASE WHEN Qrup = 'Diger_Sigortalar' THEN Pasiyent_Sayi ELSE 0 END) AS Diger_Sigorta,
                                        SUM(CASE WHEN Qrup = 'Endirimler' THEN Pasiyent_Sayi ELSE 0 END) AS Endirimler,
                                        SUM(CASE WHEN Qrup = 'Oz_Hesabina' THEN Pasiyent_Sayi ELSE 0 END) AS Oz_Hesabina
                                    FROM (
                                        SELECT Qrup, COUNT(DISTINCT pasiyent_id) AS Pasiyent_Sayi
                                        FROM (
                                            SELECT 
                                                CASE 
                                                    WHEN g.group_type = 2 AND g.id = 278680 THEN 'Icbari_Sigorta'
                                                    WHEN g.group_type = 2 AND g.id <> 278680 THEN 'Diger_Sigortalar'
                                                    WHEN g.group_type = 3 THEN 'Endirimler'
                                                    ELSE 'Oz_Hesabina'
                                                END AS Qrup,
                                                p.id AS pasiyent_id
                                            FROM patients p
                                            JOIN operations o 
                                                ON p.id = o.patient_id 
                                               AND o.deleted = 0
                                               AND o.is_operation = 1  
                                            JOIN operation_details od 
                                                ON od.operation_id = o.id 
                                               AND od.status > 2 
                                               AND od.status <> 60 
                                               AND od.deleted = 0
                                               AND o.document_date >= :startDate
                                               AND o.document_date < :endDatePlusOne
                                            JOIN groups g 
                                                ON od.group_id = g.id

                                            UNION ALL

                                            SELECT 
                                                CASE 
                                                    WHEN g.group_type = 2 AND g.id = 278680 THEN 'Icbari_Sigorta'
                                                    WHEN g.group_type = 2 AND g.id <> 278680 THEN 'Diger_Sigortalar'
                                                    WHEN g.group_type = 3 THEN 'Endirimler'
                                                    ELSE 'Oz_Hesabina'
                                                END AS Qrup,
                                                p.id AS pasiyent_id
                                            FROM patients p
                                            JOIN operations o 
                                                ON p.id = o.patient_id 
                                               AND o.deleted = 0
                                               AND o.is_operation = 0 
                                            JOIN operation_details od 
                                                ON od.operation_id = o.id 
                                               AND od.status > 2 
                                               AND od.status <> 60 
                                               AND od.deleted = 0
                                               AND o.document_date >= :startDate
                                               AND o.document_date < :endDatePlusOne
                                            JOIN groups g 
                                                ON od.group_id = g.id
                                           
                                        ) sub
                                        GROUP BY Qrup
                                    ) final";

            var XidmetTipiSql = @"SELECT 
                                  CASE 
                                    WHEN o.is_operation = 1 THEN 'Emeliyyat'
                                    WHEN o.is_operation = 0 THEN 'Poliklinik'
                                    ELSE 'Digər'
                                  END AS service_type,
                                  SUM(od.paid_amount) AS total_price
                                FROM operations o
                                JOIN operation_details od ON od.operation_id = o.id
                                Where O.DOCUMENT_DATE BETWEEN :startDate AND :endDate
                                GROUP BY o.is_operation ";

            var Xidmet_CategoryasiSql = @"SELECT * FROM (
                                                    SELECT 
                                                        sc.name AS category_name,
                                                        SUM(op.paid_amount + CASE 
                                                            WHEN gr.group_type = 1 THEN 0 
                                                            ELSE op.group_amount 
                                                        END) AS total_amount
                                                    FROM services s
                                                    INNER JOIN operation_details op ON s.id = op.service_id
                                                    INNER JOIN operations o ON o.id = op.operation_id 
                                                    INNER JOIN service_categories sc ON sc.id = s.category_id
                                                    INNER JOIN groups gr ON op.group_id = gr.id
                                                    WHERE o.deleted = 0 
                                                      AND op.deleted = 0
                                                      AND op.status > 2 
                                                      AND op.status <> 60
                                                      AND o.document_date >= :startDate
                                                      AND o.document_date < :endDate
                                                    GROUP BY sc.name
                                                    ORDER BY total_amount DESC
                                                )
                                                WHERE ROWNUM <= 5";

            var endDatePlusOne = endDate.AddDays(1);

            var Teskilatuzre = ExecuteQuery(TeskilatUzreSql, reader => new Teskilat_Uzre
            {
                Icbari_Sigorta = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0)),
                Diger_Sigorta = reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetValue(1)),
                Endirimler = reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader.GetValue(2)),
                Oz_Hesabina = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader.GetValue(3)),
            }, startDate, endDate, endDatePlusOne);

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
                    TotalPrice = reader.GetDecimal(reader.GetOrdinal("total_amount"))
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
        private T ExecuteQuery<T>(string sql, Func<IDataReader, T> mapFunc, DateTime? startDate = null, DateTime? endDate = null, DateTime? endDatePlusOne = null)
        {
            var connectionstring = _configuration.GetConnectionString("SqlConnection");

            using var conn = new OracleConnection(connectionstring);
            conn.Open();

            using var cmd = new OracleCommand(sql, conn);

            if (startDate.HasValue)
                cmd.Parameters.Add(new OracleParameter("startDate", OracleDbType.Date)).Value = startDate.Value;
            //if (endDate.HasValue)
            //    cmd.Parameters.Add(new OracleParameter("endDate", OracleDbType.Date)).Value = endDate.Value;
            if (endDatePlusOne.HasValue)
                cmd.Parameters.Add(new OracleParameter("endDatePlusOne", OracleDbType.Date)).Value = endDatePlusOne.Value;

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return mapFunc(reader);
            }

            return default;
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

        private Mal_Material_Hereketleri GetMalMaterialBalance(DateTime startDate, DateTime endDate)
        {
            var IlinkQaliqSql = @"SELECT
                                    CASE 
                                        WHEN G.ID = 101 THEN 'Inventory'
                                        WHEN G.ID = 81 THEN 'Derman'
                                        WHEN G.ID = 595 THEN 'Teserrufat'
                                        ELSE 'Other'
                                    END AS ProductGroupType,
                                    NVL(SUM(
                                        CASE
                                            WHEN GM.DOCUMENT_TYPE_CODE = 1 THEN GD.BOX_QUANTITY * GD.BOX_PRICE + GD.QUANTITY * GD.SUPPLIER_PRICE
                                            WHEN GM.DOCUMENT_TYPE_CODE IN (3, 19) THEN -(GD.BOX_QUANTITY * GD.BOX_PRICE + GD.QUANTITY * GD.SUPPLIER_PRICE)
                                            ELSE 0
                                        END
                                    ), 0) AS TotalAmount
                                FROM GOODS_MOTION GM
                                INNER JOIN GOODS_DETAILS GD ON GM.ID = GD.GOODS_MOTION_ID
                                INNER JOIN PRODUCTS P ON GD.PRODUCT_ID = P.ID
                                INNER JOIN PRODUCT_GROUPS G ON P.PRODUCT_GROUP_ID = G.ID
                                WHERE GM.DELETED = 0
                                  AND GD.DELETED = 0
                                  AND GM.STATUS = 3
                                  AND GM.INVOICE_DATE < :startDate
                                  AND G.ID IN (101, 81, 595)
                                GROUP BY G.ID
                                ";

            var MedaxilSql = @"SELECT
                                    CASE 
                                        WHEN G.ID = 101 THEN 'Inventory'
                                        WHEN G.ID = 81 THEN 'Derman'
                                        WHEN G.ID = 595 THEN 'Teserrufat'
                                        ELSE 'Other'
                                    END AS ProductGroupType,
                                    NVL(SUM(GD.BOX_QUANTITY * GD.BOX_PRICE + GD.QUANTITY * GD.SUPPLIER_PRICE), 0) AS TotalAmount
                                FROM GOODS_MOTION GM
                                INNER JOIN GOODS_DETAILS GD ON GM.ID = GD.GOODS_MOTION_ID
                                INNER JOIN PRODUCTS P ON GD.PRODUCT_ID = P.ID
                                INNER JOIN PRODUCT_GROUPS G ON P.PRODUCT_GROUP_ID = G.ID
                                WHERE GM.DELETED = 0
                                  AND GM.STATUS = 3
                                  AND GM.DOCUMENT_TYPE_CODE = 1
                                  AND GD.DELETED = 0
                                  AND GM.INVOICE_DATE BETWEEN :startDate AND :endDate
                                  AND G.ID IN (101, 81, 595)
                                GROUP BY G.ID
                                ";

            var MexaricSql = @"SELECT
                                CASE 
                                    WHEN G.ID = 101 THEN 'Inventory'
                                    WHEN G.ID = 81 THEN 'Derman'
                                    WHEN G.ID = 595 THEN 'Teserrufat'
                                    ELSE 'Other'
                                END AS ProductGroupType,
                                NVL(SUM(GD.BOX_QUANTITY * GD.BOX_PRICE + GD.QUANTITY * GD.SUPPLIER_PRICE), 0) AS TotalAmount
                            FROM GOODS_MOTION GM
                            INNER JOIN GOODS_DETAILS GD ON GM.ID = GD.GOODS_MOTION_ID
                            INNER JOIN PRODUCTS P ON GD.PRODUCT_ID = P.ID
                            INNER JOIN PRODUCT_GROUPS G ON P.PRODUCT_GROUP_ID = G.ID
                            WHERE GM.DELETED = 0
                              AND GM.STATUS = 3
                              AND GM.DOCUMENT_TYPE_CODE IN (3, 19)
                              AND GD.DELETED = 0
                              AND GM.INVOICE_DATE BETWEEN :startDate AND :endDate
                              AND G.ID IN (101, 81, 595)
                            GROUP BY G.ID
                            ";

            var SilinmeSql = @"SELECT
                                    CASE 
                                        WHEN G.ID = 101 THEN 'Inventory'
                                        WHEN G.ID = 81 THEN 'Derman'
                                        WHEN G.ID = 595 THEN 'Teserrufat'
                                        ELSE 'Other'
                                    END AS ProductGroupType,
                                    NVL(SUM(GD.BOX_QUANTITY * GD.BOX_PRICE + GD.QUANTITY * GD.SUPPLIER_PRICE), 0) AS TotalAmount
                                FROM GOODS_MOTION GM
                                INNER JOIN GOODS_DETAILS GD ON GM.ID = GD.GOODS_MOTION_ID
                                INNER JOIN PRODUCTS P ON GD.PRODUCT_ID = P.ID
                                INNER JOIN PRODUCT_GROUPS G ON P.PRODUCT_GROUP_ID = G.ID
                                WHERE GM.DELETED = 0
                                  AND GM.STATUS = 3
                                  AND GM.DOCUMENT_TYPE_CODE = 3
                                  AND GD.DELETED = 0
                                  AND GM.INVOICE_DATE BETWEEN :startDate AND :endDate
                                  AND G.ID IN (101, 81, 595)
                                GROUP BY G.ID
                                ";

            var SonQaliqSql = @"SELECT
                                    CASE 
                                        WHEN G.ID = 101 THEN 'Inventory'
                                        WHEN G.ID = 81 THEN 'Derman'
                                        WHEN G.ID = 595 THEN 'Teserrufat'
                                        ELSE 'Other'
                                    END AS ProductGroupType,
                                    NVL(SUM(
                                        CASE
                                            WHEN GM.DOCUMENT_TYPE_CODE = 1 THEN GD.BOX_QUANTITY * GD.BOX_PRICE + GD.QUANTITY * GD.SUPPLIER_PRICE
                                            WHEN GM.DOCUMENT_TYPE_CODE IN (3, 19) THEN -(GD.BOX_QUANTITY * GD.BOX_PRICE + GD.QUANTITY * GD.SUPPLIER_PRICE)
                                            ELSE 0
                                        END
                                    ), 0) AS TotalAmount
                                FROM GOODS_MOTION GM
                                INNER JOIN GOODS_DETAILS GD ON GM.ID = GD.GOODS_MOTION_ID
                                INNER JOIN PRODUCTS P ON GD.PRODUCT_ID = P.ID
                                INNER JOIN PRODUCT_GROUPS G ON P.PRODUCT_GROUP_ID = G.ID
                                WHERE GM.DELETED = 0
                                  AND GD.DELETED = 0
                                  AND GM.STATUS = 3
                                  AND GM.INVOICE_DATE < :endDatePlusOne
                                  AND G.ID IN (101, 81, 595)
                                GROUP BY G.ID
                                ";

            var endDatePlusOne = endDate.AddDays(1);

            var IlkinQaliq = ExecuteMaterialQuery(IlinkQaliqSql, startDate: startDate);
            var Medaxil = ExecuteMaterialQuery(MedaxilSql, startDate: startDate, endDatePlusOne: endDatePlusOne);
            var Mexaric = ExecuteMaterialQuery(MexaricSql, startDate: startDate, endDatePlusOne: endDatePlusOne);
            var Silinme = ExecuteMaterialQuery(SilinmeSql, startDate: startDate,endDatePlusOne: endDatePlusOne);
            var SonQaliq = ExecuteMaterialQuery(SonQaliqSql,endDatePlusOne: endDatePlusOne);

            return new Mal_Material_Hereketleri
            {
                IlkinQaliq = IlkinQaliq ?? new Mal_Material(),
                Medaxil = Medaxil ?? new Mal_Material(),
                Mexaric = Mexaric ?? new Mal_Material(),
                Silinme = Silinme ?? new Mal_Material(),
                SonQaliq = SonQaliq ?? new Mal_Material(),
            };
        }

        private Mal_Material ExecuteMaterialQuery(string sql, DateTime? startDate = null, DateTime? endDate = null, DateTime? endDatePlusOne = null)
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

            using var reader = cmd.ExecuteReader();

            var result = new Mal_Material();

            while (reader.Read())
            {
                string type = reader["ProductGroupType"]?.ToString()?.Trim();
                decimal value = 0;

                try
                {
                    value = reader["TotalAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TotalAmount"]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("TotalAmount parse xətası: " + ex.Message);
                }

                Console.WriteLine($"Gələn tip: '{type}', Dəyər: {value}");

                if (!string.IsNullOrEmpty(type))
                {
                    if (type.Contains("Derman", StringComparison.OrdinalIgnoreCase))
                        result.Derman = value;
                    else if (type.Contains("Teserrufat", StringComparison.OrdinalIgnoreCase))
                        result.Teserrufat = value;
                    else if (type.Contains("Invent", StringComparison.OrdinalIgnoreCase))
                        result.Inventor = value;
                    else
                        Console.WriteLine($"[⚠️] Naməlum tip: '{type}'");
                }
                else
                {
                    Console.WriteLine("[⚠️] ProductGroupType boş və ya null gəldi.");
                }
            }

            Console.WriteLine($"[✅] Nəticə - Derman: {result.Derman}, Teserrufat: {result.Teserrufat}, Inventor: {result.Inventor}");

            return result;
        }


    }
}
