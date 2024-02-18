using LoyaltyManagement.Data;
using LoyaltyManagement.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Security.Claims;

namespace LoyaltyManagement.Models
{
	public class SepetModel
	{
		public LoyaltyManagementDbContext _context { get; set; }
		public string AlisverisKullaniciId { get; set; }

		public const string SepetSessionKey = "SepetId"; //const: sabit
		public SepetModel GetCart(HttpContext context)
		{
			var cart = new SepetModel();
			cart._context = _context;
			cart.AlisverisKullaniciId = cart.GetCartId(context); //loginsen mailini, değilsen guidi aldı
			AlisverisKullaniciId = cart.AlisverisKullaniciId;
			return cart;
		}
		// Helper method to simplify shopping cart calls
		public SepetModel GetCart(Controller controller)
		{
			return GetCart(controller.HttpContext);
		}

		//Sepete eklenen ıd bilgisini alıp, o anki user bilgisi ile birlikte sql tarafında carts tablosuna yazar. Eğer daha önce eklenmiş ise değeri attırır. Hiç eklenmemiş ise sıfırdan o albümün bilgilerini carts tablosuna yazar.
		public void AddToCart(Urun urun)
		{
			var cartItem = _context.Sepet.Where(c => c.KullaniciId == AlisverisKullaniciId
				&& c.UrunId == urun.UrunId).FirstOrDefault();
			if (cartItem == null)
			{
				// Create a new cart item if no cart item exists
				cartItem = new Sepet
				{
					UrunId = urun.UrunId,
					KullaniciId = AlisverisKullaniciId,
					Adet = 1,
				};
				_context.Sepet.Add(cartItem);
			}
			else
			{
				// If the item does exist in the cart, 
				// then add one to the quantity
				cartItem.Adet++;
			}

			// Save changes
			_context.SaveChanges();
		}

		//Sepetten ürün silme işlemini yapar, aynı zamanda silinenin önce adedini azaltır. En son 1 den geri geldiğinde komple o ürünü siler.
		public int RemoveFromCart(int SepetId)
		{
			// Get the cart
			var cartItem = _context.Sepet.Single(cart => cart.SepetId == SepetId);

			int itemCount = 0;

			if (cartItem != null)
			{
				if (cartItem.Adet > 1)
				{
					cartItem.Adet--;
					itemCount = cartItem.Adet;
				}
				else
				{
					_context.Sepet.Remove(cartItem);
				}
				// Save changes
				_context.SaveChanges();
			}
			return itemCount;
		}

		//Sepeti Boşaltma işlemidir, aynı zamanda veri tabanındaki carts tablosundan da ürünleri siler.
		public void EmptyCart(string kullaniciId = "")
		{
			var KullaniciId = kullaniciId == "" ? AlisverisKullaniciId : kullaniciId;  //ternary 

			var cartItems = _context.Sepet.Where(cart => cart.KullaniciId == KullaniciId);

			foreach (var cartItem in cartItems)
			{
				_context.Sepet.Remove(cartItem);
			}
			// Save changes
			_context.SaveChanges();
		}

		//O userin sepetine ekleme işlemi başlangıcından bu yana dbdeki tüm ürünleri getirir.
		public List<SepetViewModel> GetCartItems()
		{
			IEnumerable<UrunFotografViewModel> urunFotograflari = from urunfotograf in _context.UrunFotograflari
																  select new UrunFotografViewModel()
																  {
																	  UrunId = urunfotograf.UrunId,
																	  UrunFotografId = urunfotograf.UrunFotografId,
																	  FotografAdres = urunfotograf.FotografAdres
																  };
			List<SepetViewModel> result = (from sepet in _context.Sepet
										   join urun in _context.Urunler on sepet.UrunId equals urun.UrunId
										   let UrunFotografList = urunFotograflari.Where(x => x.UrunId == urun.UrunId)
										   where sepet.KullaniciId == AlisverisKullaniciId
										   select new SepetViewModel()
										   {
											   SepetId = sepet.SepetId,
											   UrunId = urun.UrunId,
											   KullaniciId = sepet.SepetId,
											   UrunAdi = urun.UrunAdi,
											   UrunFiyati = urun.UrunFiyat,
											   Adet = sepet.Adet,
											   UrunFotografi = UrunFotografList.FirstOrDefault() == null ? "" : UrunFotografList.FirstOrDefault().FotografAdres
                                           }).ToList();
			return result;
		}


		//Session yapısını kullanarak giriş yapan bir user ise giriş yapan maili dönen diğer durumlarda giriş yapmamış olana verilen GUİD bilgisi bize dönecek.
		public string GetCartId(HttpContext context)
		{
			if (context.Session.GetString("SepetSessionKey") == null)
			{
				//login durumunda olma
				if (!string.IsNullOrWhiteSpace(context.User.Identity.Name))
				{
					var user = context.User;
					var idClaim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.PrimarySid);
					if (idClaim != null)
						context.Session.SetString("SepetSessionKey", idClaim.Value);
					else
						context.Session.SetString("SepetSessionKey", Guid.NewGuid().ToString());
				}
				else
				{
					//Henüz giriş yapmamış

					// Generate a new random GUID using System.Guid class
					// Guid tempCartId = Guid.NewGuid();
					// Send tempCartId back to client as a cookie

					context.Session.SetString("SepetSessionKey", Guid.NewGuid().ToString());
				}
			}
			return context.Session.GetString("SepetSessionKey");
		}
		// When a user has logged in, migrate their shopping cart to
		// be associated with their username
		//Kullanıcı login olduğu an, sepetindeki ürünleri alıp (sqlden) o userın mailiyle cart tablosundaki GUID bilgisini günceller.
		public void MigrateCart(string alisverisKullaniciId, int KullaniciId, HttpContext httpcontext)
		{
			var sepet = _context.Sepet.Where(
				c => c.KullaniciId == alisverisKullaniciId).ToList();
			if (sepet != null)
				AlisverisKullaniciId = KullaniciId.ToString();
			foreach (Sepet item in sepet)
			{
				item.KullaniciId = KullaniciId.ToString();
				_context.Sepet.Update(item);
			}
			httpcontext.Session.SetString("SepetSessionKey", KullaniciId.ToString());
			_context.SaveChanges();
		}

		public int GetCount()
		{
			// Get the count of each item in the cart and sum them up
			int? count = (from cartItems in _context.Sepet
						  where cartItems.KullaniciId == AlisverisKullaniciId
						  select (int?)cartItems.Adet).Sum();
			
			// Return 0 if all entries are null
			return count ?? 0;
		}

		public decimal GetTotal()
		{
			// Multiply album price by count of that album to get 
			// the current price for each of those albums in the cart
			// sum all album price totals to get the cart total
			decimal? total = (from cartItems in _context.Sepet
							  where cartItems.KullaniciId == AlisverisKullaniciId
							  select (int?)cartItems.Adet *
							  cartItems.Urun.UrunFiyat).Sum();

			return total ?? decimal.Zero;
		}

	}

}
