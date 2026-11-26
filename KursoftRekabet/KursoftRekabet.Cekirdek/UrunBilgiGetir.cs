using KursoftRekabet.Ayristirma;
using KursoftRekabet.Hepsiburada;
using KursoftRekabet.model;
using KursoftRekabet.n11;
using KursoftRekabet.Trendyol;
using KursoftRekabet.Yardimcilar;

namespace KursoftRekabet
{
    /// <summary>
    /// Dışarıya açılan giriş noktaları. Hepsi her durumda dolu bir model döner, exception fırlatmaz.
    ///
    ///   using static KursoftRekabet.UrunBilgiGetir;
    ///   UrunBilgiModel sonuc = await trendyolurunbilgigetir(link);
    /// </summary>
    public static class UrunBilgiGetir
    {
        public static Task<UrunBilgiModel> trendyolurunbilgigetir(string link)
        {
            return trendyolUrunBilgi.Getir(link);
        }

        public static Task<UrunBilgiModel> hepsiburadaurunbilgigetir(string link)
        {
            return hepsiburadaUrunBilgi.Getir(link);
        }

        public static Task<UrunBilgiModel> n11urunbilgigetir(string link)
        {
            return n11UrunBilgi.Getir(link);
        }

        /// <summary>Platformu linkin alan adından kendisi bulur.</summary>
        public static Task<UrunBilgiModel> urunbilgigetir(string link)
        {
            Globals.ModulType modulType;
            try
            {
                modulType = LinkYardimcisi.PlatformBul(link);
            }
            catch (Exception ex)
            {
                Logislemleri.Hata("PLATFORM_BUL", "Platform bulunamadı", ex);
                modulType = Globals.ModulType.bilinmiyor;
            }
            return urunbilgigetir(link, modulType);
        }

        /// <summary>Platform açıkça verildiğinde (ör. DB'den ModulType geldiyse).</summary>
        public static Task<UrunBilgiModel> urunbilgigetir(string link, Globals.ModulType modulType)
        {
            switch (modulType)
            {
                case Globals.ModulType.trendyol: return trendyolurunbilgigetir(link);
                case Globals.ModulType.hepsiburada: return hepsiburadaurunbilgigetir(link);
                case Globals.ModulType.n11: return n11urunbilgigetir(link);
            }

            UrunBilgiModel m = UrunBilgiAkisi.YeniModel(modulType, link, Guid.NewGuid());
            if (modulType == Globals.ModulType.bilinmiyor) m.HataYaz(Globals.HataKodlari.GECERSIZ_LINK, "Link desteklenen bir pazaryerine ait değil");
            else m.HataYaz(Globals.HataKodlari.GECERSIZ_LINK, Globals.ModulAdi(modulType) + " henüz desteklenmiyor");
            using (Logislemleri.IslemBaglamiAc(m))
            {
                Logislemleri.Uyari("BITTI", "Başarısız | " + m.HataKodu + " | " + m.HataMesaji);
            }
            return Task.FromResult(m);
        }

        /// <summary>
        /// Kayıtlı ham veriden (Tanilama klasörü ya da test Ornekler klasörü) ayrıştırma. Tarayıcı açmaz.
        /// </summary>
        public static UrunBilgiModel hamveridenayristir(Globals.ModulType modulType, HamSayfaVerisi ham)
        {
            switch (modulType)
            {
                case Globals.ModulType.trendyol: return UrunBilgiAkisi.HamVeridenModel(modulType, ham, trendyolUrunBilgi.Ayristir);
                case Globals.ModulType.hepsiburada: return UrunBilgiAkisi.HamVeridenModel(modulType, ham, hepsiburadaUrunBilgi.Ayristir);
                case Globals.ModulType.n11: return UrunBilgiAkisi.HamVeridenModel(modulType, ham, n11UrunBilgi.Ayristir);
            }
            UrunBilgiModel m = UrunBilgiAkisi.YeniModel(modulType, ham != null ? ham.IstenenLink : null, Guid.NewGuid());
            m.HataYaz(Globals.HataKodlari.GECERSIZ_LINK, Globals.ModulAdi(modulType) + " desteklenmiyor");
            return m;
        }

        public static UrunBilgiModel klasordenayristir(Globals.ModulType modulType, string klasor)
        {
            return hamveridenayristir(modulType, Tanilama.HamVeriYukle(klasor));
        }
    }
}
