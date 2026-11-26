namespace KursoftRekabet.model
{
    /// <summary>
    /// Tarayıcıdan toplanan ham sayfa verisi. Ayrıştırma (parse) sadece bunu kullanır,
    /// böylece kayıtlı dosyalarla (Tanilama klasörü / test Ornekler klasörü) internetsiz test edilebilir.
    /// Tanilama.HamVeriKaydet / HamVeriYukle bu nesneyi klasöre yazar/okur.
    /// </summary>
    public class HamSayfaVerisi
    {
        public string IstenenLink { get; set; }
        public string SonUrl { get; set; }
        public int? HttpDurum { get; set; }
        public string SayfaBasligi { get; set; }
        public DateTime ToplanmaZamani { get; set; }

        /// <summary>Yakalanan JSON ağ cevapları (ürün anahtar kelimesi geçenler tam, diğerleri sadece özet).</summary>
        public List<AgCevabi> AgCevaplari { get; set; } = new List<AgCevabi>();

        /// <summary>window global adı -> JSON metni.</summary>
        public Dictionary<string, string> Globaller { get; set; } = new Dictionary<string, string>();

        /// <summary>Sitenin eklediği tüm global'lerin listesi (tanılama için: ad, uzunluk, ürün verisi var mı).</summary>
        public List<GlobalOzeti> GlobalListesi { get; set; } = new List<GlobalOzeti>();

        /// <summary>Script seçici/id -> JSON metni (script#reduxStore, script#__NEXT_DATA__, type=application/json).</summary>
        public Dictionary<string, string> ScriptJsonlari { get; set; } = new Dictionary<string, string>();

        /// <summary>script[type=application/ld+json] metinleri.</summary>
        public List<string> LdJsonlar { get; set; } = new List<string>();

        public string Html { get; set; }

        /// <summary>Diğer satıcılar gibi ek sayfaların/sekme sayfalarının HTML'leri (sayfalama).</summary>
        public List<string> EkHtmlSayfalari { get; set; } = new List<string>();

        public List<string> ToplamaNotlari { get; set; } = new List<string>();
    }

    public class AgCevabi
    {
        public string Url { get; set; }
        public int Durum { get; set; }
        public int Uzunluk { get; set; }
        public bool UrunVar { get; set; }

        /// <summary>Sadece UrunVar = true olanlarda dolu.</summary>
        public string Govde { get; set; }
    }

    public class GlobalOzeti
    {
        public string Ad { get; set; }
        public int Uzunluk { get; set; }
        public bool UrunVar { get; set; }
    }
}
