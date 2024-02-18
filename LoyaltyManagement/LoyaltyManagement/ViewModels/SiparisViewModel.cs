namespace LoyaltyManagement.ViewModels
{
	public class SiparisViewModel
	{
		public int SiparisId { get; set; }
		public int UrunId { get; set; }
		public string UrunAdi { get; set; }
		public int KullaniciId { get; set; }
        public string KullaniciAdi { get; set; }
        public int SiparisAdet { get; set; }
        public string UrunFotografi { get; set; }
        public DateTime SiparisTarihi { get; set; }
	}
}
