using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KursoftRekabet.Ayristirma
{
    /// <summary>
    /// secici-ayarlari.json dosyasını okur. Site değişince yeniden derlemeden bu dosya güncellenir.
    /// Dosya her çalışmada bir kere okunur.
    /// </summary>
    public static class SeciciAyarlari
    {
        private static Dictionary<string, PlatformAyari> _platformlar;
        private static string _yuklenenDosya;
        private static readonly object _kilit = new object();

        /// <summary>Ayar dosyasını yükler. Hatalıysa exception fırlatır (Program bunu exit code 4'e çevirir).</summary>
        public static void Yukle(string dosyaYolu)
        {
            if (!File.Exists(dosyaYolu)) throw new FileNotFoundException("Seçici ayar dosyası bulunamadı: " + dosyaYolu);

            string metin = File.ReadAllText(dosyaYolu);
            JObject kok = JObject.Parse(metin);
            Dictionary<string, PlatformAyari> liste = new Dictionary<string, PlatformAyari>(StringComparer.OrdinalIgnoreCase);
            foreach (JProperty p in kok.Properties())
            {
                if (p.Name.StartsWith("_")) continue;   // _aciklama vb.
                PlatformAyari ayar = p.Value.ToObject<PlatformAyari>();
                ayar.Ad = p.Name;
                ayar.ModulType = Globals.ModulTypeCevir(p.Name);
                liste[p.Name] = ayar;
            }

            lock (_kilit)
            {
                _platformlar = liste;
                _yuklenenDosya = dosyaYolu;
            }
        }

        private static void GerekirseYukle()
        {
            // Program açılışta Yukle() çağırır; çekirdek tek başına kullanılırsa exe yanındaki dosya okunur
            if (_platformlar != null) return;
            Yukle(Globals.SeciciAyarDosyasi);
        }

        public static string YuklenenDosya
        {
            get { return _yuklenenDosya; }
        }

        /// <summary>Platformun ayarı; yoksa null.</summary>
        public static PlatformAyari Platform(Globals.ModulType modulType)
        {
            GerekirseYukle();
            PlatformAyari ayar;
            if (_platformlar.TryGetValue(Globals.ModulAdi(modulType), out ayar)) return ayar;
            return null;
        }

        public static List<PlatformAyari> TumPlatformlar()
        {
            GerekirseYukle();
            return new List<PlatformAyari>(_platformlar.Values);
        }
    }

    public class PlatformAyari
    {
        [JsonIgnore] public string Ad { get; set; }
        [JsonIgnore] public Globals.ModulType ModulType { get; set; }

        // ---------- Link ----------
        public List<string> AlanAdlari { get; set; } = new List<string>();
        public List<string> KisaLinkAlanlari { get; set; } = new List<string>();

        /// <summary>Ürün sayfası linkinin yolunda (path) aranan regex. Ör. Trendyol "-p-\d+".</summary>
        public string UrunLinkDeseni { get; set; }

        /// <summary>Normalize ederken korunacak query parametreleri (geri kalanı takip parametresi sayılıp silinir).</summary>
        public List<string> KorunacakParametreler { get; set; } = new List<string>();

        // ---------- Sayfa ----------
        /// <summary>Ağ cevaplarında ve global'lerde ürün verisini tanıtan kelimeler (biri geçse yeter).</summary>
        public List<string> UrunAnahtarKelimeleri { get; set; } = new List<string>();

        /// <summary>Ürün verisinin geldiğini gösteren HTML seçicileri (JSON gelmeyen platformlar için).</summary>
        public List<string> HazirSecicileri { get; set; } = new List<string>();

        /// <summary>Veri okumayı engelleyen çerez/konum pop-up'larının kapatma butonları.</summary>
        public List<string> PopupKapatSecicileri { get; set; } = new List<string>();

        /// <summary>null ise genel MedyaEngelle ayarı geçerli.</summary>
        public bool? MedyaEngelle { get; set; }

        public int PuanOlcegi { get; set; } = 5;
        public string VarsayilanParaBirimi { get; set; } = "TRY";

        public GorselAyari Gorsel { get; set; } = new GorselAyari();

        public IsaretTanimi EngelIsaretleri { get; set; } = new IsaretTanimi();
        public IsaretTanimi BulunamadiIsaretleri { get; set; } = new IsaretTanimi();
        public IsaretTanimi KaldirilmisIsaretleri { get; set; } = new IsaretTanimi();

        public DigerSaticiSayfasiAyari DigerSaticiSayfasi { get; set; } = new DigerSaticiSayfasiAyari();

        // ---------- Veri yolları ----------
        public List<KaynakTanimi> Kaynaklar { get; set; } = new List<KaynakTanimi>();

        /// <summary>Alan adı -> sırayla denenecek adaylar.</summary>
        public Dictionary<string, List<AlanAdayi>> Alanlar { get; set; } = new Dictionary<string, List<AlanAdayi>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Liste adı (Gorseller, Varyantlar, DigerSaticilar...) -> liste tanımı.</summary>
        public Dictionary<string, ListeTanimi> Listeler { get; set; } = new Dictionary<string, ListeTanimi>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Platforma özel serbest ayarlar (kod içinde adıyla okunur).</summary>
        public Dictionary<string, string> Ozel { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string OzelDeger(string anahtar)
        {
            string d;
            if (Ozel != null && Ozel.TryGetValue(anahtar, out d)) return d;
            return null;
        }
    }

    /// <summary>
    /// Verinin okunacağı kök. Tur: AG_JSON, WINDOW_GLOBAL, SCRIPT_JSON, LD_JSON, HTML, URL
    /// </summary>
    public class KaynakTanimi
    {
        public string Ad { get; set; }
        public string Tur { get; set; }

        /// <summary>WINDOW_GLOBAL: global adı ("*" = sitenin eklediği, ürün kelimesi geçen tüm global'ler).</summary>
        public string Global { get; set; }

        /// <summary>SCRIPT_JSON: script seçicisi ("script#reduxStore"). "*" = toplanan tüm script JSON'ları.</summary>
        public string Secici { get; set; }

        /// <summary>AG_JSON: URL'de geçmesi gereken parçalar (biri yeter). Boşsa hepsi.</summary>
        public List<string> UrlIcerir { get; set; } = new List<string>();

        /// <summary>AG_JSON: cevap gövdesinde geçmesi gereken kelimeler (hepsi). Boşsa platformun UrunAnahtarKelimeleri.</summary>
        public List<string> IcerikIcerir { get; set; } = new List<string>();

        /// <summary>Kökü bulmak için sırayla denenecek yollar ("props.product", "productState.product").</summary>
        public List<string> KokYollari { get; set; } = new List<string>();

        /// <summary>Yol tutmazsa: bu anahtarların hepsine sahip ilk nesne kök kabul edilir (derin arama).</summary>
        public List<string> KokAnahtarlari { get; set; } = new List<string>();

        /// <summary>LD_JSON: @type filtresi (Product, Offer, BreadcrumbList...).</summary>
        public string LdTip { get; set; }

        /// <summary>HTML: true ise diğer satıcı sayfaları (EkHtmlSayfalari) de kök olarak eklenir.</summary>
        public bool EkSayfalar { get; set; }
    }

    public class AlanAdayi
    {
        /// <summary>KaynakTanimi.Ad. Liste alt alanlarında boş bırakılır (eleman kendisi kaynaktır).</summary>
        public string Kaynak { get; set; }

        /// <summary>JSON kaynaklarında yol ("$" = kökün kendisi).</summary>
        public string Yol { get; set; }

        /// <summary>HTML kaynaklarında CSS seçici (boş = elemanın kendisi).</summary>
        public string Secici { get; set; }

        /// <summary>HTML: okunacak nitelik. Boş = metin. "html" = iç HTML. "var" = eleman varsa "true".</summary>
        public string Nitelik { get; set; }

        /// <summary>Değerden parça almak için regex (1. grup ya da tüm eşleşme).</summary>
        public string Desen { get; set; }

        /// <summary>true: tüm eşleşmeleri alt alta birleştirir (açıklama gibi parçalı alanlar).</summary>
        public bool Birlestir { get; set; }

        /// <summary>Bool alanlarda: değer bunlardan birini içeriyorsa true, içermiyorsa false.</summary>
        public List<string> DogruDegerler { get; set; }

        /// <summary>Bool alanlarda değer bu listedekilerden birini içeriyorsa false.</summary>
        public List<string> YanlisDegerler { get; set; }

        /// <summary>Seçici/yol bulunamazsa yazılacak değer ("var" ile birlikte "false" için). Boşsa sonraki adaya geçilir.</summary>
        public string YoksaDeger { get; set; }

        /// <summary>Listelerde: true ise kaynağın tüm köklerindeki elemanlar birleştirilir (sayfalanmış HTML'ler).</summary>
        public bool TumKokler { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Yol)) return (Kaynak ?? "") + ":" + Yol;
            return (Kaynak ?? "") + ":" + (Secici ?? "") + (string.IsNullOrEmpty(Nitelik) ? "" : "@" + Nitelik);
        }
    }

    public class ListeTanimi
    {
        /// <summary>Listeyi veren adaylar (ilk boş olmayan kazanır).</summary>
        public List<AlanAdayi> Adaylar { get; set; } = new List<AlanAdayi>();

        /// <summary>Elemanın alanları: alan adı -> adaylar (yol/seçici elemana göre).</summary>
        public Dictionary<string, List<AlanAdayi>> Alanlar { get; set; } = new Dictionary<string, List<AlanAdayi>>(StringComparer.OrdinalIgnoreCase);

        public int MaksimumEleman { get; set; } = 500;
    }

    public class IsaretTanimi
    {
        public List<int> HttpDurumlari { get; set; } = new List<int>();
        public List<string> BaslikIcerir { get; set; } = new List<string>();

        /// <summary>HTML'de aranacak metin parçaları (küçük/büyük harf duyarsız).</summary>
        public List<string> HtmlIcerir { get; set; } = new List<string>();
        public List<string> UrlIcerir { get; set; } = new List<string>();
    }

    public class GorselAyari
    {
        /// <summary>Göreli görsel yollarının önüne eklenir ("/ty123/..." -> "https://cdn.dsmcdn.com/ty123/...").</summary>
        public string OnEk { get; set; }

        /// <summary>URL içindeki boyut yer tutucusu (Hepsiburada "{size}").</summary>
        public string YerTutucu { get; set; }
        public string NormalBoyut { get; set; }
        public string BuyukBoyut { get; set; }
        public string KucukBoyut { get; set; }

        /// <summary>Görsel yolu (OnEk'ten sonraki kısım, baştaki / olmadan) {yol} ile yerleştirilir.</summary>
        public string BuyukSablon { get; set; }
        public string KucukSablon { get; set; }
    }

    public class DigerSaticiSayfasiAyari
    {
        public bool Acik { get; set; }

        /// <summary>Diğer satıcıları açan sekme/buton (sayfa içinde).</summary>
        public string TiklanacakSecici { get; set; }

        /// <summary>Doluysa ayrı sayfaya gidilir. {url} = normalize ürün linki (query'siz).</summary>
        public string UrlSablonu { get; set; }

        /// <summary>Satıcı listesinin geldiğini gösteren seçici.</summary>
        public string BeklenecekSecici { get; set; }

        /// <summary>Sonraki sayfa butonu. Boşsa sayfalama yok.</summary>
        public string SonrakiSecici { get; set; }

        /// <summary>Sonraki butonu bu class'ı taşıyorsa son sayfadayız.</summary>
        public string PasifSinifIcerir { get; set; } = "disable";

        public int MaksimumSayfa { get; set; } = 20;
        public int SayfaBeklemeMs { get; set; } = 1500;
    }
}
