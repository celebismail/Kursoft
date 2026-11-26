using KursoftRekabet.model;

namespace KursoftRekabet.Ayristirma
{
    /// <summary>
    /// Sayfanın bot engeli, 404 ya da kaldırılmış ürün sayfası olup olmadığını secici-ayarlari.json'daki
    /// işaretlere göre anlar. Engel tespit edilirse atlatmaya çalışılmaz, sadece raporlanır.
    /// </summary>
    public static class DurumTespiti
    {
        /// <summary>
        /// Kesin işaretler (HTTP durumu, sayfa başlığı, URL). Ayrıştırmadan önce bakılır.
        /// Dönen değer hata kodu ya da null.
        /// </summary>
        public static string KesinDurum(HamSayfaVerisi ham, PlatformAyari ayar, out string aciklama)
        {
            aciklama = null;
            if (Eslesir(ham, ayar.EngelIsaretleri, false, out aciklama)) return Globals.HataKodlari.ENGEL;
            if (Eslesir(ham, ayar.BulunamadiIsaretleri, false, out aciklama)) return Globals.HataKodlari.BULUNAMADI;
            return null;
        }

        /// <summary>
        /// HTML metni işaretleri. Normal sayfada da "captcha", "satışta değil" gibi metinler script içinde
        /// geçebildiği için sadece zorunlu alanlar okunamadığında bakılır (yanlış alarm olmasın).
        /// </summary>
        public static string HtmlDurumu(HamSayfaVerisi ham, PlatformAyari ayar, bool zorunluAlanlarEksik, out string aciklama)
        {
            aciklama = null;
            if (!zorunluAlanlarEksik) return null;
            if (Eslesir(ham, ayar.KaldirilmisIsaretleri, false, out aciklama)) return Globals.HataKodlari.KALDIRILMIS;
            if (Eslesir(ham, ayar.KaldirilmisIsaretleri, true, out aciklama)) return Globals.HataKodlari.KALDIRILMIS;
            if (Eslesir(ham, ayar.EngelIsaretleri, true, out aciklama)) return Globals.HataKodlari.ENGEL;
            if (Eslesir(ham, ayar.BulunamadiIsaretleri, true, out aciklama)) return Globals.HataKodlari.BULUNAMADI;
            return null;
        }

        private static bool Eslesir(HamSayfaVerisi ham, IsaretTanimi isaret, bool htmlBak, out string aciklama)
        {
            aciklama = null;
            if (isaret == null) return false;

            if (!htmlBak)
            {
                if (ham.HttpDurum.HasValue && isaret.HttpDurumlari != null && isaret.HttpDurumlari.Contains(ham.HttpDurum.Value))
                {
                    aciklama = "HTTP " + ham.HttpDurum.Value;
                    return true;
                }
                if (!string.IsNullOrEmpty(ham.SayfaBasligi) && isaret.BaslikIcerir != null)
                {
                    foreach (string s in isaret.BaslikIcerir)
                    {
                        if (ham.SayfaBasligi.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            aciklama = "sayfa başlığı: '" + ham.SayfaBasligi + "'";
                            return true;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(ham.SonUrl) && isaret.UrlIcerir != null)
                {
                    foreach (string s in isaret.UrlIcerir)
                    {
                        if (ham.SonUrl.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            aciklama = "yönlenen adres: " + ham.SonUrl;
                            return true;
                        }
                    }
                }
                return false;
            }

            if (!string.IsNullOrEmpty(ham.Html) && isaret.HtmlIcerir != null)
            {
                foreach (string s in isaret.HtmlIcerir)
                {
                    if (ham.Html.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        aciklama = "sayfada '" + s + "' geçiyor";
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
