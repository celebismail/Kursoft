using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace KursoftRekabet.model
{
    /// <summary>
    /// xxxurunbilgigetir(link) fonksiyonlarının döndüğü ortak model.
    /// Bulunamayan alan null kalır; uydurma değer yazılmaz.
    /// </summary>
    public class UrunBilgiModel
    {
        // ---------- İşlem bilgisi ----------
        public Guid IslemID { get; set; } = Guid.NewGuid();
        public string CalismaID { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Globals.ModulType ModulType { get; set; }

        public string Link { get; set; }
        public string NormalizeLink { get; set; }
        public DateTime IstekZamani { get; set; }
        public long SureMs { get; set; }
        public int DenemeSayisi { get; set; }

        public bool Basarili { get; set; }
        public string HataKodu { get; set; }
        public string HataMesaji { get; set; }
        public List<string> Uyarilar { get; set; } = new List<string>();

        /// <summary>Zorunlu alanları veren ana kaynak türü (AG_JSON, WINDOW_GLOBAL, SCRIPT_JSON, LD_JSON, HTML).</summary>
        public string VeriKaynagi { get; set; }

        /// <summary>Alan adı -> "KAYNAK_TURU:kaynakAdi:yol". Hangi alanın nereden okunduğu.</summary>
        public Dictionary<string, string> AlanKaynaklari { get; set; } = new Dictionary<string, string>();

        /// <summary>Hata olduysa tanılama klasörünün yolu.</summary>
        public string TanilamaKlasoru { get; set; }

        // ---------- Ürün ----------
        public string PlatformUrunID { get; set; }
        public string ContentID { get; set; }
        public string ListingID { get; set; }
        public string MerchantSku { get; set; }
        public string Barkod { get; set; }
        public string GTIN { get; set; }
        public string ModelKodu { get; set; }

        public string UrunAdi { get; set; }
        public string Marka { get; set; }
        public List<string> KategoriYolu { get; set; } = new List<string>();
        public string KategoriID { get; set; }

        public string Aciklama { get; set; }
        public List<OzellikModel> Ozellikler { get; set; } = new List<OzellikModel>();

        public List<GorselModel> Gorseller { get; set; } = new List<GorselModel>();
        public string VideoUrl { get; set; }

        public decimal? UrunPuani { get; set; }
        public int? YorumSayisi { get; set; }
        public int? DegerlendirmeSayisi { get; set; }
        public int? SoruSayisi { get; set; }
        public int? FavoriSayisi { get; set; }
        public string SatisAdediMetni { get; set; }

        public List<VaryantModel> Varyantlar { get; set; } = new List<VaryantModel>();
        public VaryantModel SeciliVaryant { get; set; }

        // ---------- Fiyat (ana satıcı / buybox) ----------
        public string FiyatMetni { get; set; }
        public decimal? SatisFiyati { get; set; }
        public string ListeFiyatiMetni { get; set; }
        public decimal? ListeFiyati { get; set; }
        public string SepetteFiyatMetni { get; set; }
        public decimal? SepetteFiyat { get; set; }
        public decimal? IndirimOrani { get; set; }
        public string ParaBirimi { get; set; }

        public List<string> Kampanyalar { get; set; } = new List<string>();
        public string KuponMetni { get; set; }
        public string TaksitBilgisi { get; set; }

        public string KargoUcretiMetni { get; set; }
        public decimal? KargoUcreti { get; set; }
        public bool? KargoBedavaMi { get; set; }
        public string TahminiTeslimat { get; set; }
        public bool? HizliTeslimatMi { get; set; }

        public bool? StokVar { get; set; }
        public bool? TukendiMi { get; set; }
        public int? StokAdedi { get; set; }
        public int? MaksimumSatisAdedi { get; set; }

        // ---------- Satıcı ----------
        public SaticiModel Satici { get; set; } = new SaticiModel();
        public List<SaticiModel> DigerSaticilar { get; set; } = new List<SaticiModel>();
        public int? ToplamSaticiSayisi { get; set; }
        public decimal? EnDusukFiyat { get; set; }
        public string EnDusukFiyatliSatici { get; set; }

        // ---------- Ham veri ----------
        /// <summary>Sadece ayarda HamJsonDondur = true ise dolar.</summary>
        public string HamJson { get; set; }

        public void UyariEkle(string uyari)
        {
            if (string.IsNullOrWhiteSpace(uyari)) return;
            if (!Uyarilar.Contains(uyari)) Uyarilar.Add(uyari);
        }

        public void HataYaz(string hataKodu, string hataMesaji)
        {
            Basarili = false;
            HataKodu = hataKodu;
            HataMesaji = hataMesaji;
        }
    }
}
