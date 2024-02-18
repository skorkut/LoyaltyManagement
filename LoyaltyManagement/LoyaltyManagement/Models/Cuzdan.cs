namespace LoyaltyManagement.Models
{
    public class Cuzdan
    {
        public int CuzdanId { get; set; }
        public int KampanyaKodId { get; set; }
        public int KullaniciId { get; set; }
        public virtual Kullanici Kullanici { get; set; }
        public virtual KampanyaKod KampanyaKod { get; set; }
    }
}
