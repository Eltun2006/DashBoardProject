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
    public class Teskilat_Uzre
    {
        public int Icbari_Sigorta { get; set; }
        public int Diger_Sigorta { get; set; }
        public int Endirimler { get; set; }
        public int Oz_Hesabina { get; set; }
    }
    public class Xidmet_Tipi_Uzre
    {
        public decimal Emeliyyat { get; set; }
        public decimal Poliklinik { get; set; }
    }
    public class Xidmet_Categoryasi
    {
        public string CategoryName { get; set; }
        public decimal TotalPrice { get; set; }
    }
    public class Mal_Material
    {
        public decimal Derman { get; set; }
        public decimal Teserrufat { get; set; }
        public decimal Inventor { get; set; }
    }
    public class Mal_Material_Hereketleri
    {
        public Mal_Material IlkinQaliq { get; set; }
        public Mal_Material Medaxil { get; set; }
        public Mal_Material Mexaric { get; set; }
        public Mal_Material Silinme { get; set; }
        public Mal_Material SonQaliq { get; set; }
    }

    public class Dovriyye_Statistikasi
    {
        public Teskilat_Uzre TeskilatUzre { get; set; }
        public Xidmet_Tipi_Uzre XidmetTipi { get; set; }
        public List<Xidmet_Categoryasi> Xidmet_Categoryasi { get; set; }

    }

    public class FullDashBoardModel
    {
        public AccountBalance MalMulk { get; set; }
        public Dovriyye_Statistikasi Dovriyye { get; set; }
        public Mal_Material_Hereketleri MalMaterialHereketleri { get; set; }
    }
}
