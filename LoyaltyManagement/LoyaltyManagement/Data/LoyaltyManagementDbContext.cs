using LoyaltyManagement.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LoyaltyManagement.Data
{
    public class LoyaltyManagementDbContext : DbContext
    {
        public LoyaltyManagementDbContext(DbContextOptions<LoyaltyManagementDbContext> options) : base(options)
        {
        }
        public DbSet<Rol> Roller { get; set; }
        public DbSet<Kullanici> Kullanicilar { get; set; }
        public DbSet<Kategori> Kategoriler { get; set; }
        public DbSet<Marka> Markalar { get; set; }
        public DbSet<Urun> Urunler { get; set; }
        public DbSet<UrunFotograf> UrunFotograflari {  get; set; }
        public DbSet<Sepet> Sepet { get; set; }
        public DbSet<Favori> Favoriler { get; set; }
        public DbSet<Cuzdan> Cuzdan {  get; set; }
        public DbSet<KampanyaKod> KampanyaKodlari { get; set; }
        public DbSet<Siparis> Siparisler { get; set; }
		public DbSet<KategoriMarka> KategoriMarkalari { get; set; }
	}
}
