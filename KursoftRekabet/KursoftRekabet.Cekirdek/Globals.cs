using System.Reflection;

namespace KursoftRekabet
{
    /// <summary>
    /// Ortak sabitler, enum'lar ve çalışma boyunca geçerli değerler.
    /// </summary>
    public static class Globals
    {
        // Yeni pazaryeri eklerken: buraya bir değer + secici-ayarlari.json'a bir bölüm + kendi klasörü.
        public enum ModulType
        {
            bilinmiyor = 0,
            trendyol = 1,
            hepsiburada = 2,
            n11 = 3,
            pazarama = 4,
            ciceksepeti = 5,
            idefix = 6,
            amazontr = 7,
            koctas = 8
        }

        public static bool TrendyolMu(ModulType modulType) { return modulType == ModulType.trendyol; }
        public static bool HepsiburadaMu(ModulType modulType) { return modulType == ModulType.hepsiburada; }
        public static bool N11Mi(ModulType modulType) { return modulType == ModulType.n11; }

        // Faz 1'de kodu yazılmış platformlar
        public static bool DesteklenenMi(ModulType modulType)
        {
            return TrendyolMu(modulType) || HepsiburadaMu(modulType) || N11Mi(modulType);
        }

        /// <summary>secici-ayarlari.json'daki bölüm adı ve log dosyası adı.</summary>
        public static string ModulAdi(ModulType modulType)
        {
            return modulType.ToString();
        }

        /// <summary>"trendyol", "Trendyol", "1" gibi DB'den gelen değeri enum'a çevirir. Çeviremezse bilinmiyor.</summary>
        public static ModulType ModulTypeCevir(string deger)
        {
            if (string.IsNullOrWhiteSpace(deger)) return ModulType.bilinmiyor;
            ModulType sonuc;
            if (Enum.TryParse(deger.Trim(), true, out sonuc) && Enum.IsDefined(typeof(ModulType), sonuc)) return sonuc;
            return ModulType.bilinmiyor;
        }

        // ---------- Hata kodları (UrunBilgiModel.HataKodu) ----------
        public static class HataKodlari
        {
            public const string GECERSIZ_LINK = "GECERSIZ_LINK";
            public const string BULUNAMADI = "BULUNAMADI";
            public const string KALDIRILMIS = "KALDIRILMIS";
            public const string ENGEL = "ENGEL";
            public const string ZAMAN_ASIMI = "ZAMAN_ASIMI";
            public const string VERI_YAPISI_DEGISTI = "VERI_YAPISI_DEGISTI";
            public const string BILINMEYEN = "BILINMEYEN";

            // Tekrar denemenin anlamsız olduğu hatalar
            public static bool KaliciMi(string hataKodu)
            {
                return hataKodu == GECERSIZ_LINK || hataKodu == BULUNAMADI || hataKodu == KALDIRILMIS || hataKodu == VERI_YAPISI_DEGISTI;
            }
        }

        // ---------- Veri kaynakları (UrunBilgiModel.VeriKaynagi / AlanKaynaklari) ----------
        public static class VeriKaynaklari
        {
            public const string AG_JSON = "AG_JSON";
            public const string WINDOW_GLOBAL = "WINDOW_GLOBAL";
            public const string SCRIPT_JSON = "SCRIPT_JSON";
            public const string LD_JSON = "LD_JSON";
            public const string HTML = "HTML";
        }

        // ---------- Çalışma boyunca sabit değerler ----------

        /// <summary>Bu exe çalışmasının kimliği. Program açılışta bir kere üretir.</summary>
        public static string CalismaID = Guid.NewGuid().ToString("N").Substring(0, 12);

        /// <summary>Zamanlanmış görev System32'den başlatabilir; her yol buna göre kurulur.</summary>
        public static string AnaKlasor
        {
            get { return AppContext.BaseDirectory; }
        }

        public static string LogKlasoru
        {
            get { return Path.Combine(AnaKlasor, "Logs"); }
        }

        public static string TarayiciKlasoru
        {
            get { return Path.Combine(AnaKlasor, "tarayicilar"); }
        }

        public static string SeciciAyarDosyasi
        {
            get { return Path.Combine(AnaKlasor, "secici-ayarlari.json"); }
        }

        public static string UygulamaSurumu
        {
            get
            {
                Assembly asm = Assembly.GetEntryAssembly() ?? typeof(Globals).Assembly;
                Version v = asm.GetName().Version;
                return v == null ? "0.0.0" : v.ToString(3);
            }
        }

        /// <summary>Çekirdeğin kullandığı çalışma ayarları. Program açılışta appsettings.json'dan doldurur.</summary>
        public static CalismaAyarlari Ayarlar = new CalismaAyarlari();
    }

    /// <summary>
    /// Çekirdeğin ihtiyaç duyduğu ayarlar (appsettings.json içindeki karşılıkları aynı adla).
    /// DB ile ilgili olanlar Gorev projesinde.
    /// </summary>
    public class CalismaAyarlari
    {
        public int MaksimumEszamanliSayfa { get; set; } = 2;
        public int TekrarDenemeSayisi { get; set; } = 2;
        public int TekrarBeklemeMs { get; set; } = 3000;
        public int LinkZamanAsimiSn { get; set; } = 90;
        public int SayfaYuklemeZamanAsimiSn { get; set; } = 45;
        public int VeriBeklemeSn { get; set; } = 15;

        /// <summary>Platform başına istekler arası bekleme (ms). Anahtar: trendyol, hepsiburada, n11 ...</summary>
        public Dictionary<string, int> PlatformBeklemeMs { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public int VarsayilanPlatformBeklemeMs { get; set; } = 3000;

        /// <summary>ENGEL gelince bekleme kaç katına çıkabilir.</summary>
        public int MaksimumEngelCarpani { get; set; } = 16;

        public bool MedyaEngelle { get; set; } = true;
        public bool HamJsonDondur { get; set; } = false;
        public bool DetayLog { get; set; } = false;
        public int LogSaklamaGun { get; set; } = 30;
        public int LogDosyaMaksimumMb { get; set; } = 50;

        /// <summary>Başarısız işlemlerde ham veri Tanilama klasörüne kaydedilsin mi (VERI_YAPISI_DEGISTI her zaman kaydedilir).</summary>
        public bool HatadaTanilamaKaydet { get; set; } = true;

        /// <summary>--tanila modunda true: her işlemin ham verisi kaydedilir.</summary>
        public bool HerZamanTanilamaKaydet { get; set; } = false;

        public bool Headless { get; set; } = true;

        /// <summary>Boşsa Playwright'ın kurduğu Chromium kullanılır. Geliştirme için başka bir chromium yolu verilebilir.</summary>
        public string TarayiciYolu { get; set; } = "";

        /// <summary>Boşsa tarayıcının kendi user-agent'ı kullanılır.</summary>
        public string KullaniciAjani { get; set; } = "";

        public int MaksimumDigerSaticiSayfasi { get; set; } = 20;
    }
}
