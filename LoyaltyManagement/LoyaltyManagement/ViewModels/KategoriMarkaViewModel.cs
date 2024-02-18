namespace LoyaltyManagement.ViewModels
{
	public class KategoriMarkaViewModel
	{
		public int KategoriId { get; set; }
		public string KategoriAdi { get; set; }
		public List<ViewModels.MarkaViewModel> MarkaListe { get; set; }
	}
}
