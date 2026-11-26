using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using KursoftRekabet.model;
using KursoftRekabet.Yardimcilar;
using Newtonsoft.Json.Linq;

namespace KursoftRekabet.Ayristirma
{
    /// <summary>
    /// Ham sayfa verisinden, secici-ayarlari.json'daki adaylara göre alan okur.
    /// Her alan için adaylar sırayla denenir; ilk değer veren kazanır ve hangi kaynaktan geldiği
    /// model.AlanKaynaklari'na yazılır. Tarayıcıya ihtiyaç duymaz (kayıtlı dosyalarla da çalışır).
    /// </summary>
    public class AlanOkuyucu
    {
        private readonly HamSayfaVerisi _ham;
        private readonly PlatformAyari _ayar;
        private readonly UrunBilgiModel _model;

        // Kaynak adı -> o kaynağın kökleri (ilk istendiğinde hesaplanır)
        private readonly Dictionary<string, List<Dugum>> _kokler = new Dictionary<string, List<Dugum>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, KaynakTanimi> _kaynaklar = new Dictionary<string, KaynakTanimi>(StringComparer.OrdinalIgnoreCase);

        private IDocument _htmlBelge;
        private List<IDocument> _ekBelgeler;
        private static readonly HtmlParser _htmlParser = new HtmlParser();

        public AlanOkuyucu(HamSayfaVerisi ham, PlatformAyari ayar, UrunBilgiModel model)
        {
            _ham = ham;
            _ayar = ayar;
            _model = model;
            if (ayar.Kaynaklar != null)
            {
                foreach (KaynakTanimi k in ayar.Kaynaklar)
                {
                    if (!string.IsNullOrEmpty(k.Ad)) _kaynaklar[k.Ad] = k;
                }
            }
        }

        public PlatformAyari Ayar
        {
            get { return _ayar; }
        }

        public HamSayfaVerisi Ham
        {
            get { return _ham; }
        }

        // ---------- Tekil alanlar ----------

        /// <summary>Alanın ham değeri (metin + JSON token). Hiçbir aday tutmazsa null.</summary>
        public OkunanDeger Oku(string alanAdi)
        {
            List<AlanAdayi> adaylar;
            if (_ayar.Alanlar == null || !_ayar.Alanlar.TryGetValue(alanAdi, out adaylar) || adaylar == null || adaylar.Count == 0)
            {
                return null;
            }

            foreach (AlanAdayi aday in adaylar)
            {
                List<Dugum> kokler = KokleriAl(aday.Kaynak);
                if (kokler.Count == 0)
                {
                    Logislemleri.Detay("ALAN_ADAY", alanAdi + " <- " + aday + " : kaynak yok");
                    continue;
                }

                foreach (Dugum kok in kokler)
                {
                    OkunanDeger d = AdayOku(kok, aday);
                    if (d != null)
                    {
                        d.KaynakTuru = kok.KaynakTuru;
                        d.Aciklama = kok.KaynakTuru + ":" + aday;
                        _model.AlanKaynaklari[alanAdi] = d.Aciklama;
                        Logislemleri.Detay("ALAN_OKUNDU", alanAdi + " <- " + d.Aciklama + " = " + Kisalt(d.Metin));
                        return d;
                    }
                }
                Logislemleri.Detay("ALAN_ADAY", alanAdi + " <- " + aday + " : boş");
            }
            return null;
        }

        public string Metin(string alanAdi)
        {
            OkunanDeger d = Oku(alanAdi);
            return d == null ? null : d.Metin;
        }

        /// <summary>Fiyat alanı: metin + decimal. JSON'da sayı geldiyse doğrudan kullanılır.</summary>
        public OkunanFiyat Fiyat(string alanAdi)
        {
            return FiyatCevirDeger(Oku(alanAdi), alanAdi);
        }

        public decimal? Ondalik(string alanAdi)
        {
            return OndalikCevir(Oku(alanAdi));
        }

        public int? Tamsayi(string alanAdi)
        {
            return TamsayiCevir(Oku(alanAdi));
        }

        public bool? Mantiksal(string alanAdi)
        {
            return MantiksalCevir(Oku(alanAdi));
        }

        // ---------- Listeler ----------

        /// <summary>Listenin elemanları (Gorseller, Varyantlar, DigerSaticilar ...). Tanım yoksa boş liste.</summary>
        public List<Dugum> Liste(string listeAdi)
        {
            List<Dugum> sonuc = new List<Dugum>();
            ListeTanimi tanim;
            if (_ayar.Listeler == null || !_ayar.Listeler.TryGetValue(listeAdi, out tanim) || tanim == null) return sonuc;

            foreach (AlanAdayi aday in tanim.Adaylar)
            {
                List<Dugum> kokler = KokleriAl(aday.Kaynak);
                foreach (Dugum kok in kokler)
                {
                    List<Dugum> elemanlar = ElemanlariAl(kok, aday);
                    foreach (Dugum e in elemanlar)
                    {
                        e.ListeAdi = listeAdi;
                        if (sonuc.Count < tanim.MaksimumEleman) sonuc.Add(e);
                    }
                    // TumKokler değilse ilk dolu kök yeter
                    if (sonuc.Count > 0 && !aday.TumKokler) break;
                }

                if (sonuc.Count > 0)
                {
                    _model.AlanKaynaklari[listeAdi] = (kokler.Count > 0 ? kokler[0].KaynakTuru : "") + ":" + aday;
                    Logislemleri.Detay("LISTE_OKUNDU", listeAdi + " <- " + aday + " : " + sonuc.Count + " eleman");
                    return sonuc;
                }
                Logislemleri.Detay("LISTE_ADAY", listeAdi + " <- " + aday + " : boş");
            }
            return sonuc;
        }

        /// <summary>Liste elemanının alt alanı.</summary>
        public OkunanDeger AltOku(Dugum eleman, string altAlan)
        {
            ListeTanimi tanim;
            if (eleman == null || _ayar.Listeler == null || !_ayar.Listeler.TryGetValue(eleman.ListeAdi, out tanim)) return null;
            List<AlanAdayi> adaylar;
            if (tanim.Alanlar == null || !tanim.Alanlar.TryGetValue(altAlan, out adaylar) || adaylar == null) return null;

            foreach (AlanAdayi aday in adaylar)
            {
                OkunanDeger d;
                if (!string.IsNullOrEmpty(aday.Kaynak))
                {
                    // Alt alan sayfanın başka bir kaynağından da okunabilir (nadiren)
                    d = null;
                    foreach (Dugum kok in KokleriAl(aday.Kaynak))
                    {
                        d = AdayOku(kok, aday);
                        if (d != null) break;
                    }
                }
                else
                {
                    d = AdayOku(eleman, aday);
                }
                if (d != null) return d;
            }
            return null;
        }

        public string AltMetin(Dugum eleman, string altAlan)
        {
            OkunanDeger d = AltOku(eleman, altAlan);
            return d == null ? null : d.Metin;
        }

        public OkunanFiyat AltFiyat(Dugum eleman, string altAlan)
        {
            return FiyatCevirDeger(AltOku(eleman, altAlan), eleman.ListeAdi + "." + altAlan);
        }

        public decimal? AltOndalik(Dugum eleman, string altAlan)
        {
            return OndalikCevir(AltOku(eleman, altAlan));
        }

        public int? AltTamsayi(Dugum eleman, string altAlan)
        {
            return TamsayiCevir(AltOku(eleman, altAlan));
        }

        public bool? AltMantiksal(Dugum eleman, string altAlan)
        {
            return MantiksalCevir(AltOku(eleman, altAlan));
        }

        /// <summary>Verilen türde (AG_JSON, WINDOW_GLOBAL ...) veri veren bir kaynak var mı (tanılama/log için).</summary>
        public Dictionary<string, int> KaynakDurumu()
        {
            Dictionary<string, int> durum = new Dictionary<string, int>();
            foreach (KaynakTanimi k in _kaynaklar.Values)
            {
                durum[k.Tur + ":" + k.Ad] = KokleriAl(k.Ad).Count;
            }
            return durum;
        }

        // ---------- Çeviriler ----------

        private OkunanFiyat FiyatCevirDeger(OkunanDeger d, string alanAdi)
        {
            OkunanFiyat f = new OkunanFiyat();
            if (d == null) return f;
            f.Metin = d.Metin;
            if (d.Token != null && (d.Token.Type == JTokenType.Float || d.Token.Type == JTokenType.Integer))
            {
                f.Deger = d.Token.Value<decimal>();
                return f;
            }
            FiyatSonucu s = FiyatCevir.Coz(d.Metin);
            f.Deger = s.Deger;
            f.ParaBirimi = s.ParaBirimi;
            if (s.Deger == null) _model.UyariEkle(alanAdi + " çevrilemedi: '" + d.Metin + "'");
            else if (s.AralikMi) _model.UyariEkle(alanAdi + " aralık fiyat: '" + d.Metin + "'");
            return f;
        }

        private static decimal? OndalikCevir(OkunanDeger d)
        {
            if (d == null) return null;
            if (d.Token != null && (d.Token.Type == JTokenType.Float || d.Token.Type == JTokenType.Integer)) return d.Token.Value<decimal>();
            return FiyatCevir.Cevir(d.Metin);
        }

        private static int? TamsayiCevir(OkunanDeger d)
        {
            if (d == null) return null;
            if (d.Token != null && (d.Token.Type == JTokenType.Float || d.Token.Type == JTokenType.Integer)) return (int)d.Token.Value<decimal>();
            // "1.234 Değerlendirme", "(56)", "500+": rakam olmayan her şeyi at
            StringBuilder sb = new StringBuilder();
            foreach (char c in d.Metin)
            {
                if (char.IsDigit(c)) sb.Append(c);
                else if (sb.Length > 0 && c != '.' && c != ',' && c != ' ') break;   // ilk sayı grubu bitti
            }
            int sonuc;
            if (sb.Length > 0 && sb.Length < 10 && int.TryParse(sb.ToString(), out sonuc)) return sonuc;
            return null;
        }

        private static bool? MantiksalCevir(OkunanDeger d)
        {
            if (d == null) return null;
            if (d.Mantiksal.HasValue) return d.Mantiksal;
            if (d.Token != null && d.Token.Type == JTokenType.Boolean) return d.Token.Value<bool>();
            string m = d.Metin.Trim().ToLowerInvariant();
            if (m == "true" || m == "1" || m == "evet" || m == "yes") return true;
            if (m == "false" || m == "0" || m == "hayır" || m == "hayir" || m == "no") return false;
            return null;
        }

        // ---------- Aday okuma ----------

        private OkunanDeger AdayOku(Dugum kok, AlanAdayi aday)
        {
            string metin = null;
            JToken token = null;

            // Aday ile kök türü uyuşmalı: JSON yolu HTML elemanına, CSS seçici JSON'a uygulanmaz
            // (yoksa HTML'de "Yol" adayı elemanın tüm metnini döndürürdü)
            if (kok.Json != null && aday.Yol == null && aday.Secici != null) return null;
            if (kok.Html != null && aday.Yol != null && aday.Secici == null) return null;

            if (kok.Json != null)
            {
                if (aday.Birlestir)
                {
                    List<string> parcalar = new List<string>();
                    foreach (JToken t in JsonYolu.Hepsi(kok.Json, aday.Yol))
                    {
                        string s = JsonYolu.MetneCevir(t);
                        if (s != null) parcalar.Add(s);
                    }
                    if (parcalar.Count > 0) metin = string.Join("\n", parcalar);
                }
                else
                {
                    token = JsonYolu.Ilk(kok.Json, aday.Yol);
                    metin = JsonYolu.MetneCevir(token);
                }
            }
            else if (kok.Html != null)
            {
                metin = HtmlOku(kok.Html, aday);
            }
            else if (kok.Metin != null)
            {
                metin = kok.Metin;
            }

            if (metin != null && !string.IsNullOrEmpty(aday.Desen))
            {
                Match m = Regex.Match(metin, aday.Desen, RegexOptions.IgnoreCase);
                if (m.Success) metin = m.Groups.Count > 1 && m.Groups[1].Success ? m.Groups[1].Value : m.Value;
                else metin = null;
                token = null;
            }

            if (metin == null)
            {
                if (!string.IsNullOrEmpty(aday.YoksaDeger))
                    return new OkunanDeger { Metin = aday.YoksaDeger, Mantiksal = MantiksalSabit(aday.YoksaDeger) };
                return null;
            }

            OkunanDeger sonuc = new OkunanDeger { Metin = metin, Token = token };

            // Bool eşleme: "InStock" -> true, "OutOfStock" -> false
            if (aday.DogruDegerler != null && aday.DogruDegerler.Count > 0)
                sonuc.Mantiksal = IcerirMi(metin, aday.DogruDegerler);
            if (aday.YanlisDegerler != null && aday.YanlisDegerler.Count > 0 && IcerirMi(metin, aday.YanlisDegerler))
                sonuc.Mantiksal = false;
            else if (aday.YanlisDegerler != null && aday.YanlisDegerler.Count > 0 && (aday.DogruDegerler == null || aday.DogruDegerler.Count == 0))
                sonuc.Mantiksal = true;

            return sonuc;
        }

        private static bool? MantiksalSabit(string s)
        {
            if (s == "true") return true;
            if (s == "false") return false;
            return null;
        }

        private static bool IcerirMi(string metin, List<string> liste)
        {
            foreach (string s in liste)
            {
                if (metin.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        private static string HtmlOku(IParentNode kok, AlanAdayi aday)
        {
            List<IElement> elemanlar = new List<IElement>();
            if (string.IsNullOrWhiteSpace(aday.Secici))
            {
                if (kok is IElement) elemanlar.Add((IElement)kok);
                else if (kok is IDocument && ((IDocument)kok).DocumentElement != null) elemanlar.Add(((IDocument)kok).DocumentElement);
            }
            else
            {
                try
                {
                    if (aday.Birlestir)
                    {
                        foreach (IElement e in kok.QuerySelectorAll(aday.Secici)) elemanlar.Add(e);
                    }
                    else
                    {
                        IElement e = kok.QuerySelector(aday.Secici);
                        if (e != null) elemanlar.Add(e);
                    }
                }
                catch (Exception ex)
                {
                    Logislemleri.Uyari("HTML_SECICI", "Geçersiz CSS seçici: '" + aday.Secici + "' (" + ex.Message + ")");
                    return null;
                }
            }

            if (elemanlar.Count == 0) return null;
            if (aday.Nitelik == "var") return "true";

            List<string> parcalar = new List<string>();
            foreach (IElement e in elemanlar)
            {
                string s;
                if (string.IsNullOrEmpty(aday.Nitelik)) s = MetniTemizle(e.TextContent);
                else if (aday.Nitelik == "html") s = e.InnerHtml;
                else s = e.GetAttribute(aday.Nitelik);
                if (!string.IsNullOrWhiteSpace(s)) parcalar.Add(s.Trim());
            }
            if (parcalar.Count == 0) return null;
            return string.Join("\n", parcalar);
        }

        /// <summary>Fazla boşluk ve satır sonlarını tek boşluğa indirir.</summary>
        public static string MetniTemizle(string s)
        {
            if (s == null) return null;
            string t = Regex.Replace(s, @"\s+", " ").Trim();
            return t.Length == 0 ? null : t;
        }

        /// <summary>HTML içeren açıklamayı düz metne çevirir.</summary>
        public static string HtmldenDuzMetin(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return null;
            if (html.IndexOf('<') < 0) return html.Trim();
            // Satır yapısı kaybolmasın diye blok etiketlerinden önce satır sonu ekle
            string hazir = Regex.Replace(html, @"<\s*(br|/p|/li|/div|/h\d|/tr)\b[^>]*>", "\n", RegexOptions.IgnoreCase);
            IDocument d = _htmlParser.ParseDocument("<body>" + hazir + "</body>");
            string metin = d.Body != null ? d.Body.TextContent : hazir;
            string[] satirlar = metin.Split('\n');
            List<string> temiz = new List<string>();
            foreach (string satir in satirlar)
            {
                string s = Regex.Replace(satir, @"[ \t ]+", " ").Trim();
                if (s.Length > 0) temiz.Add(s);
            }
            return temiz.Count == 0 ? null : string.Join("\n", temiz);
        }

        private List<Dugum> ElemanlariAl(Dugum kok, AlanAdayi aday)
        {
            List<Dugum> sonuc = new List<Dugum>();
            if (kok.Json != null)
            {
                foreach (JToken t in JsonYolu.Hepsi(kok.Json, aday.Yol))
                {
                    if (t is JArray)
                    {
                        foreach (JToken e in (JArray)t)
                        {
                            if (e.Type != JTokenType.Null) sonuc.Add(new Dugum { Json = e, KaynakTuru = kok.KaynakTuru, KaynakAdi = kok.KaynakAdi });
                        }
                    }
                    else
                    {
                        sonuc.Add(new Dugum { Json = t, KaynakTuru = kok.KaynakTuru, KaynakAdi = kok.KaynakAdi });
                    }
                }
            }
            else if (kok.Html != null && !string.IsNullOrWhiteSpace(aday.Secici))
            {
                try
                {
                    foreach (IElement e in kok.Html.QuerySelectorAll(aday.Secici))
                    {
                        sonuc.Add(new Dugum { Html = e, KaynakTuru = kok.KaynakTuru, KaynakAdi = kok.KaynakAdi });
                    }
                }
                catch (Exception ex)
                {
                    Logislemleri.Uyari("HTML_SECICI", "Geçersiz CSS seçici: '" + aday.Secici + "' (" + ex.Message + ")");
                }
            }
            return sonuc;
        }

        // ---------- Kaynak kökleri ----------

        private List<Dugum> KokleriAl(string kaynakAdi)
        {
            List<Dugum> kokler;
            if (string.IsNullOrEmpty(kaynakAdi)) return new List<Dugum>();
            if (_kokler.TryGetValue(kaynakAdi, out kokler)) return kokler;

            kokler = new List<Dugum>();
            KaynakTanimi k;
            if (!_kaynaklar.TryGetValue(kaynakAdi, out k))
            {
                Logislemleri.Uyari("AYAR", "secici-ayarlari.json: '" + kaynakAdi + "' adında kaynak tanımlı değil (" + _ayar.Ad + ")");
                _kokler[kaynakAdi] = kokler;
                return kokler;
            }

            try
            {
                KokleriHesapla(k, kokler);
            }
            catch (Exception ex)
            {
                Logislemleri.Hata("KAYNAK", "Kaynak okunamadı: " + k.Ad, ex);
            }

            Logislemleri.Detay("KAYNAK_DENENDI", k.Tur + ":" + k.Ad + " -> " + kokler.Count + " kök");
            _kokler[kaynakAdi] = kokler;
            return kokler;
        }

        private void KokleriHesapla(KaynakTanimi k, List<Dugum> kokler)
        {
            string tur = (k.Tur ?? "").ToUpperInvariant();
            if (tur == Globals.VeriKaynaklari.AG_JSON)
            {
                List<string> kelimeler = k.IcerikIcerir != null && k.IcerikIcerir.Count > 0 ? k.IcerikIcerir : null;
                foreach (AgCevabi c in _ham.AgCevaplari)
                {
                    if (string.IsNullOrEmpty(c.Govde)) continue;
                    if (k.UrlIcerir != null && k.UrlIcerir.Count > 0 && !IcerirMi(c.Url ?? "", k.UrlIcerir)) continue;
                    if (kelimeler != null && !HepsiniIcerirMi(c.Govde, kelimeler)) continue;
                    JsonKokEkle(JsonYolu.GuvenliParse(c.Govde), k, kokler);
                }
            }
            else if (tur == Globals.VeriKaynaklari.WINDOW_GLOBAL)
            {
                foreach (KeyValuePair<string, string> g in _ham.Globaller)
                {
                    if (k.Global == "*" || string.Equals(g.Key, k.Global, StringComparison.Ordinal))
                        JsonKokEkle(JsonYolu.GuvenliParse(g.Value), k, kokler);
                }
            }
            else if (tur == Globals.VeriKaynaklari.SCRIPT_JSON)
            {
                foreach (KeyValuePair<string, string> s in _ham.ScriptJsonlari)
                {
                    if (k.Secici == "*" || string.Equals(s.Key, k.Secici, StringComparison.OrdinalIgnoreCase))
                        JsonKokEkle(JsonYolu.GuvenliParse(ScriptMetniniTemizle(s.Value)), k, kokler);
                }
            }
            else if (tur == Globals.VeriKaynaklari.LD_JSON)
            {
                foreach (string ld in _ham.LdJsonlar)
                {
                    JToken t = JsonYolu.GuvenliParse(ld);
                    List<JToken> nesneler = new List<JToken>();
                    LdNesneleriniTopla(t, nesneler);
                    foreach (JToken n in nesneler)
                    {
                        if (!string.IsNullOrEmpty(k.LdTip) && !LdTipUyuyorMu(n, k.LdTip)) continue;
                        JsonKokEkle(n, k, kokler);
                    }
                }
            }
            else if (tur == Globals.VeriKaynaklari.HTML)
            {
                IDocument belge = HtmlBelgesi();
                if (belge != null) kokler.Add(new Dugum { Html = belge, KaynakTuru = Globals.VeriKaynaklari.HTML, KaynakAdi = k.Ad });
                if (k.EkSayfalar)
                {
                    foreach (IDocument ek in EkBelgeler())
                    {
                        kokler.Add(new Dugum { Html = ek, KaynakTuru = Globals.VeriKaynaklari.HTML, KaynakAdi = k.Ad });
                    }
                }
            }
            else if (tur == "URL")
            {
                string url = !string.IsNullOrEmpty(_ham.SonUrl) ? _ham.SonUrl : _ham.IstenenLink;
                if (!string.IsNullOrEmpty(url)) kokler.Add(new Dugum { Metin = url, KaynakTuru = "URL", KaynakAdi = k.Ad });
            }
            else
            {
                Logislemleri.Uyari("AYAR", "Bilinmeyen kaynak türü: '" + k.Tur + "' (" + k.Ad + ")");
            }
        }

        private static string ScriptMetniniTemizle(string s)
        {
            // Bazı siteler script içine "window.x = {...};" yazar; sadece JSON kısmını al
            if (s == null) return null;
            string t = s.Trim();
            if (t.StartsWith("{") || t.StartsWith("[")) return t;
            int bas = t.IndexOf('{');
            int son = t.LastIndexOf('}');
            if (bas >= 0 && son > bas) return t.Substring(bas, son - bas + 1);
            return t;
        }

        private static void JsonKokEkle(JToken belge, KaynakTanimi k, List<Dugum> kokler)
        {
            if (belge == null) return;
            JToken kok = null;

            if (k.KokYollari != null && k.KokYollari.Count > 0)
            {
                foreach (string yol in k.KokYollari)
                {
                    kok = JsonYolu.Ilk(belge, yol);
                    if (kok != null) break;
                }
            }
            if (kok == null && k.KokAnahtarlari != null && k.KokAnahtarlari.Count > 0)
            {
                kok = JsonYolu.AnahtarlariIcerenNesne(belge, k.KokAnahtarlari, 12);
            }
            if (kok == null && (k.KokYollari == null || k.KokYollari.Count == 0) && (k.KokAnahtarlari == null || k.KokAnahtarlari.Count == 0))
            {
                kok = belge;
            }
            if (kok == null) return;

            kokler.Add(new Dugum { Json = kok, KaynakTuru = (k.Tur ?? "").ToUpperInvariant(), KaynakAdi = k.Ad });
        }

        private static void LdNesneleriniTopla(JToken t, List<JToken> liste)
        {
            if (t == null) return;
            if (t is JArray)
            {
                foreach (JToken e in (JArray)t) LdNesneleriniTopla(e, liste);
                return;
            }
            if (t is JObject)
            {
                liste.Add(t);
                JToken graf = t["@graph"];
                if (graf != null) LdNesneleriniTopla(graf, liste);
            }
        }

        private static bool LdTipUyuyorMu(JToken n, string tip)
        {
            JToken t = n["@type"];
            if (t == null) return false;
            if (t is JArray)
            {
                foreach (JToken e in (JArray)t)
                {
                    if (string.Equals(e.ToString(), tip, StringComparison.OrdinalIgnoreCase)) return true;
                }
                return false;
            }
            return string.Equals(t.ToString(), tip, StringComparison.OrdinalIgnoreCase);
        }

        private static bool HepsiniIcerirMi(string metin, List<string> kelimeler)
        {
            foreach (string k in kelimeler)
            {
                if (metin.IndexOf(k, StringComparison.Ordinal) < 0) return false;
            }
            return true;
        }

        private IDocument HtmlBelgesi()
        {
            if (_htmlBelge == null && !string.IsNullOrEmpty(_ham.Html)) _htmlBelge = _htmlParser.ParseDocument(_ham.Html);
            return _htmlBelge;
        }

        private List<IDocument> EkBelgeler()
        {
            if (_ekBelgeler == null)
            {
                _ekBelgeler = new List<IDocument>();
                foreach (string h in _ham.EkHtmlSayfalari)
                {
                    if (!string.IsNullOrEmpty(h)) _ekBelgeler.Add(_htmlParser.ParseDocument(h));
                }
            }
            return _ekBelgeler;
        }

        private static string Kisalt(string s)
        {
            if (s == null) return "null";
            s = s.Replace("\n", " ");
            return s.Length > 120 ? s.Substring(0, 120) + "..." : s;
        }
    }

    /// <summary>Okuma kökü ya da liste elemanı: JSON düğümü, HTML elemanı veya düz metin (URL).</summary>
    public class Dugum
    {
        public JToken Json;
        public IParentNode Html;
        public string Metin;
        public string KaynakTuru;
        public string KaynakAdi;
        public string ListeAdi;
    }

    public class OkunanDeger
    {
        public string Metin;
        public JToken Token;
        public bool? Mantiksal;
        public string KaynakTuru;
        public string Aciklama;
    }

    public class OkunanFiyat
    {
        public string Metin;
        public decimal? Deger;
        public string ParaBirimi;
    }
}
