using LoyaltyManagement.Data;
using LoyaltyManagement.Models;
using LoyaltyManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Runtime;
using System.Text.Encodings.Web;

namespace LoyaltyManagement.Controllers
{
    [ServiceFilter(typeof(MenuActionFilter))]
    public class SiparisController : Controller
    {
        private readonly LoyaltyManagementDbContext loyaltyContextDb;
        private SepetModel sepetModel;
        public SiparisController(LoyaltyManagementDbContext context)
        {
            loyaltyContextDb = context;
            sepetModel = new SepetModel();
            sepetModel._context = context;
        }
        public IActionResult Sepet()
        {
            SepetModel bus = new SepetModel();
            var cart = sepetModel.GetCart(this.HttpContext); //maili ya da Guid bilgisini okuduk
            HttpContext.Session.SetString("adet", cart.GetCount().ToString());
            bus.AlisverisKullaniciId = cart.AlisverisKullaniciId;
            // Set up our ViewModel
            List<SepetViewModel> viewModel = cart.GetCartItems();

            return View(viewModel);
        }

        //
        // GET: /Store/AddToCart/5
        [HttpPost]
        public IActionResult SepeteEkle(int urunid)
        {
            var urun = loyaltyContextDb.Urunler.Where(x => x.UrunId == urunid).FirstOrDefault();
            var cart = sepetModel.GetCart(this.HttpContext); //alışveriş yapanın mailini ya da guid bilgisini alırız.
            cart.AddToCart(urun);
            HttpContext.Session.SetString("adet", cart.GetCount().ToString());
            var adett = HttpContext.Session.GetString("adet");
            ViewData["sepetAdedi"] = adett;
            return RedirectToAction("Urunler", "Urun");
        }
        [HttpPost]
        public void SepettenSil(int sepetid)
        {
            var cart = sepetModel.GetCart(this.HttpContext);
            int itemCount = cart.RemoveFromCart(sepetid);
            HttpContext.Session.SetString("adet", cart.GetCount().ToString());
        }
        public IActionResult SepetiBosalt()
        {
            HttpContext.Session.Clear();        //logout iken çalıştı
            sepetModel.EmptyCart(ViewBag.AktifKullaniciId);
            return RedirectToAction("Index", "Home");
        }
        public JsonResult GetUrunToplam()
        {
            decimal toplam = 0;
            var cart = sepetModel.GetCart(this.HttpContext); //maili ya da Guid bilgisini okuduk
            sepetModel.AlisverisKullaniciId = cart.AlisverisKullaniciId;
            toplam = sepetModel.GetTotal();
            return Json(toplam);
        }
        public void AdetKaydet(int sepetid, int adet)
        {
            var sepet = loyaltyContextDb.Sepet.Where(x => x.SepetId == sepetid).FirstOrDefault();
            sepet.Adet = adet;
            loyaltyContextDb.Sepet.Update(sepet);
            loyaltyContextDb.SaveChanges();
        }
        public IActionResult SiparisVer()
        {
            var aktifkullanici = ViewBag.AktifKullaniciId;
            if (aktifkullanici == null)
            {
                return RedirectToAction("Login","Home");
            }
            SepetModel bus = new SepetModel();
            var cart = sepetModel.GetCart(this.HttpContext); //maili ya da Guid bilgisini okuduk
            bus.AlisverisKullaniciId = cart.AlisverisKullaniciId;
            // Set up our ViewModel
            List<SepetViewModel> viewModel = cart.GetCartItems();
            TempData["SepettekiUrunler"] = viewModel;

            int KullaniciId = Convert.ToInt32(bus.AlisverisKullaniciId);
            Kullanici kullaniciBilgisi = KullaniciBilgileriniGetir(KullaniciId);
            return View(kullaniciBilgisi);
        }
        public Kullanici KullaniciBilgileriniGetir(int KullaniciId)
        {
            Kullanici kullaniciBilgi = loyaltyContextDb.Kullanicilar.Where(x => x.KullaniciId == KullaniciId).FirstOrDefault();
            return kullaniciBilgi;
        }
        public IActionResult KartBilgi()
        {
            return View();
        }
        public string SiparisiTamamla(string adres, bool puankullanilsinmi)
        {
            int kullaniciId = Convert.ToInt32(ViewBag.AktifKullaniciId);

            var kullanici = loyaltyContextDb.Kullanicilar.Where(x => x.KullaniciId == kullaniciId).FirstOrDefault();
            string takipkodu = "";
            // Kullanıcının adresi güncellensin.
            AdresGuncelle(kullanici, adres);
            if (puankullanilsinmi)
                CuzdanBakiyeTemizle();
            SiparisKaydet();
            // Sepetteki ürünler temizlensin.
            sepetModel.EmptyCart(ViewBag.AktifKullaniciId);

            //Takip kodu döndüren fonk yazılıp çağırılsın.

            Random random = new Random();
            takipkodu = random.Next(10000000, 99999999).ToString();

            //Mail gönderilsin.
            Mail mail = new Mail();
            string icerik = SiparisEpostaİcerikHazirla(kullanici.Ad, kullanici.Soyad, kullanici.KullaniciId, takipkodu);
            mail.MailAt(kullanici.EPosta, kullanici.Ad, kullanici.Soyad, "Sipariş Oluşturuldu", icerik);
            return takipkodu;
        }
        public void CuzdanBakiyeTemizle()
        {
            int kullaniciId = Convert.ToInt32(ViewBag.AktifKullaniciId);
            var cuzdanPuanlari = loyaltyContextDb.Cuzdan.Where(x => x.KullaniciId == kullaniciId).ToList();
            foreach (var puan in cuzdanPuanlari)
            {
                loyaltyContextDb.Cuzdan.Remove(puan);
            }
            loyaltyContextDb.SaveChanges();
        }
        public string SiparisEpostaİcerikHazirla(string Ad, string Soyad, int kullaniciId, string takipkodu)
        {
            string metin = "Sayın " + Ad + " " + Soyad + " siparişiniz başarı ile oluşturulmuştur. " + takipkodu + " takip kodu ile siparişinizi takip edebilirsiniz.";
            return metin;
        }

        public void AdresGuncelle(Kullanici kullanici, string adres)
        {
            kullanici.Adres = adres;
            loyaltyContextDb.Kullanicilar.Update(kullanici);
            loyaltyContextDb.SaveChanges();
        }
        public List<SiparisViewModel> SiparisleriGetir()
        {
            IEnumerable<UrunFotografViewModel> urunFotograflari = from urunfotograf in loyaltyContextDb.UrunFotograflari
                                                                  select new UrunFotografViewModel()
                                                                  {
                                                                      UrunId = urunfotograf.UrunId,
                                                                      UrunFotografId = urunfotograf.UrunFotografId,
                                                                      FotografAdres = urunfotograf.FotografAdres
                                                                  };

            List<SiparisViewModel> siparisler = (from siparis in loyaltyContextDb.Siparisler
                                                 join urun in loyaltyContextDb.Urunler on siparis.UrunId equals urun.UrunId
                                                 join kullanici in loyaltyContextDb.Kullanicilar on siparis.KullaniciId equals kullanici.KullaniciId
                                                 let UrunFotografList = urunFotograflari.Where(x => x.UrunId == urun.UrunId)
                                                 orderby siparis.SiparisTarihi descending
                                                 select new SiparisViewModel()
                                                 {
                                                     SiparisId = siparis.SiparisId,
                                                     UrunAdi = urun.UrunAdi,
                                                     KullaniciAdi = kullanici.Ad + " " + kullanici.Soyad,
                                                     SiparisAdet = siparis.SiparisAdet,
                                                     SiparisTarihi = siparis.SiparisTarihi,
                                                     UrunFotografi = UrunFotografList.FirstOrDefault() == null ? "" : UrunFotografList.FirstOrDefault().FotografAdres
                                                 }).ToList();
            return siparisler;
        }
        public IActionResult SiparisListe()
        {
            List<SiparisViewModel> siparisler = SiparisleriGetir();
            return View(siparisler);
        }
        public void SiparisKaydet()
        {
            string kullaniciId = ViewBag.AktifKullaniciId;
            var sepettekiurunler = loyaltyContextDb.Sepet.Where(x => x.KullaniciId == kullaniciId).ToList();
            foreach (var urun in sepettekiurunler)
            {
                Siparis siparis = new Siparis();
                siparis.KullaniciId = Convert.ToInt32(kullaniciId);
                siparis.SiparisTarihi = DateTime.Now;
                siparis.SiparisAdet = urun.Adet;
                siparis.UrunId = urun.UrunId;
                loyaltyContextDb.Siparisler.Add(siparis);
            }
            loyaltyContextDb.SaveChanges();
            UrunStokGuncelle(sepettekiurunler);
        }
        public void UrunStokGuncelle(List<Sepet> sepeturunleri)
        {
            foreach (var urun in sepeturunleri)
            {
                var urunkayit = loyaltyContextDb.Urunler.Where(x => x.UrunId == urun.UrunId).FirstOrDefault();
                if (urunkayit.UrunAdet >= urun.Adet)
                {
                    urunkayit.UrunAdet -= urun.Adet;
                    loyaltyContextDb.Urunler.Update(urunkayit);
                }
            }
            loyaltyContextDb.SaveChanges();
        }
    }
}

