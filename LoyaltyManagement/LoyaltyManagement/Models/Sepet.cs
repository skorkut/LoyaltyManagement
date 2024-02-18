namespace LoyaltyManagement.Models
{
    public class Sepet
    {
        public int SepetId { get; set; }
        public int UrunId { get; set; }
        public string KullaniciId { get; set; }
		public int Adet { get; set; }
		public virtual Urun Urun { get; set; }

    }
}
