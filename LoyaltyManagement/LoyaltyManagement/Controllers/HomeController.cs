using LoyaltyManagement.Data;
using LoyaltyManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net.Mail;
using System.Net;
using System.Runtime;
using System.Linq.Expressions;
//using LoyaltyManagement.Migrations;
using LoyaltyManagement.ViewModels;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace LoyaltyManagement.Controllers
{
	[ServiceFilter(typeof(MenuActionFilter))]
	public class HomeController : Controller
	{
		public enum Roller
		{
			Admin = 1,
			Kullanici = 2

		}
		private readonly LogSettings _settings;

		private readonly LogService _myService;

		private readonly ILogger<HomeController> _logger;
		private readonly LoyaltyManagementDbContext loyaltyContextDb;
		private SepetModel sepetModel;
		public HomeController(ILogger<HomeController> logger, LoyaltyManagementDbContext context, LogService service, IOptions<LogSettings> settings)
		{
			_logger = logger;
			loyaltyContextDb = context;
			_settings = settings.Value;
			_myService = service;
			sepetModel = new SepetModel();
			sepetModel._context = context;
		}

		public IActionResult Index()
		{
			var nameClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
			var surnameClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname);
			if (nameClaim != null && surnameClaim != null)
			{
				var adsoyad = nameClaim.Value + " " + surnameClaim.Value;
				TempData["AktifKullaniciAdi"] = adsoyad;
			}
			else
				TempData["AktifKullaniciAdi"] = "";
			List<UrunViewModel> cokSatanlar = CokSatanlariGetir();
            var cart = sepetModel.GetCart(this.HttpContext); //maili ya da Guid bilgisini okuduk
            HttpContext.Session.SetString("adet", cart.GetCount().ToString());
            return View(cokSatanlar);
		}

		public List<UrunViewModel> CokSatanlariGetir()
		{
			// Siparişler tablosuna en çok eklenen ilk 2 ürünü getiren sorgu
			List<int> coksatanUrunIdler = (from siparis in loyaltyContextDb.Siparisler
										   group siparis by siparis.UrunId into urunGrup
										   select new
										   {
											   UrunId = urunGrup.Key,
											   UrunSayisi = urunGrup.Count()
										   }).OrderByDescending(x => x.UrunSayisi).Take(3).Select(x => x.UrunId).ToList();

			IEnumerable<UrunFotografViewModel> urunFotograflari = from urunfotograf in loyaltyContextDb.UrunFotograflari
																  where coksatanUrunIdler.Contains(urunfotograf.UrunId)
																  select new UrunFotografViewModel()
																  {
																	  UrunId = urunfotograf.UrunId,
																	  UrunFotografId = urunfotograf.UrunFotografId,
																	  FotografAdres = urunfotograf.FotografAdres
																  };

			List<UrunViewModel> cokSatanUrunler = (from urun in loyaltyContextDb.Urunler
												   join marka in loyaltyContextDb.Markalar on urun.MarkaId equals marka.MarkaId
												   join kategori in loyaltyContextDb.Kategoriler on urun.KategoriId equals kategori.KategoriId
												   let UrunFotografList = urunFotograflari.Where(x => x.UrunId == urun.UrunId)
												   where coksatanUrunIdler.Contains(urun.UrunId)
												   select new UrunViewModel()
												   {
													   UrunAdedi = urun.UrunAdet,
													   UrunAdi = urun.UrunAdi,
													   UrunAciklamasi = urun.UrunAciklama,
													   UrunFiyati = urun.UrunFiyat,
													   UrunId = urun.UrunId,
													   MarkaAdi = marka.MarkaAdi,
													   KategoriAdi = kategori.KategoriAdi,
													   UrunFotograflari = UrunFotografList.ToList()
												   }).ToList();
			return cokSatanUrunler;
		}

		public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
		[HttpGet]
		public IActionResult Login()
		{
			return View();
		}
		[HttpPost]
		public async Task<IActionResult> Login(LoyaltyManagement.ViewModels.LoginRegisterViewModel model)
		{
			try
			{
				Kullanici kullanici = loyaltyContextDb.Kullanicilar.Where(x => x.EPosta == model.EPosta && x.Sifre == model.Sifre).FirstOrDefault();
				if (kullanici == null)
				{
					_myService.Log("Logs/LoginLogs.txt", "Var olmayan kullanıcı girişi. Girilen e-posta :" + model.EPosta + " Hata tarihi :" + DateTime.Now + "\n");
					throw new Exception("Girdiginiz e-posta ve sifreye ait bir kullanici sistemde bulunmamaktadir.");
				}
				else if (kullanici.AktifMi == false)
				{
					_myService.Log("Logs/LoginLogs.txt", "Aktif olmayan kullanıcı girişi. Girilen e-posta :" + model.EPosta + " Hata tarihi :" + DateTime.Now + "\n");
					throw new Exception("Kullanici aktif edilmedi, mail adresinize gelen linke tiklayarak aktif edebilirsiniz.");
				}
				else
				{
					if (kullanici.RolId == 1)
					{
						return RedirectToAction("AdminPanel", "Home");
					}
                    TempData["ErrorMessage"] = "";
					var claims = new List<Claim> { new Claim(ClaimTypes.Name, kullanici.Ad), new Claim(ClaimTypes.Surname, kullanici.Soyad), new Claim(ClaimTypes.PrimarySid, kullanici.KullaniciId.ToString()) };
					var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
					var authProperties = new AuthenticationProperties
					{
						ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(40),
						RedirectUri = "/Home/Index"
					};
					await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
					var cart = sepetModel.GetCart(this.HttpContext); //maili ya da Guid bilgisini okuduk
					sepetModel.MigrateCart(cart.AlisverisKullaniciId, kullanici.KullaniciId, this.HttpContext);
					return RedirectToAction("Index", "Home");
				}
			}
			catch (Exception ex)
			{
				TempData["ErrorMessage"] = ex.Message;
				return View();
			}
		}

		[HttpPost]
		public IActionResult Register(LoyaltyManagement.ViewModels.LoginRegisterViewModel model)
		{
			try
			{
				Kullanici kullanici = loyaltyContextDb.Kullanicilar.Where(x => x.EPosta == model.EPosta).FirstOrDefault();
				if (kullanici != null)
				{
					throw new Exception("Girdiginiz e-posta sistemde bulunmaktadir. Lutfen farkli bir e-posta girisi deneyiniz.");
				}
				else
				{
					Kullanici kullaniciDb = new Kullanici();
					kullaniciDb.Ad = model.Ad;
					kullaniciDb.Soyad = model.Soyad;
					kullaniciDb.EPosta = model.EPosta;
					kullaniciDb.Sifre = model.Sifre;
					kullaniciDb.Adres = "";
					kullaniciDb.AktifMi = false;
					kullaniciDb.RolId = (int)Roller.Kullanici;
					loyaltyContextDb.Kullanicilar.Add(kullaniciDb);
					loyaltyContextDb.SaveChanges();
					string icerik = KayitEpostaİcerikHazirla(kullaniciDb.Ad, kullaniciDb.Soyad, kullaniciDb.KullaniciId);
					Mail mail = new Mail();
					mail.MailAt(kullaniciDb.EPosta, kullaniciDb.Ad, kullaniciDb.Soyad, "Kullanıcı Aktivitasyonu", icerik);
					return RedirectToAction("Login");
				}

			}
			catch (Exception ex)
			{
				TempData["ErrorMessage"] = ex.Message;
				return RedirectToAction("Login");
			}
		}
		public async Task<IActionResult> CikisYap()
		{
			await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			HttpContext.Session.Clear();
			return RedirectToAction("Index");
		}
		public string KayitEpostaİcerikHazirla(string Ad, string Soyad, int kullaniciId)
		{
			string metin = "Sayın" + Ad + " " + Soyad + "kullanıcı bilgileriniz başarı bir şekilde alınmıştır. Aktivasyon için lütfen aşağıdaki linke tıklayınız.";
			string link = "https://localhost:7187/Home/AktifEt?kullaniciId=" + kullaniciId;
			metin += link;
			return metin;
		}

		public IActionResult AktifEt(int kullaniciId)
		{
			Kullanici kullanici = loyaltyContextDb.Kullanicilar.Where(x => x.KullaniciId == kullaniciId).FirstOrDefault();
			if (kullanici != null)
			{
				kullanici.AktifMi = true;
				loyaltyContextDb.Kullanicilar.Update(kullanici);
				loyaltyContextDb.SaveChanges();
			}
			return RedirectToAction("Login");
		}

		[HttpPost]
		public string KampanyaKodKaydet(string kod)
		{
			var message = "";
			try
			{
				var kampanyakodu = loyaltyContextDb.KampanyaKodlari.Where(x => x.KampanyaKodu == kod).FirstOrDefault();
				if (kampanyakodu == null)
				{
					throw new Exception("Girdiginiz koda ait bir kampanya kodu bulunamadi.");
				}
				else if (kampanyakodu.KullanildiMi == true)
				{
					throw new Exception("Girdiginiz kampanya kodu daha once kullanilmistir.");
				}
				else
				{
					int bakiye = Convert.ToInt16(ViewBag.CuzdanBakiye);
					var sonuc = bakiye + kampanyakodu.Puan;
					if (sonuc > 100)
					{
						throw new Exception("Cüzdanınızdaki toplam puanınız 100 den büyük olamaz.");
					}
					int KullaniciId = ViewBag.AktifKullaniciId == "" ? 0 : Convert.ToInt16(ViewBag.AktifKullaniciId);
					Cuzdan cuzdandb = new Cuzdan();
					cuzdandb.KampanyaKodId = kampanyakodu.KampanyaKodId;
					cuzdandb.KullaniciId = KullaniciId;
					loyaltyContextDb.Cuzdan.Add(cuzdandb);
					kampanyakodu.KullanildiMi = true;
					loyaltyContextDb.KampanyaKodlari.Update(kampanyakodu);
					loyaltyContextDb.SaveChanges();
					message = "Girdiginiz kod basarili ile cuzdaniniza eklenmistir.";
					return message;
				}
			}
			catch (Exception ex)
			{
				message = ex.Message;
				return message;
			}
		}
		public IActionResult Cuzdan()
		{
			int kullaniciid = Convert.ToInt16(ViewBag.AktifKullaniciId);
			var cuzdanpuan = (from cuzdan in loyaltyContextDb.Cuzdan
							  join kampanyakod in loyaltyContextDb.KampanyaKodlari on cuzdan.KampanyaKodId equals kampanyakod.KampanyaKodId
							  where cuzdan.KullaniciId == kullaniciid
                              select kampanyakod.Puan).Sum();
            return View(cuzdanpuan);
		}
		[HttpGet]
		public IActionResult KullaniciBilgiGuncelle()
		{
			int KullaniciId = ViewBag.AktifKullaniciId == "" ? 0 : Convert.ToInt16(ViewBag.AktifKullaniciId);
			var kullaniciBilgi = loyaltyContextDb.Kullanicilar.Where(x => x.KullaniciId == KullaniciId).FirstOrDefault();
			return View(kullaniciBilgi);
		}
		[HttpPost]
		public IActionResult KullaniciBilgiGuncelle(Kullanici model)
		{
			Kullanici kullanici = loyaltyContextDb.Kullanicilar.Where(x => x.KullaniciId == model.KullaniciId).FirstOrDefault();
			kullanici.Ad = model.Ad;
			kullanici.Soyad = model.Soyad;
			kullanici.EPosta = model.EPosta;
			kullanici.Adres = model.Adres;
			loyaltyContextDb.Kullanicilar.Update(kullanici);
			loyaltyContextDb.SaveChanges();


			return RedirectToAction("KullaniciBilgiGuncelle");
		}

		public void SifreGuncelle(string sifre)
		{
            int KullaniciId = ViewBag.AktifKullaniciId == "" ? 0 : Convert.ToInt16(ViewBag.AktifKullaniciId);
            Kullanici kullanici = loyaltyContextDb.Kullanicilar.Where(x=>x.KullaniciId==KullaniciId).FirstOrDefault();
			kullanici.Sifre = sifre;
			loyaltyContextDb.Kullanicilar.Update(kullanici);
			loyaltyContextDb.SaveChanges();
        }

		public IActionResult AdminPanel()
		{
			var siparisSayisi = ToplamSiparisSayisiGetir();
			ViewBag.SiparisSayisi = siparisSayisi;

            var kullaniciSayisi = KullaniciSayisiGetir();
            ViewBag.kullaniciSayisi = kullaniciSayisi;

            return View();
		}

        public List<KullaniciViewModel> KullanicilariGetir() {


			List<KullaniciViewModel> kullanicilar = (from kullanici in loyaltyContextDb.Kullanicilar
													 where kullanici.RolId == (int)Roller.Kullanici
                                                     orderby kullanici.Ad
													 select new KullaniciViewModel()
													 {
														 KullaniciId= kullanici.KullaniciId,
                                                         AdSoyad = kullanici.Ad + " " + kullanici.Soyad,
														 EPosta= kullanici.EPosta,
														 Adres= kullanici.Adres,
														 Aktifmi= kullanici.AktifMi

													 }).ToList();
			return kullanicilar;

                }
        public IActionResult KullaniciListe()
        {
            List<KullaniciViewModel> kullanicilar = KullanicilariGetir();
            return View(kullanicilar);
        }
		public int ToplamSiparisSayisiGetir()
		{
            int count = (from siparis in loyaltyContextDb.Siparisler
                          select siparis).Count();
			return count;
        }

        public int KullaniciSayisiGetir()
        {
            int count = (from kullanici in loyaltyContextDb.Kullanicilar
						 where kullanici.AktifMi==true && kullanici.RolId==(int)Roller.Kullanici
                         select kullanici).Count();
            return count;
        }
    }
}