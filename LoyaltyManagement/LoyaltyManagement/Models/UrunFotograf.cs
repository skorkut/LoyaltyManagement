namespace LoyaltyManagement.Models
{
    public class UrunFotograf
    {
        public int UrunFotografId { get; set; }
        public int UrunId { get; set; }
        public string FotografAdres { get; set; }
        public virtual Urun Urun { get; set; }
    }
}
