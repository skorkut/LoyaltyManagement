namespace LoyaltyManagement.Models
{
    public class Siparis
    {
        public int SiparisId { get; set; }
        public int UrunId { get; set; }
        public int KullaniciId { get; set; }
        public int SiparisAdet {  get; set; }
        public DateTime SiparisTarihi { get; set; }
        public virtual Urun Urun { get; set; }
        public virtual Kullanici Kullanici { get; set; }
    }
}
