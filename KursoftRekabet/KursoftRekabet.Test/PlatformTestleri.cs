using KursoftRekabet.Ayristirma;
using KursoftRekabet.model;
using Xunit;

namespace KursoftRekabet.Test
{
    internal static class OrnekYardimcisi
    {
        private static readonly object _kilit = new object();
        private static bool _hazir;

        public static void AyarlariYukle()
        {
            lock (_kilit)
            {
                if (_hazir) return;
                Logislemleri.Baslat(Globals.Ayarlar, false);
                SeciciAyarlari.Yukle(Path.Combine(AppContext.BaseDirectory, "secici-ayarlari.json"));
                _hazir = true;
            }
        }

        public static string Klasor(string platform, string ad)
        {
            return Path.Combine(AppContext.BaseDirectory, "Ornekler", platform, ad);
        }

        public static UrunBilgiModel Ayristir(Globals.ModulType modulType, string ornek)
        {
            AyarlariYukle();
            return UrunBilgiGetir.klasordenayristir(modulType, Klasor(Globals.ModulAdi(modulType), ornek));
        }

        /// <summary>Ornekler altındaki tüm klasörler (sonradan eklenen gerçek --tanila kayıtları dahil).</summary>
        public static IEnumerable<object[]> TumOrnekler()
        {
            string kok = Path.Combine(AppContext.BaseDirectory, "Ornekler");
            List<object[]> liste = new List<object[]>();
            if (!Directory.Exists(kok)) return liste;
            foreach (string platformKlasoru in Directory.GetDirectories(kok))
            {
                foreach (string ornek in Directory.GetDirectories(platformKlasoru))
                {
                    if (File.Exists(Path.Combine(ornek, "ozet.json"))) liste.Add(new object[] { Path.GetFileName(platformKlasoru), Path.GetFileName(ornek) });
                }
            }
            return liste;
        }
    }

    public class TrendyolTestleri
    {
        [Fact]
        public void NormalUrun()
        {
            UrunBilgiModel m = OrnekYardimcisi.Ayristir(Globals.ModulType.trendyol, "sentetik_normal");
            Assert.True(m.Basarili, m.HataKodu + " " + m.HataMesaji);
            Assert.Equal("Kablosuz Kulaklık Bluetooth 5.3", m.UrunAdi);
            Assert.Equal(1299.90m, m.SatisFiyati);
            Assert.Equal(1599.90m, m.ListeFiyati);
            Assert.Equal("1.299,90 TL", m.FiyatMetni);
            Assert.Equal(18.75m, m.IndirimOrani);
            Assert.Equal("TRY", m.ParaBirimi);
            Assert.Equal("SesDünyası", m.Satici.SaticiAdi);
            Assert.Equal(9.4m, m.Satici.SaticiPuani);
            Assert.Equal(10, m.Satici.PuanOlcegi);
            Assert.Equal("https://www.trendyol.com/magaza/sesdunyasi-m-9001", m.Satici.SaticiLinki);
            Assert.Equal("Sesmarka", m.Marka);
            Assert.Equal("123456789", m.PlatformUrunID);
            Assert.Equal("8690000000011", m.Barkod);
            Assert.Equal(new List<string> { "Elektronik", "Telefon Aksesuarları", "Kulaklık" }, m.KategoriYolu);
            Assert.Equal(3, m.Gorseller.Count);
            Assert.StartsWith("https://cdn.dsmcdn.com/ty1001/", m.Gorseller[0].Url);
            Assert.StartsWith("https://cdn.dsmcdn.com/mnresize/1200/1800/ty1001/", m.Gorseller[0].BuyukUrl);
            Assert.Equal(4.6m, m.UrunPuani);
            Assert.Equal(845, m.YorumSayisi);
            Assert.Equal(2, m.Ozellikler.Count);
            Assert.Contains("Sesmarka tarafından", m.Aciklama);
            Assert.True(m.KargoBedavaMi);
            Assert.True(m.StokVar);
            Assert.False(m.TukendiMi);
            Assert.Contains("2. Ürüne %10 İndirim", m.Kampanyalar);
            Assert.Equal(Globals.VeriKaynaklari.WINDOW_GLOBAL, m.VeriKaynagi);
            Assert.Equal("https://www.trendyol.com/sesmarka/kablosuz-kulaklik-p-123456789?boutiqueId=61&merchantId=9001", m.NormalizeLink);
            Assert.Empty(m.DigerSaticilar);
            Assert.Equal(1, m.ToplamSaticiSayisi);
        }

        [Fact]
        public void CokSaticiliUrunAgJsondanOkunur()
        {
            UrunBilgiModel m = OrnekYardimcisi.Ayristir(Globals.ModulType.trendyol, "sentetik_cok_satici");
            Assert.True(m.Basarili, m.HataMesaji);
            Assert.Equal(Globals.VeriKaynaklari.AG_JSON, m.VeriKaynagi);
            Assert.StartsWith("AG_JSON", m.AlanKaynaklari["SaticiAdi"]);
            // Ana satıcı (SesDünyası) diğer satıcılardan çıkarılır
            Assert.Equal(2, m.DigerSaticilar.Count);
            Assert.Equal(3, m.ToplamSaticiSayisi);
            Assert.Equal("KulaklıkMarket", m.DigerSaticilar[0].SaticiAdi);
            Assert.Equal(1249.00m, m.DigerSaticilar[0].SatisFiyati);
            Assert.Equal(8.7m, m.DigerSaticilar[0].SaticiPuani);
            Assert.False(m.DigerSaticilar[0].BuyboxMi);
            Assert.Equal(1249.00m, m.EnDusukFiyat);
            Assert.Equal("KulaklıkMarket", m.EnDusukFiyatliSatici);
        }

        [Fact]
        public void VaryantliUrun()
        {
            UrunBilgiModel m = OrnekYardimcisi.Ayristir(Globals.ModulType.trendyol, "sentetik_varyantli");
            Assert.True(m.Basarili, m.HataMesaji);
            Assert.Equal(3, m.Varyantlar.Count);
            Assert.Equal("Beden", m.Varyantlar[0].Ozellik);
            Assert.Equal("S", m.Varyantlar[0].Deger);
            Assert.False(m.Varyantlar[2].StokVar);
            Assert.Equal(419.99m, m.Varyantlar[2].Fiyat);
            Assert.NotNull(m.SeciliVaryant);
            Assert.Equal("M", m.SeciliVaryant.Deger);
        }

        [Fact]
        public void TukenmisUrunFiyatsizDaBasarili()
        {
            UrunBilgiModel m = OrnekYardimcisi.Ayristir(Globals.ModulType.trendyol, "sentetik_tukenmis");
            Assert.True(m.Basarili, m.HataMesaji);
            Assert.True(m.TukendiMi);
            Assert.False(m.StokVar);
            Assert.Null(m.SatisFiyati);
            Assert.Contains("ürün tükenmiş, satış fiyatı yok", m.Uyarilar);
        }

        [Fact]
        public void EngelTespitEdilir()
        {
            UrunBilgiModel m = OrnekYardimcisi.Ayristir(Globals.ModulType.trendyol, "sentetik_engel");
            Assert.False(m.Basarili);
            Assert.Equal(Globals.HataKodlari.ENGEL, m.HataKodu);
        }

        [Fact]
        public void YapiDegisinceAlarmVerir()
        {
            UrunBilgiModel m = OrnekYardimcisi.Ayristir(Globals.ModulType.trendyol, "sentetik_yapi_degisti");
            Assert.False(m.Basarili);
            Assert.Equal(Globals.HataKodlari.VERI_YAPISI_DEGISTI, m.HataKodu);
            Assert.Contains("UrunAdi", m.HataMesaji);
        }
    }

    public class HepsiburadaTestleri
    {
        [Fact]
        public void NormalUrun()
        {
            UrunBilgiModel m = OrnekYardimcisi.Ayristir(Globals.ModulType.hepsiburada, "sentetik_normal");
            Assert.True(m.Basarili, m.HataMesaji);
            Assert.Equal("Akıllı Saat Pro 2", m.UrunAdi);
            Assert.Equal(2499.00m, m.SatisFiyati);
            Assert.Equal("2.499,00 TL", m.FiyatMetni);
            Assert.Equal(2299.00m, m.SepetteFiyat);
            Assert.StartsWith("HTML", m.AlanKaynaklari["SepetteFiyat"]);
            Assert.Equal("SaatDünyası", m.Satici.SaticiAdi);
            Assert.Equal(9.6m, m.Satici.SaticiPuani);
            Assert.Equal("Saatçi", m.Marka);
            Assert.Equal("HBCV00000ABCDE", m.PlatformUrunID);
            Assert.Equal(2, m.Gorseller.Count);
            Assert.Equal("https://productimages.hepsiburada.net/s/777/1500/110000111.jpg", m.Gorseller[0].Url);
            Assert.Equal("https://productimages.hepsiburada.net/s/777/200/110000111.jpg", m.Gorseller[0].KucukUrl);
            Assert.Equal(new List<string> { "Elektronik", "Giyilebilir Teknoloji", "Akıllı Saat" }, m.KategoriYolu);
            Assert.Equal(2, m.Ozellikler.Count);
            Assert.Equal(Globals.VeriKaynaklari.SCRIPT_JSON, m.VeriKaynagi);
            Assert.Equal("https://www.hepsiburada.com/saatci-akilli-saat-pro-2-p-HBCV00000ABCDE?magaza=SaatDunyasi", m.NormalizeLink);
            Assert.Empty(m.DigerSaticilar);
        }

        [Fact]
        public void CokSaticiliUrun()
        {
            UrunBilgiModel m = OrnekYardimcisi.Ayristir(Globals.ModulType.hepsiburada, "sentetik_cok_satici");
            Assert.True(m.Basarili, m.HataMesaji);
            Assert.Equal(3, m.DigerSaticilar.Count);
            Assert.Equal(4, m.ToplamSaticiSayisi);
            Assert.Equal(2449.90m, m.EnDusukFiyat);
            Assert.Equal("TeknoKöşe", m.EnDusukFiyatliSatici);
            SaticiModel tk = m.DigerSaticilar[0];
            Assert.Equal("TeknoKöşe", tk.SaticiAdi);
            Assert.Equal("M2", tk.SaticiID);
            Assert.False(tk.KargoBedavaMi);
            Assert.Equal("Yarın kargoda", tk.TahminiTeslimat);
        }

        [Fact]
        public void VaryantliUrun()
        {
            UrunBilgiModel m = OrnekYardimcisi.Ayristir(Globals.ModulType.hepsiburada, "sentetik_varyantli");
            Assert.True(m.Basarili, m.HataMesaji);
            Assert.Equal(3, m.Varyantlar.Count);
            Assert.Equal("Renk", m.Varyantlar[1].Ozellik);
            Assert.Equal("Gümüş", m.Varyantlar[1].Deger);
            Assert.Equal("https://www.hepsiburada.com/saatci-akilli-saat-pro-2-p-HBCV00000ABCDF", m.Varyantlar[1].Link);
            Assert.False(m.Varyantlar[2].StokVar);
            Assert.Equal("Siyah", m.SeciliVaryant.Deger);
        }

        [Fact]
        public void TukenmisUrun()
        {
            UrunBilgiModel m = OrnekYardimcisi.Ayristir(Globals.ModulType.hepsiburada, "sentetik_tukenmis");
            Assert.True(m.Basarili, m.HataMesaji);
            Assert.False(m.StokVar);
            Assert.True(m.TukendiMi);
        }

        [Fact]
        public void GuvenlikSayfasiEngelDir()
        {
            UrunBilgiModel m = OrnekYardimcisi.Ayristir(Globals.ModulType.hepsiburada, "sentetik_engel");
            Assert.Equal(Globals.HataKodlari.ENGEL, m.HataKodu);
        }
    }

    public class N11Testleri
    {
        [Fact]
        public void NormalUrun()
        {
            UrunBilgiModel m = OrnekYardimcisi.Ayristir(Globals.ModulType.n11, "sentetik_normal");
            Assert.True(m.Basarili, m.HataMesaji);
            Assert.Equal("Mekanik Klavye RGB", m.UrunAdi);
            Assert.Equal(1899.00m, m.SatisFiyati);
            Assert.Equal(2199.00m, m.ListeFiyati);
            Assert.Equal("Klavyeci", m.Satici.SaticiAdi);
            Assert.Equal(98m, m.Satici.SaticiPuani);
            Assert.Equal("Tuşçu", m.Marka);
            Assert.Equal("12345678", m.PlatformUrunID);
            Assert.Equal(4.7m, m.UrunPuani);
            Assert.Equal(88, m.YorumSayisi);
            Assert.Equal(2, m.Gorseller.Count);
            Assert.Equal(new List<string> { "Elektronik", "Bilgisayar", "Klavye" }, m.KategoriYolu);
            Assert.Single(m.Ozellikler);
            Assert.True(m.StokVar);
            Assert.Equal("https://www.n11.com/urun/mekanik-klavye-rgb-12345678?magaza=klavyeci", m.NormalizeLink);
        }

        [Fact]
        public void CokSaticiliUrunSayfalamaDahil()
        {
            UrunBilgiModel m = OrnekYardimcisi.Ayristir(Globals.ModulType.n11, "sentetik_cok_satici");
            Assert.True(m.Basarili, m.HataMesaji);
            // 2 sayfa x kartlar, ana satıcı (Klavyeci) çıkarılır
            Assert.Equal(4, m.DigerSaticilar.Count);
            Assert.Equal(5, m.ToplamSaticiSayisi);
            Assert.Equal(1799.00m, m.EnDusukFiyat);
            Assert.Equal("BilgisayarCim", m.EnDusukFiyatliSatici);
            Assert.Equal("https://www.n11.com/magaza/tuşdünyası", m.DigerSaticilar[0].SaticiLinki);
        }

        [Fact]
        public void VaryantliUrun()
        {
            UrunBilgiModel m = OrnekYardimcisi.Ayristir(Globals.ModulType.n11, "sentetik_varyantli");
            Assert.True(m.Basarili, m.HataMesaji);
            Assert.Equal(3, m.Varyantlar.Count);
            Assert.Equal("Switch", m.Varyantlar[0].Ozellik);
            Assert.Equal("Kırmızı", m.SeciliVaryant.Deger);
        }

        [Fact]
        public void CloudflareEngel()
        {
            UrunBilgiModel m = OrnekYardimcisi.Ayristir(Globals.ModulType.n11, "sentetik_engel");
            Assert.Equal(Globals.HataKodlari.ENGEL, m.HataKodu);
        }
    }

    /// <summary>
    /// Ornekler/ altındaki HER klasör için genel kontroller. --tanila ile kaydedilen gerçek sayfa
    /// klasörlerini Ornekler/{platform}/ altına kopyalayınca otomatik test edilir.
    /// Klasör adı "engel", "yapi_degisti", "kaldirilmis" veya "bulunamadi" içeriyorsa başarısız olması beklenir.
    /// </summary>
    public class TumOrneklerTestleri
    {
        public static IEnumerable<object[]> Ornekler()
        {
            return OrnekYardimcisi.TumOrnekler();
        }

        [Theory]
        [MemberData(nameof(Ornekler))]
        public void OrnekAyristirilir(string platform, string ornek)
        {
            Globals.ModulType modul = Globals.ModulTypeCevir(platform);
            UrunBilgiModel m = OrnekYardimcisi.Ayristir(modul, ornek);
            bool hataBekleniyor = ornek.Contains("engel") || ornek.Contains("yapi_degisti") || ornek.Contains("kaldirilmis") || ornek.Contains("bulunamadi");
            if (hataBekleniyor)
            {
                Assert.False(m.Basarili);
                return;
            }

            Assert.True(m.Basarili, platform + "/" + ornek + ": " + m.HataKodu + " " + m.HataMesaji);
            Assert.False(string.IsNullOrWhiteSpace(m.UrunAdi));
            Assert.False(string.IsNullOrWhiteSpace(m.Satici.SaticiAdi));
            if (m.TukendiMi != true)
            {
                Assert.NotNull(m.SatisFiyati);
                Assert.True(m.SatisFiyati.Value > 0);
            }
            Assert.NotEmpty(m.Gorseller);
            foreach (GorselModel g in m.Gorseller) Assert.StartsWith("http", g.Url);
            foreach (SaticiModel s in m.DigerSaticilar) Assert.False(string.IsNullOrWhiteSpace(s.SaticiAdi));
            Assert.Equal(m.ToplamSaticiSayisi, 1 + m.DigerSaticilar.Count);
        }
    }
}
