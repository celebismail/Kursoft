using System.Text;
using KursoftRekabet.model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KursoftRekabet.Yardimcilar
{
    /// <summary>
    /// Hata anında (ya da --tanila modunda) sayfanın ham verisini klasöre kaydeder.
    /// Aynı klasör yapısı test projesindeki Ornekler/ klasöründe de kullanılır:
    /// tanılama klasörünü Ornekler altına kopyalamak = yeni test örneği.
    ///
    ///   ozet.json            link, durum, başlık, global listesi, ağ özeti, dosya eşlemeleri
    ///   ag/001.json ...      ürün verisi taşıyan ağ cevapları
    ///   globaller/001.json   window global'leri
    ///   scriptler/001.json   script içi JSON'lar
    ///   ldjson/001.json      ld+json blokları
    ///   sayfa.html           sayfanın HTML'i
    ///   ek_001.html ...      diğer satıcı sayfaları
    ///   ekran.png            ekran görüntüsü
    ///   sonuc.json           o anki UrunBilgiModel
    /// </summary>
    public static class Tanilama
    {
        private static readonly UTF8Encoding _utf8 = new UTF8Encoding(false);

        public static string KlasorOlustur(Globals.ModulType modulType, Guid islemId)
        {
            string klasor = Path.Combine(Globals.LogKlasoru, "Tanilama", Globals.ModulAdi(modulType),
                DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + islemId.ToString("N").Substring(0, 12));
            Directory.CreateDirectory(klasor);
            return klasor;
        }

        /// <summary>Ham veriyi klasöre yazar. Hata olursa loglar ve null döner (exception yok).</summary>
        public static string HamVeriKaydet(HamSayfaVerisi ham, Globals.ModulType modulType, UrunBilgiModel model, byte[] ekranGoruntusu)
        {
            try
            {
                string klasor = KlasorOlustur(modulType, model != null ? model.IslemID : Guid.NewGuid());
                Kaydet(ham, klasor, model, ekranGoruntusu);
                return klasor;
            }
            catch (Exception ex)
            {
                Logislemleri.Hata("TANILAMA", "Tanılama verisi kaydedilemedi", ex);
                return null;
            }
        }

        public static void Kaydet(HamSayfaVerisi ham, string klasor, UrunBilgiModel model, byte[] ekranGoruntusu)
        {
            Directory.CreateDirectory(klasor);
            JObject ozet = new JObject();
            ozet["istenenLink"] = ham.IstenenLink;
            ozet["sonUrl"] = ham.SonUrl;
            ozet["httpDurum"] = ham.HttpDurum;
            ozet["sayfaBasligi"] = ham.SayfaBasligi;
            ozet["toplanmaZamani"] = ham.ToplanmaZamani;
            ozet["notlar"] = JArray.FromObject(ham.ToplamaNotlari);
            ozet["globalListesi"] = JArray.FromObject(ham.GlobalListesi);
            if (model != null)
            {
                ozet["modulType"] = Globals.ModulAdi(model.ModulType);
                ozet["hataKodu"] = model.HataKodu;
                ozet["hataMesaji"] = model.HataMesaji;
            }

            JArray agOzeti = new JArray();
            int sira = 1;
            foreach (AgCevabi c in ham.AgCevaplari)
            {
                JObject o = new JObject();
                o["url"] = c.Url;
                o["durum"] = c.Durum;
                o["uzunluk"] = c.Uzunluk;
                o["urunVar"] = c.UrunVar;
                if (!string.IsNullOrEmpty(c.Govde))
                {
                    string dosya = "ag/" + sira.ToString("000") + ".json";
                    MetinYaz(klasor, dosya, GuzelJson(c.Govde));
                    o["dosya"] = dosya;
                    sira++;
                }
                agOzeti.Add(o);
            }
            ozet["agCevaplari"] = agOzeti;

            ozet["globaller"] = SozlukYaz(klasor, "globaller", ham.Globaller);
            ozet["scriptler"] = SozlukYaz(klasor, "scriptler", ham.ScriptJsonlari);

            JArray ld = new JArray();
            sira = 1;
            foreach (string l in ham.LdJsonlar)
            {
                string dosya = "ldjson/" + sira.ToString("000") + ".json";
                MetinYaz(klasor, dosya, GuzelJson(l));
                ld.Add(dosya);
                sira++;
            }
            ozet["ldjson"] = ld;

            if (!string.IsNullOrEmpty(ham.Html))
            {
                MetinYaz(klasor, "sayfa.html", ham.Html);
                ozet["html"] = "sayfa.html";
            }

            JArray ekler = new JArray();
            sira = 1;
            foreach (string h in ham.EkHtmlSayfalari)
            {
                string dosya = "ek_" + sira.ToString("000") + ".html";
                MetinYaz(klasor, dosya, h);
                ekler.Add(dosya);
                sira++;
            }
            ozet["ekSayfalar"] = ekler;

            MetinYaz(klasor, "ozet.json", ozet.ToString(Formatting.Indented));
            if (ekranGoruntusu != null && ekranGoruntusu.Length > 0) File.WriteAllBytes(Path.Combine(klasor, "ekran.png"), ekranGoruntusu);
            if (model != null) MetinYaz(klasor, "sonuc.json", JsonConvert.SerializeObject(model, Formatting.Indented));
        }

        /// <summary>Kaydedilmiş klasörden ham veriyi geri okur (testler ve yeniden ayrıştırma için).</summary>
        public static HamSayfaVerisi HamVeriYukle(string klasor)
        {
            JObject ozet = JObject.Parse(File.ReadAllText(Path.Combine(klasor, "ozet.json")));
            HamSayfaVerisi ham = new HamSayfaVerisi();
            ham.IstenenLink = (string)ozet["istenenLink"];
            ham.SonUrl = (string)ozet["sonUrl"];
            ham.HttpDurum = (int?)ozet["httpDurum"];
            ham.SayfaBasligi = (string)ozet["sayfaBasligi"];
            if (ozet["toplanmaZamani"] != null && ozet["toplanmaZamani"].Type != JTokenType.Null) ham.ToplanmaZamani = (DateTime)ozet["toplanmaZamani"];
            if (ozet["globalListesi"] is JArray) ham.GlobalListesi = ozet["globalListesi"].ToObject<List<GlobalOzeti>>();

            if (ozet["agCevaplari"] is JArray)
            {
                foreach (JToken o in (JArray)ozet["agCevaplari"])
                {
                    AgCevabi c = new AgCevabi();
                    c.Url = (string)o["url"];
                    c.Durum = o["durum"] != null ? (int)o["durum"] : 200;
                    c.Uzunluk = o["uzunluk"] != null ? (int)o["uzunluk"] : 0;
                    c.UrunVar = o["urunVar"] != null && (bool)o["urunVar"];
                    string dosya = (string)o["dosya"];
                    if (!string.IsNullOrEmpty(dosya)) c.Govde = MetinOku(klasor, dosya);
                    ham.AgCevaplari.Add(c);
                }
            }

            SozlukOku(klasor, ozet["globaller"] as JObject, ham.Globaller);
            SozlukOku(klasor, ozet["scriptler"] as JObject, ham.ScriptJsonlari);

            if (ozet["ldjson"] is JArray)
            {
                foreach (JToken d in (JArray)ozet["ldjson"]) ham.LdJsonlar.Add(MetinOku(klasor, (string)d));
            }

            string html = (string)ozet["html"];
            if (!string.IsNullOrEmpty(html)) ham.Html = MetinOku(klasor, html);

            if (ozet["ekSayfalar"] is JArray)
            {
                foreach (JToken d in (JArray)ozet["ekSayfalar"]) ham.EkHtmlSayfalari.Add(MetinOku(klasor, (string)d));
            }
            return ham;
        }

        private static JObject SozlukYaz(string klasor, string altKlasor, Dictionary<string, string> sozluk)
        {
            JObject eslesme = new JObject();
            int sira = 1;
            foreach (KeyValuePair<string, string> kv in sozluk)
            {
                string dosya = altKlasor + "/" + sira.ToString("000") + ".json";
                MetinYaz(klasor, dosya, GuzelJson(kv.Value));
                eslesme[kv.Key] = dosya;
                sira++;
            }
            return eslesme;
        }

        private static void SozlukOku(string klasor, JObject eslesme, Dictionary<string, string> hedef)
        {
            if (eslesme == null) return;
            foreach (JProperty p in eslesme.Properties()) hedef[p.Name] = MetinOku(klasor, (string)p.Value);
        }

        private static string GuzelJson(string metin)
        {
            // Okunabilsin diye girintili yaz; JSON değilse olduğu gibi
            JToken t = JsonYolu.GuvenliParse(metin);
            return t != null ? t.ToString(Formatting.Indented) : metin;
        }

        private static void MetinYaz(string klasor, string goreliYol, string metin)
        {
            string yol = Path.Combine(klasor, goreliYol.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(yol));
            File.WriteAllText(yol, metin ?? "", _utf8);
        }

        private static string MetinOku(string klasor, string goreliYol)
        {
            string yol = Path.Combine(klasor, goreliYol.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(yol) ? File.ReadAllText(yol) : null;
        }
    }
}
