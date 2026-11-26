using KursoftRekabet.Ayristirma;
using KursoftRekabet.model;

namespace KursoftRekabet.Hepsiburada
{
    /// <summary>
    /// HEPSİBURADA
    /// Veri yolları secici-ayarlari.json -> "hepsiburada" bölümünde. Okuma sırası:
    ///   1) script#reduxStore içindeki productState.product
    ///   2) Ağ cevaplarında ürün/listing verisi taşıyan JSON
    ///   3) ld+json Product
    ///   4) HTML (sepette fiyat sadece burada: data-test-id="checkout-price")
    /// Diğer satıcılar product.listings içinde gelir (ana satıcı da bu listede olabilir; ortak temizlik çıkarır).
    /// </summary>
    public static class hepsiburadaUrunBilgi
    {
        public static Task<UrunBilgiModel> Getir(string link)
        {
            return UrunBilgiAkisi.CalistirAsync(Globals.ModulType.hepsiburada, link, Ayristir);
        }

        public static void Ayristir(AlanOkuyucu oku, UrunBilgiModel model)
        {
            // Ürün kodu (HBC...) JSON'da yoksa linkten: ...-p-HBCV00000ABCDE
            if (model.PlatformUrunID == null) model.PlatformUrunID = oku.Metin("LinktenUrunID");

            // Ana satıcı bilgisi JSON'da ayrı yoksa listings içindeki ilk ilandan (sayfanın öne çıkardığı) tamamla
            if ((model.Satici.SaticiAdi == null || model.SatisFiyati == null) && model.DigerSaticilar.Count > 0)
            {
                SaticiModel ilk = model.DigerSaticilar[0];
                if (model.Satici.SaticiAdi == null)
                {
                    model.Satici.SaticiAdi = ilk.SaticiAdi;
                    model.Satici.SaticiID = ilk.SaticiID;
                    model.Satici.SaticiPuani = ilk.SaticiPuani;
                    model.Satici.SaticiPuaniMetni = ilk.SaticiPuaniMetni;
                    model.AlanKaynaklari["SaticiAdi"] = "DigerSaticilar[0]";
                    model.UyariEkle("ana satıcı ayrı alanda yok, ilk ilandan alındı");
                }
                if (model.SatisFiyati == null && ilk.SatisFiyati.HasValue)
                {
                    model.SatisFiyati = ilk.SatisFiyati;
                    model.FiyatMetni = ilk.FiyatMetni;
                    model.Satici.SatisFiyati = ilk.SatisFiyati;
                    model.Satici.FiyatMetni = ilk.FiyatMetni;
                    model.AlanKaynaklari["SatisFiyati"] = "DigerSaticilar[0]";
                }
            }

            foreach (SaticiModel s in model.DigerSaticilar) s.SaticiLinki = TamLink(s.SaticiLinki);
            model.Satici.SaticiLinki = TamLink(model.Satici.SaticiLinki);
            foreach (VaryantModel v in model.Varyantlar) v.Link = TamLink(v.Link);
        }

        private static string TamLink(string link)
        {
            if (string.IsNullOrWhiteSpace(link)) return link;
            if (link.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return link;
            return "https://www.hepsiburada.com/" + link.TrimStart('/');
        }
    }
}
