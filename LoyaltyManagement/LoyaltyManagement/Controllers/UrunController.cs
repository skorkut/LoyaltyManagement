using LoyaltyManagement.Data;
using LoyaltyManagement.Models;
using LoyaltyManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LoyaltyManagement.Controllers
{
    [ServiceFilter(typeof(MenuActionFilter))]
    public class UrunController : Controller
    {
        private readonly LoyaltyManagementDbContext loyaltyContextDb;

        public UrunController(LoyaltyManagementDbContext context)
        {
            loyaltyContextDb = context;
        }

        public List<UrunViewModel> UrunleriGetir(int kategoriid, int? markaid = 0)
        {
            IEnumerable<UrunFotografViewModel> urunFotograflari = from urunfotograf in loyaltyContextDb.UrunFotograflari
                                                                  select new UrunFotografViewModel()
                                                                  {
                                                                      UrunId = urunfotograf.UrunId,
                                                                      UrunFotografId = urunfotograf.UrunFotografId,
                                                                      FotografAdres = urunfotograf.FotografAdres
                                                                  };
            List<UrunViewModel> Urunler = (from urun in loyaltyContextDb.Urunler
                                           join marka in loyaltyContextDb.Markalar on urun.MarkaId equals marka.MarkaId
                                           join kategori in loyaltyContextDb.Kategoriler on urun.KategoriId equals kategori.KategoriId
                                           let UrunFotografList = urunFotograflari.Where(x => x.UrunId == urun.UrunId)
                                           where (urun.KategoriId == kategoriid || kategoriid == 0) && (urun.MarkaId == markaid || markaid == 0)
                                           select new UrunViewModel()
                                           {
                                               UrunId = urun.UrunId,
                                               UrunAdi = urun.UrunAdi,
                                               UrunAciklamasi = urun.UrunAciklama,
                                               UrunAdedi = urun.UrunAdet,
                                               UrunFiyati = urun.UrunFiyat,
                                               MarkaAdi = marka.MarkaAdi,
                                               KategoriAdi = kategori.KategoriAdi,
                                               UrunFotograflari = UrunFotografList.ToList()
                                           }).ToList();
            return Urunler;
        }

        public IActionResult Urunler(int kategoriid, int? markaid = 0)
        {
            List<UrunViewModel> urunliste = UrunleriGetir(kategoriid, markaid);
            return PartialView(urunliste);
        }
        public IActionResult UrunDetay(int urunid)
        {
            List<UrunFotografViewModel> urunFotograflari = (from urunfotograf in loyaltyContextDb.UrunFotograflari
                                                            where urunfotograf.UrunId == urunid
                                                            select new UrunFotografViewModel()
                                                            {
                                                                UrunId = urunfotograf.UrunId,
                                                                UrunFotografId = urunfotograf.UrunFotografId,
                                                                FotografAdres = urunfotograf.FotografAdres
                                                            }).ToList();
            UrunViewModel? UrunDetay = (from urun in loyaltyContextDb.Urunler
                                        join marka in loyaltyContextDb.Markalar on urun.MarkaId equals marka.MarkaId
                                        join kategori in loyaltyContextDb.Kategoriler on urun.KategoriId equals kategori.KategoriId
                                        where urun.UrunId == urunid
                                        select new UrunViewModel()
                                        {
                                            UrunId = urun.UrunId,
                                            UrunAdi = urun.UrunAdi,
                                            UrunAciklamasi = urun.UrunAciklama,
                                            UrunAdedi = urun.UrunAdet,
                                            UrunFiyati = urun.UrunFiyat,
                                            MarkaAdi = marka.MarkaAdi,
                                            KategoriAdi = kategori.KategoriAdi,
                                            UrunFotograflari = urunFotograflari
                                        }).FirstOrDefault();
            return PartialView(UrunDetay);
        }
        [HttpPost]
        public IActionResult UrunKaydet(UrunViewModel model)
        {
            bool YeniUrunMu = true;
            Urun urunDb = new Urun();
            if (model.UrunId > 0)
            {
                urunDb = loyaltyContextDb.Urunler.Where(x => x.UrunId == model.UrunId).FirstOrDefault();
                YeniUrunMu = false;
            }
            urunDb.UrunAdi = model.UrunAdi;
            urunDb.UrunAdet = model.UrunAdedi;
            urunDb.UrunFiyat = model.UrunFiyati;
            urunDb.UrunAciklama = model.UrunAciklamasi;
            urunDb.MarkaId = model.MarkaId;
            urunDb.KategoriId = model.KategoriId;
            if (model.UrunId > 0)
            {
                loyaltyContextDb.Urunler.Update(urunDb);
            }
            else
            {
                loyaltyContextDb.Urunler.Add(urunDb);
            }
            loyaltyContextDb.SaveChanges();
            UrunFotografKaydet(model.ProductImages, urunDb.UrunId, YeniUrunMu);
            return RedirectToAction("UrunListe");
        }
        public void UrunFotografKaydet(List<IFormFile> ProductImages, int UrunId, bool YeniUrunMu)
        {
            if (YeniUrunMu == false)
            {
                var urunFotograflari = loyaltyContextDb.UrunFotograflari.Where(x => x.UrunId == UrunId).ToList();
                foreach (var item in urunFotograflari)
                {
                    loyaltyContextDb.UrunFotograflari.Remove(item);
                }
            }
            if (ProductImages != null)
            {
                foreach (var item in ProductImages)
                {
                    UrunFotograf fotoDb = new UrunFotograf();
                    fotoDb.FotografAdres = "../resimler/" + item.FileName;
                    fotoDb.UrunId = UrunId;
                    loyaltyContextDb.UrunFotograflari.Add(fotoDb);
                }
            }            
            loyaltyContextDb.SaveChanges();
        }
        public IActionResult UrunSil(int urunid)
        {

            var urun = loyaltyContextDb.Urunler.Where(x => x.UrunId == urunid).FirstOrDefault();
            loyaltyContextDb.Urunler.Remove(urun);
            var urunfotograflari = loyaltyContextDb.UrunFotograflari.Where(x => x.UrunId == urunid).ToList();
            foreach (var urunfotograf in urunfotograflari)
            {
                loyaltyContextDb.UrunFotograflari.Remove(urunfotograf);
            }
            loyaltyContextDb.SaveChanges();
           return RedirectToAction("UrunListe", "Urun");
        }
        public List<SiparisViewModel> SiparisleriGetir()
        {
            List<SiparisViewModel> siparisler = null;
            return siparisler;
        }
        public IActionResult UrunListe()
        {
            List<UrunViewModel> urunler = UrunleriGetir(0, 0);
            return View(urunler);
        }

        public IActionResult UrunEkle(int? urunid = 0)
        {
            UrunViewModel model = new UrunViewModel();
            List<SelectListItem> modelVerisi = new List<SelectListItem>();
            var kategoriler = loyaltyContextDb.Kategoriler.ToList().OrderBy(k => k.KategoriAdi);
            foreach (var item in kategoriler)
            {
                modelVerisi.Add(new SelectListItem { Value = item.KategoriId.ToString(), Text = item.KategoriAdi });
            }
            ViewBag.KategoriOptions = modelVerisi;
            if (urunid > 0)
            {
                var urunbilgi = loyaltyContextDb.Urunler.Where(x => x.UrunId == urunid).FirstOrDefault();
                var urunfotograflari = (from urunfoto in loyaltyContextDb.UrunFotograflari
                                        where urunfoto.UrunId == urunid
                                        select new UrunFotografViewModel()
                                        {
                                            UrunFotografId = urunfoto.UrunFotografId,
                                            FotografAdres = urunfoto.FotografAdres,
                                            UrunId = urunfoto.UrunId
                                        }).ToList();
                model.UrunFiyati = urunbilgi.UrunFiyat;
                model.UrunAdi = urunbilgi.UrunAdi;
                model.UrunAdedi = urunbilgi.UrunAdet;
                model.UrunId = urunbilgi.UrunId;
                model.KategoriId = urunbilgi.KategoriId;
                model.MarkaId = urunbilgi.MarkaId;
                model.UrunAciklamasi = urunbilgi.UrunAciklama;
                model.UrunFotograflari = urunfotograflari;
            }
            return View(model);
        }
        public List<MarkaViewModel> MarkalariGetir(int kategoriId)
        {
            List<MarkaViewModel> markalar = (from kategorimarka in loyaltyContextDb.KategoriMarkalari
                                             join marka in loyaltyContextDb.Markalar on kategorimarka.MarkaId equals marka.MarkaId
                                             where kategorimarka.KategoriId == kategoriId
                                             orderby marka.MarkaAdi
                                             select new MarkaViewModel()
                                             {
                                                 MarkaId = marka.MarkaId,
                                                 MarkaAdi = marka.MarkaAdi
                                             }).ToList();
            return markalar;
        }

        public IActionResult KodUret()
        {
            return View();
        }

        public void KodKaydet(int quantity, int points)
        {
            KampanyaKod kampanyaKod;
            for (int i = 0; i < quantity; i++)
            {
                var kod = RandomString();
                kampanyaKod = new KampanyaKod();
                kampanyaKod.Puan = points;
                kampanyaKod.KampanyaKodu = kod;
                kampanyaKod.KullanildiMi = false;
                loyaltyContextDb.KampanyaKodlari.Add(kampanyaKod);
            }
            loyaltyContextDb.SaveChanges();
        }
        private static Random random = new Random();
        private static string RandomString()
        {
            const string pool = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var chars = Enumerable.Range(0, 6)
                .Select(x => pool[random.Next(0, pool.Length)]);
            return new string(chars.ToArray());
        }
        public int UrunStokGetir(int urunid)
        {
            var urun = loyaltyContextDb.Urunler.Where(x => x.UrunId == urunid).FirstOrDefault();
            int stok = urun == null ? 0 : urun.UrunAdet;
            return stok;
        }
    }

   
}

