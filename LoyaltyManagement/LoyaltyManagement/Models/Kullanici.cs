using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LoyaltyManagement.Models
{
    public class Kullanici
    {
        public int KullaniciId { get; set; }
        public string Ad { get; set; }
        public string Soyad { get; set; }
        public string EPosta { get; set; }
        public string Sifre { get; set; }
        public string Adres { get; set; }
        public int RolId { get; set; }
        public bool AktifMi { get; set; }
        public virtual Rol Rol { get; set; }

    }
}
