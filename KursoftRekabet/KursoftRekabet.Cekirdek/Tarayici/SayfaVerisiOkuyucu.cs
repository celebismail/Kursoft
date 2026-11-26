using System.Diagnostics;
using KursoftRekabet.Ayristirma;
using KursoftRekabet.model;
using Microsoft.Playwright;
using Newtonsoft.Json.Linq;

namespace KursoftRekabet.Tarayici
{
    /// <summary>
    /// Ürün sayfasını açar, verinin gelmesini bekler ve tüm ham veriyi (ağ JSON, window global'leri,
    /// script JSON'ları, ld+json, HTML, diğer satıcı sayfaları) HamSayfaVerisi'ne toplar.
    /// Ayrıştırma burada yapılmaz.
    /// </summary>
    public static class SayfaVerisiOkuyucu
    {
        // Bilinen global'leri okur + (tara=true ise) sitenin eklediği tüm global'leri tarar.
        // Döngüsel referanslar ve fonksiyonlar atlanır.
        private const string GloballeriOkuJs = @"(a) => {
            const sonuc = { globaller: [], veriler: {} };
            const kelimeler = a.kelimeler || [];
            const icerir = (m) => kelimeler.some(k => m.indexOf(k) >= 0);
            const metne = (v) => {
                try {
                    if (typeof v === 'string') return v;
                    const gorulen = new WeakSet();
                    return JSON.stringify(v, (k, x) => {
                        if (typeof x === 'function') return undefined;
                        if (typeof x === 'object' && x !== null) { if (gorulen.has(x)) return undefined; gorulen.add(x); }
                        return x;
                    });
                } catch (e) { return null; }
            };
            for (const ad of a.bilinenler) {
                try {
                    const v = window[ad];
                    if (v === undefined || v === null) continue;
                    const m = metne(v);
                    if (!m) continue;
                    sonuc.veriler[ad] = m;
                    sonuc.globaller.push({ Ad: ad, Uzunluk: m.length, UrunVar: icerir(m) });
                } catch (e) { }
            }
            if (a.tara) {
                try {
                    const f = document.createElement('iframe');
                    f.style.display = 'none';
                    document.documentElement.appendChild(f);
                    const temiz = new Set(Object.getOwnPropertyNames(f.contentWindow));
                    f.remove();
                    for (const k of Object.getOwnPropertyNames(window)) {
                        if (temiz.has(k) || a.bilinenler.indexOf(k) >= 0) continue;
                        let v;
                        try { v = window[k]; } catch (e) { continue; }
                        if (!v || typeof v !== 'object') continue;
                        const m = metne(v);
                        if (!m || m.length < 20) continue;
                        const u = icerir(m);
                        sonuc.globaller.push({ Ad: k, Uzunluk: m.length, UrunVar: u });
                        if (u && m.length < 8000000) sonuc.veriler[k] = m;
                    }
                } catch (e) { sonuc.hata = String(e); }
            }
            return JSON.stringify(sonuc);
        }";

        private const string ScriptleriOkuJs = @"(seciciler) => {
            const r = {};
            for (const s of seciciler) {
                try { const e = document.querySelector(s); if (e && e.textContent) r[s] = e.textContent; } catch (e) { }
            }
            document.querySelectorAll('script[type=""application/json""]').forEach((e, i) => {
                const key = e.id ? 'script#' + e.id : 'application/json#' + i;
                if (!(key in r) && e.textContent && e.textContent.length > 2 && e.textContent.length < 8000000) r[key] = e.textContent;
            });
            return JSON.stringify(r);
        }";

        private const string LdJsonJs = @"() => JSON.stringify(Array.from(document.querySelectorAll('script[type=""application/ld+json""]')).map(s => s.textContent))";

        private const string HazirMiJs = @"(a) => {
            const icerir = (m) => !a.kelimeler.length || a.kelimeler.some(k => m.indexOf(k) >= 0);
            for (const g of a.globaller) {
                try { const v = window[g]; if (v && icerir(typeof v === 'string' ? v : JSON.stringify(v))) return 'global:' + g; } catch (e) { }
            }
            for (const s of a.scriptler) {
                try { const e = document.querySelector(s); if (e && icerir(e.textContent || '')) return 'script:' + s; } catch (e) { }
            }
            for (const s of a.html) {
                try { if (document.querySelector(s)) return 'html:' + s; } catch (e) { }
            }
            return '';
        }";

        /// <summary>
        /// Sayfayı açıp ham veriyi toplar. Sayfa açık kalır (ekran görüntüsü için); kapatmak çağıranın işi.
        /// </summary>
        public static async Task<HamSayfaVerisi> AcVeTopla(IPage sayfa, string link, PlatformAyari ayar)
        {
            HamSayfaVerisi ham = new HamSayfaVerisi();
            ham.IstenenLink = link;
            ham.ToplanmaZamani = DateTime.Now;

            AgYakalayici ag = new AgYakalayici(ayar.UrunAnahtarKelimeleri);
            ag.Baslat(sayfa);

            List<string> bilinenGloballer = new List<string>();
            List<string> scriptSecicileri = new List<string>();
            bool globalTara = false;
            foreach (KaynakTanimi k in ayar.Kaynaklar)
            {
                string tur = (k.Tur ?? "").ToUpperInvariant();
                if (tur == Globals.VeriKaynaklari.WINDOW_GLOBAL)
                {
                    if (k.Global == "*") globalTara = true;
                    else if (!string.IsNullOrEmpty(k.Global) && !bilinenGloballer.Contains(k.Global)) bilinenGloballer.Add(k.Global);
                }
                else if (tur == Globals.VeriKaynaklari.SCRIPT_JSON && !string.IsNullOrEmpty(k.Secici) && k.Secici != "*" && !scriptSecicileri.Contains(k.Secici))
                {
                    scriptSecicileri.Add(k.Secici);
                }
            }

            // 1) Sayfaya git. networkidle'a güvenmiyoruz; DOM gelince verinin gelmesini kendimiz bekliyoruz.
            Stopwatch sw = Stopwatch.StartNew();

            // Ana belgenin HTTP durumu: gövdesiz 403 gibi cevaplarda GotoAsync hata fırlatır, durumu buradan alırız
            int? anaBelgeDurumu = null;
            EventHandler<IResponse> belgeDinleyici = delegate (object g, IResponse r)
            {
                try
                {
                    if (r.Request.IsNavigationRequest && r.Frame == sayfa.MainFrame) anaBelgeDurumu = r.Status;
                }
                catch (Exception ex)
                {
                    Logislemleri.Detay("BELGE_DURUMU", "Ana belge durumu okunamadı: " + ex.Message);
                }
            };
            sayfa.Response += belgeDinleyici;

            IResponse cevap = null;
            try
            {
                cevap = await sayfa.GotoAsync(link, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = Math.Max(5, Globals.Ayarlar.SayfaYuklemeZamanAsimiSn) * 1000
                });
            }
            catch (PlaywrightException ex) when (ex.Message != null && ex.Message.Contains("ERR_HTTP_RESPONSE_CODE_FAILURE") && anaBelgeDurumu.HasValue)
            {
                // Sunucu hata koduyla boş sayfa döndü (çoğunlukla 403/429 bot engeli). Durum tespiti karar verecek.
                ham.ToplamaNotlari.Add("sayfa hata koduyla döndü: HTTP " + anaBelgeDurumu.Value);
            }
            finally
            {
                sayfa.Response -= belgeDinleyici;
            }
            ham.HttpDurum = cevap != null ? (int?)cevap.Status : anaBelgeDurumu;
            ham.SonUrl = sayfa.Url;
            ham.SayfaBasligi = await GuvenliBaslik(sayfa);
            Logislemleri.Bilgi("SAYFA_ACILDI", "HTTP " + (ham.HttpDurum.HasValue ? ham.HttpDurum.Value.ToString() : "?") + " | " + ham.SayfaBasligi, sw.ElapsedMilliseconds);

            // 2) Engel/404 sayfasında veri beklemek boşuna
            string aciklama;
            string kesin = DurumTespiti.KesinDurum(ham, ayar, out aciklama);
            if (kesin == null)
            {
                await VeriyiBekle(sayfa, ayar, ag, bilinenGloballer, scriptSecicileri, ham);
            }
            else
            {
                ham.ToplamaNotlari.Add("veri beklenmedi: " + kesin + " (" + aciklama + ")");
            }

            // 3) Topla
            await ag.BekleAsync(3000);
            await Topla(sayfa, ayar, bilinenGloballer, scriptSecicileri, globalTara, ham);

            // 4) Diğer satıcılar ayrı sekme/sayfadaysa
            if (kesin == null && ayar.DigerSaticiSayfasi != null && ayar.DigerSaticiSayfasi.Acik)
            {
                await DigerSaticiSayfalariniTopla(sayfa, ayar, ham);
            }

            await ag.BekleAsync(3000);
            ag.Durdur();
            ham.AgCevaplari = ag.Cevaplar();
            return ham;
        }

        private static async Task VeriyiBekle(IPage sayfa, PlatformAyari ayar, AgYakalayici ag, List<string> globaller, List<string> scriptler, HamSayfaVerisi ham)
        {
            Stopwatch sw = Stopwatch.StartNew();
            int sinirMs = Math.Max(1, Globals.Ayarlar.VeriBeklemeSn) * 1000;
            bool popupDenendi = false;
            Dictionary<string, object> jsArgSozluk = new Dictionary<string, object>();
            jsArgSozluk["globaller"] = globaller.ToArray();
            jsArgSozluk["scriptler"] = scriptler.ToArray();
            jsArgSozluk["html"] = (ayar.HazirSecicileri ?? new List<string>()).ToArray();
            jsArgSozluk["kelimeler"] = (ayar.UrunAnahtarKelimeleri ?? new List<string>()).ToArray();

            while (sw.ElapsedMilliseconds < sinirMs)
            {
                if (ag.UrunVerisiGeldiMi)
                {
                    Logislemleri.Detay("VERI_HAZIR", "Ürün verisi ağ cevabında geldi", sw.ElapsedMilliseconds);
                    return;
                }

                string hazir = null;
                try
                {
                    hazir = await sayfa.EvaluateAsync<string>(HazirMiJs, jsArgSozluk);
                }
                catch (Exception ex)
                {
                    // Sayfa o an yönleniyor olabilir; bir sonraki turda tekrar bakılır
                    if (TarayiciYonetimi.CokmeHatasiMi(ex)) throw;
                    Logislemleri.Detay("VERI_BEKLE", "Hazır kontrolü yapılamadı: " + ex.Message);
                }
                if (!string.IsNullOrEmpty(hazir))
                {
                    Logislemleri.Detay("VERI_HAZIR", "Ürün verisi hazır: " + hazir, sw.ElapsedMilliseconds);
                    return;
                }

                // Pop-up veri okumayı engelliyor olabilir: bir kere kapatmayı dene
                if (!popupDenendi && sw.ElapsedMilliseconds > 1500)
                {
                    popupDenendi = true;
                    await PopuplariKapat(sayfa, ayar);
                }

                await Task.Delay(500);
            }

            ham.ToplamaNotlari.Add("veri bekleme süresi doldu (" + sinirMs + " ms), mevcut veriyle devam edildi");
            Logislemleri.Uyari("VERI_BEKLE", "Ürün verisi " + sinirMs + " ms içinde gelmedi, mevcut veriyle devam", sw.ElapsedMilliseconds);
        }

        private static async Task Topla(IPage sayfa, PlatformAyari ayar, List<string> bilinenGloballer, List<string> scriptSecicileri, bool globalTara, HamSayfaVerisi ham)
        {
            Stopwatch sw = Stopwatch.StartNew();
            ham.SonUrl = sayfa.Url;
            ham.SayfaBasligi = await GuvenliBaslik(sayfa);

            // Window global'leri
            try
            {
                Dictionary<string, object> a = new Dictionary<string, object>();
                a["bilinenler"] = bilinenGloballer.ToArray();
                a["kelimeler"] = (ayar.UrunAnahtarKelimeleri ?? new List<string>()).ToArray();
                a["tara"] = globalTara || Logislemleri.DetayAcikMi;
                string metin = await sayfa.EvaluateAsync<string>(GloballeriOkuJs, a);
                JObject o = JObject.Parse(metin);
                if (o["veriler"] is JObject)
                {
                    foreach (JProperty p in ((JObject)o["veriler"]).Properties()) ham.Globaller[p.Name] = (string)p.Value;
                }
                if (o["globaller"] is JArray) ham.GlobalListesi = o["globaller"].ToObject<List<GlobalOzeti>>();
                if (o["hata"] != null) ham.ToplamaNotlari.Add("global tarama hatası: " + o["hata"]);
            }
            catch (Exception ex)
            {
                if (TarayiciYonetimi.CokmeHatasiMi(ex)) throw;
                ham.ToplamaNotlari.Add("global'ler okunamadı: " + ex.Message);
                Logislemleri.Uyari("TOPLA", "Window global'leri okunamadı: " + ex.Message);
            }

            // Script içi JSON'lar
            try
            {
                string metin = await sayfa.EvaluateAsync<string>(ScriptleriOkuJs, scriptSecicileri.ToArray());
                JObject o = JObject.Parse(metin);
                foreach (JProperty p in o.Properties()) ham.ScriptJsonlari[p.Name] = (string)p.Value;
            }
            catch (Exception ex)
            {
                if (TarayiciYonetimi.CokmeHatasiMi(ex)) throw;
                ham.ToplamaNotlari.Add("script JSON'ları okunamadı: " + ex.Message);
                Logislemleri.Uyari("TOPLA", "Script JSON'ları okunamadı: " + ex.Message);
            }

            // ld+json
            try
            {
                string metin = await sayfa.EvaluateAsync<string>(LdJsonJs);
                JArray dizi = JArray.Parse(metin);
                foreach (JToken t in dizi)
                {
                    string s = (string)t;
                    if (!string.IsNullOrWhiteSpace(s)) ham.LdJsonlar.Add(s);
                }
            }
            catch (Exception ex)
            {
                if (TarayiciYonetimi.CokmeHatasiMi(ex)) throw;
                ham.ToplamaNotlari.Add("ld+json okunamadı: " + ex.Message);
            }

            // HTML
            try
            {
                ham.Html = await sayfa.ContentAsync();
            }
            catch (Exception ex)
            {
                if (TarayiciYonetimi.CokmeHatasiMi(ex)) throw;
                ham.ToplamaNotlari.Add("HTML okunamadı: " + ex.Message);
            }

            Logislemleri.Detay("VERI_TOPLANDI", "global: " + ham.Globaller.Count + ", script: " + ham.ScriptJsonlari.Count
                + ", ld+json: " + ham.LdJsonlar.Count + ", html: " + (ham.Html != null ? ham.Html.Length : 0), sw.ElapsedMilliseconds);
        }

        private static async Task DigerSaticiSayfalariniTopla(IPage sayfa, PlatformAyari ayar, HamSayfaVerisi ham)
        {
            DigerSaticiSayfasiAyari d = ayar.DigerSaticiSayfasi;
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                if (!string.IsNullOrWhiteSpace(d.UrlSablonu))
                {
                    Uri uri = new Uri(sayfa.Url);
                    string temelUrl = uri.GetLeftPart(UriPartial.Path);
                    string hedef = d.UrlSablonu.Replace("{url}", temelUrl);
                    await sayfa.GotoAsync(hedef, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
                }
                else if (!string.IsNullOrWhiteSpace(d.TiklanacakSecici))
                {
                    await PopuplariKapat(sayfa, ayar);
                    IElementHandle sekme = await sayfa.QuerySelectorAsync(d.TiklanacakSecici);
                    if (sekme == null)
                    {
                        Logislemleri.Detay("DIGER_SATICI", "Diğer satıcılar butonu yok (tek satıcılı ürün olabilir)");
                        return;
                    }
                    await sekme.ScrollIntoViewIfNeededAsync();
                    await sekme.ClickAsync();
                }

                if (!string.IsNullOrWhiteSpace(d.BeklenecekSecici))
                {
                    try
                    {
                        await sayfa.WaitForSelectorAsync(d.BeklenecekSecici, new PageWaitForSelectorOptions { Timeout = 10000, State = WaitForSelectorState.Attached });
                    }
                    catch (TimeoutException)
                    {
                        Logislemleri.Detay("DIGER_SATICI", "Diğer satıcı listesi gelmedi (tek satıcılı ürün olabilir)");
                        return;
                    }
                }

                int maksimum = Math.Max(1, Math.Min(d.MaksimumSayfa, Globals.Ayarlar.MaksimumDigerSaticiSayfasi));
                string oncekiHtml = null;
                for (int sayfaNo = 1; sayfaNo <= maksimum; sayfaNo++)
                {
                    await sayfa.WaitForTimeoutAsync(Math.Max(300, d.SayfaBeklemeMs));
                    string html = await sayfa.ContentAsync();
                    if (html == oncekiHtml) break;   // sayfa değişmedi: son sayfadayız
                    ham.EkHtmlSayfalari.Add(html);
                    oncekiHtml = html;

                    if (string.IsNullOrWhiteSpace(d.SonrakiSecici)) break;
                    IElementHandle sonraki = await sayfa.QuerySelectorAsync(d.SonrakiSecici);
                    if (sonraki == null) break;
                    string sinif = (await sonraki.GetAttributeAsync("class")) ?? "";
                    string pasif = await sonraki.GetAttributeAsync("disabled");
                    if (pasif != null || (!string.IsNullOrEmpty(d.PasifSinifIcerir) && sinif.Contains(d.PasifSinifIcerir))) break;
                    await sonraki.ClickAsync();

                    if (sayfaNo == maksimum) Logislemleri.Uyari("DIGER_SATICI", "Diğer satıcı sayfa sınırına (" + maksimum + ") ulaşıldı, kalanlar okunmadı");
                }
                Logislemleri.Detay("DIGER_SATICI", ham.EkHtmlSayfalari.Count + " sayfa toplandı", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                if (TarayiciYonetimi.CokmeHatasiMi(ex)) throw;
                ham.ToplamaNotlari.Add("diğer satıcı sayfası okunamadı: " + ex.Message);
                Logislemleri.Uyari("DIGER_SATICI", "Diğer satıcı sayfası okunamadı: " + ex.Message, sw.ElapsedMilliseconds);
            }
        }

        private static async Task PopuplariKapat(IPage sayfa, PlatformAyari ayar)
        {
            if (ayar.PopupKapatSecicileri == null) return;
            foreach (string s in ayar.PopupKapatSecicileri)
            {
                try
                {
                    IElementHandle e = await sayfa.QuerySelectorAsync(s);
                    if (e != null && await e.IsVisibleAsync())
                    {
                        await e.ClickAsync(new ElementHandleClickOptions { Timeout = 2000 });
                        Logislemleri.Detay("POPUP", "Pop-up kapatıldı: " + s);
                    }
                }
                catch (Exception ex)
                {
                    if (TarayiciYonetimi.CokmeHatasiMi(ex)) throw;
                    Logislemleri.Detay("POPUP", "Pop-up kapatılamadı: " + s + " (" + ex.Message + ")");
                }
            }
        }

        private static async Task<string> GuvenliBaslik(IPage sayfa)
        {
            try
            {
                return await sayfa.TitleAsync();
            }
            catch (Exception ex)
            {
                if (TarayiciYonetimi.CokmeHatasiMi(ex)) throw;
                return null;
            }
        }
    }
}
