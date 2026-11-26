using KursoftRekabet.Ayristirma;
using KursoftRekabet.model;

namespace KursoftRekabet.n11
{
    /// <summary>
    /// N11
    /// Veri yolları secici-ayarlari.json -> "n11" bölümünde. Okuma sırası:
    ///   1) Ağ cevapları / __NEXT_DATA__ / site global'leri (yeni arayüzde ürün JSON'u varsa)
    ///   2) ld+json Product (ad, fiyat, marka, görsel, puan, satıcı)
    ///   3) HTML (eski düzen: h1.proName, div.newPrice ins, a.unf-p-seller-name ...)
    /// Diğer satıcılar: "Diğer satıcılar" sekmesine tıklanır, sayfa sayfa dolaşılır (DigerSaticiSayfasi ayarı),
    /// her sayfanın HTML'i EkHtmlSayfalari'na eklenir ve satıcı kartları oradan okunur.
    /// </summary>
    public static class n11UrunBilgi
    {
        public static Task<UrunBilgiModel> Getir(string link)
        {
            return UrunBilgiAkisi.CalistirAsync(Globals.ModulType.n11, link, Ayristir);
        }

        public static void Ayristir(AlanOkuyucu oku, UrunBilgiModel model)
        {
            if (model.PlatformUrunID == null) model.PlatformUrunID = oku.Metin("LinktenUrunID");

            // HTML'den okunan satıcı adı bazen "Mağaza: XYZ" ya da puanla birlikte gelir
            model.Satici.SaticiAdi = SaticiAdiTemizle(model.Satici.SaticiAdi);
            foreach (SaticiModel s in model.DigerSaticilar) s.SaticiAdi = SaticiAdiTemizle(s.SaticiAdi);

            model.Satici.SaticiLinki = TamLink(model.Satici.SaticiLinki);
            foreach (SaticiModel s in model.DigerSaticilar) s.SaticiLinki = TamLink(s.SaticiLinki);
        }

        private static string SaticiAdiTemizle(string ad)
        {
            if (ad == null) return null;
            string t = ad.Trim();
            if (t.StartsWith("Mağaza:", StringComparison.OrdinalIgnoreCase)) t = t.Substring(7).Trim();
            return t.Length == 0 ? null : t;
        }

        private static string TamLink(string link)
        {
            if (string.IsNullOrWhiteSpace(link)) return link;
            if (link.StartsWith("//")) return "https:" + link;
            if (link.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return link;
            return "https://www.n11.com/" + link.TrimStart('/');
        }
    }
}
