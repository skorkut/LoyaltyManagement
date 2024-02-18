using System.Net.Mail;
using System.Net;

namespace LoyaltyManagement.Models
{
	public class Mail
	{
		public bool MailAt(string eposta, string ad, string soyad, string konu, string icerik)
		{
			try
			{

				MailMessage mail = new MailMessage();
				mail.From = new MailAddress("loyaltymanagement4@gmail.com", "Serkan Korkut");
				mail.Priority = MailPriority.High;

				mail.Subject = konu;
				mail.To.Add(new MailAddress(eposta, ad + " " + soyad));
				mail.Bcc.Add(new MailAddress("loyaltymanagement4@gmail.com", "Serkan Korkut"));
				// mail.Body = ad+ " "+soyad+" adındaki kişilerin bilgileri alınmıştır.";
				mail.Body = icerik;
				mail.IsBodyHtml = true;

				SmtpClient client = new SmtpClient("smtp.gmail.com", 587);
				NetworkCredential girisIzni = new NetworkCredential("loyaltymanagement4@gmail.com", "jicq hbds oxew sdsk");
				client.EnableSsl = true;
				client.Credentials = girisIzni;
				client.Send(mail);
				return true;

			}
			catch
			{

				return false;
			}
		}
	}
}
