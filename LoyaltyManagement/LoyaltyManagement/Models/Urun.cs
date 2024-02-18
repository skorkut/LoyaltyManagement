namespace LoyaltyManagement.Models
{
    public class Urun
    {
        public int UrunId { get; set; }
        public string UrunAdi { get; set; }
        public int UrunAdet { get; set; }
        public string UrunAciklama { get; set; }
        public int UrunFiyat { get; set; }
        public int KategoriId { get; set; }
        public int MarkaId { get; set; }
        public virtual Kategori Kategori { get; set; }
        public virtual Marka Marka { get; set; }
    }
}
