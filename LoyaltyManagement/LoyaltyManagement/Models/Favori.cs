namespace LoyaltyManagement.Models
{
    public class Favori
    {
        public int FavoriId { get; set; }
        public int UrunId { get; set; }
        public int KullaniciId { get; set; }
        public virtual Urun Urun { get; set; }
        public virtual Kullanici Kullanici { get; set; }
    }
}
