namespace LoyaltyManagement.Models
{
	public class KategoriMarka
	{
		public int KategoriMarkaId { get; set; }
		public int KategoriId {  get; set; }
		public int MarkaId {  get; set; }
		public virtual Kategori Kategori { get; set; }
		public virtual Marka Marka { get; set; }
	}
}
