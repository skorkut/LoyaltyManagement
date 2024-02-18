using LoyaltyManagement.Controllers;
using LoyaltyManagement.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace LoyaltyManagement
{
    public class MenuActionFilter: IActionFilter
    {
        private readonly IServiceProvider _serviceProvider;
        public MenuActionFilter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {

		}

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.User;
            var adClaim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
            var soyadClaim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname);
			var idClaim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.PrimarySid);
			using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LoyaltyManagementDbContext>();
            var controller = context.Controller as Controller;
            if (controller != null)
            {
                controller.ViewBag.MenuItems = GetMenuItems(dbContext);
                controller.ViewBag.CartCount = context.HttpContext.Session.GetString("adet");
                if (adClaim != null && soyadClaim != null)
                    controller.ViewBag.AktifKullaniciAdi = adClaim.Value + " " + soyadClaim.Value;
                else
                    controller.ViewBag.AktifKullaniciAdi = "";
                if (idClaim != null)
                {
                    controller.ViewBag.AktifKullaniciId = idClaim.Value;
                    int KullaniciId = idClaim.Value == "" ? 0 : Convert.ToInt16(idClaim.Value);
                    controller.ViewBag.CuzdanBakiye = CuzdanBakiyeGetir(dbContext, KullaniciId);
                }
                   
            }
        }
        public List<ViewModels.KategoriMarkaViewModel> GetMenuItems(LoyaltyManagementDbContext loyaltyContextDb)
        {
            IEnumerable<ViewModels.MarkaViewModel> markalarList = from marka in loyaltyContextDb.Markalar
                                                                  join kategorimarka in loyaltyContextDb.KategoriMarkalari on marka.MarkaId equals kategorimarka.MarkaId
                                                                  orderby marka.MarkaAdi
                                                                  select new ViewModels.MarkaViewModel
                                                                  {
                                                                      KategoriId = kategorimarka.KategoriId,
                                                                      MarkaAdi = marka.MarkaAdi,
                                                                      MarkaId = marka.MarkaId
                                                                  };

            List<ViewModels.KategoriMarkaViewModel> kategorivemarkalari = (from kategori in loyaltyContextDb.Kategoriler
                                                                           let markalistesi = markalarList.Where(x => x.KategoriId == kategori.KategoriId)
                                                                           orderby kategori.KategoriAdi
                                                                           select new ViewModels.KategoriMarkaViewModel
                                                                           {
                                                                               KategoriId = kategori.KategoriId,
                                                                               KategoriAdi = kategori.KategoriAdi,
                                                                               MarkaListe = markalistesi.ToList()
                                                                           }).ToList();
            return kategorivemarkalari;
        }
        public int CuzdanBakiyeGetir(LoyaltyManagementDbContext loyaltyContextDb, int kullaniciId)
        {
            int bakiye = 0;
            bakiye = (from cuzdan in loyaltyContextDb.Cuzdan
                      join kampanyakod in loyaltyContextDb.KampanyaKodlari on cuzdan.KampanyaKodId equals kampanyakod.KampanyaKodId
                      where cuzdan.KullaniciId == kullaniciId
                      select kampanyakod.Puan).Sum();
            return bakiye;
        }
    }
}
