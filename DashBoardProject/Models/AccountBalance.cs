using System.Reflection.Metadata.Ecma335;

namespace DashBoardProject.Models
{
    public class MalMulkHereketleri
    {
        public decimal Bank { get; set; }
        public decimal Kassa { get; set; }
    }
    public class AccountBalance
    {
        public MalMulkHereketleri IlkinQaliq {  get; set; }
        public MalMulkHereketleri Medaxil { get; set; }
        public MalMulkHereketleri Mexaric { get; set; }
        public MalMulkHereketleri SonQaliq { get; set; }

    }
    public class ByOrganization
    {
        public int Icbari_Sigorta { get; set; }
        public int Diger_Sigorta { get; set; }
        public int Endirimler { get; set; }
        public int Oz_Hesabina { get; set; }
    }
    public class Byservicetype
    {
        public decimal Emeliyyat { get; set; }
        public decimal Poliklinik { get; set; }
    }
    public class Xidmet_Categoryasi
    {
        public string CategoryName { get; set; }
        public decimal TotalPrice { get; set; }
    }
    public class Goods_and_materials
    {
        public decimal Derman { get; set; }
        public decimal Serfiyyat { get; set; }
        public decimal Digerleri { get; set; }
    }
    public class Inventory_Movement
    {
        public Goods_and_materials IlkinQaliq { get; set; }
        public Goods_and_materials Medaxil { get; set; }
        public Goods_and_materials Mexaric { get; set; }
        public Goods_and_materials Silinme { get; set; }
        public Goods_and_materials SonQaliq { get; set; }
    }

    public class TurnoverStatistics
    {
        public ByOrganization TeskilatUzre { get; set; }
        public Byservicetype XidmetTipi { get; set; }
        public List<Xidmet_Categoryasi> Xidmet_Categoryasi { get; set; }

    }

    public class FullDashBoardModel
    {
        public AccountBalance MalMulk { get; set; }
        public TurnoverStatistics Dovriyye { get; set; }
        public Inventory_Movement MalMaterialHereketleri { get; set; }
    }
}
