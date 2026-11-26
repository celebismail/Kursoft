using KursoftRekabet.Ayristirma;
using KursoftRekabet.model;

namespace KursoftRekabet.Trendyol
{
    /// <summary>
    /// TRENDYOL
    /// Veri yolları secici-ayarlari.json -> "trendyol" bölümünde. Okuma sırası:
    ///   1) window.__envoy__PROPS içindeki ürün (product / merchantListing)
    ///   2) Ağ cevaplarında içinde "merchantListing" geçen JSON
    ///   3) Sitenin eklediği diğer global'ler (içinde "merchantListing" geçen)
    ///   4) Eski yapı: window.__PRODUCT_DETAIL_APP_INITIAL_STATE__.product
    ///   5) ld+json Product, en son HTML
    /// Diğer satıcılar merchantListing.otherMerchants içinde gelir; ayrı sayfaya gitmeye gerek yok.
    /// </summary>
    public static class trendyolUrunBilgi
    {
        public static Task<UrunBilgiModel> Getir(string link)
        {
            return UrunBilgiAkisi.CalistirAsync(Globals.ModulType.trendyol, link, Ayristir);
        }

        /// <summary>Ortak alanlar okunduktan sonra Trendyol'a özel düzeltmeler.</summary>
        public static void Ayristir(AlanOkuyucu oku, UrunBilgiModel model)
        {
            // Ürün ID'si JSON'da yoksa linkten: ...-p-123456789
            if (model.PlatformUrunID == null) model.PlatformUrunID = oku.Metin("LinktenUrunID");
            if (model.ContentID == null) model.ContentID = model.PlatformUrunID;

            // Kategori yolu liste olarak gelmediyse "Elektronik/Telefon/Cep Telefonu" metninden böl
            if (model.KategoriYolu.Count == 0)
            {
                string hiyerarsi = oku.Metin("KategoriHiyerarsi");
                if (hiyerarsi != null)
                {
                    foreach (string parca in hiyerarsi.Split('/'))
                    {
                        string p = parca.Trim();
                        if (p.Length > 0) model.KategoriYolu.Add(p);
                    }
                }
            }

            // Satıcı puanı 10 üzerinden (PuanOlcegi ayardan). Ürün puanı 5 üzerinden.

            // Satıcı linki göreli gelir: /magaza/xxx-m-123
            model.Satici.SaticiLinki = TamLink(model.Satici.SaticiLinki);
            foreach (SaticiModel s in model.DigerSaticilar) s.SaticiLinki = TamLink(s.SaticiLinki);
            foreach (VaryantModel v in model.Varyantlar) v.Link = TamLink(v.Link);

            // Trendyol "Sepette" fiyatı ayrı alan olarak verir; satış fiyatından farklı değilse boşalt (tekrarı önle)
            if (model.SepetteFiyat.HasValue && model.SatisFiyati.HasValue && model.SepetteFiyat.Value == model.SatisFiyati.Value)
            {
                model.SepetteFiyat = null;
                model.SepetteFiyatMetni = null;
            }
        }

        private static string TamLink(string link)
        {
            if (string.IsNullOrWhiteSpace(link)) return link;
            if (link.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return link;
            return "https://www.trendyol.com/" + link.TrimStart('/');
        }
    }
}
