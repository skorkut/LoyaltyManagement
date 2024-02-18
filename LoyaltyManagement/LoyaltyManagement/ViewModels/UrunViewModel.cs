using System.ComponentModel.DataAnnotations;

namespace LoyaltyManagement.ViewModels
{
    public class UrunViewModel
    {
        public int UrunId { get; set; }
        public string UrunAdi { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Adet 0'dan büyük olmalıdır.")]
        public int UrunAdedi { get; set; }
        public string UrunAciklamasi { get; set; }
        public int UrunFiyati { get; set; }
        public string KategoriAdi { get; set; }
        public string MarkaAdi { get; set; }
		public int KategoriId { get; set; }

		public int MarkaId { get; set; }

        public List<UrunFotografViewModel> UrunFotograflari {  get; set; }

        public List<IFormFile> ProductImages { get; set; }
    }
}
