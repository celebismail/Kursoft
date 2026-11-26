using System.Diagnostics;
using KursoftRekabet.Ayristirma;
using KursoftRekabet.model;
using KursoftRekabet.Tarayici;
using KursoftRekabet.Yardimcilar;
using Microsoft.Playwright;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KursoftRekabet
{
    /// <summary>
    /// Bir linkin baştan sona işlenmesi (her platform için aynı):
    ///   link doğrula -> hız sınırı -> sayfayı aç, ham veriyi topla -> ayrıştır -> zorunlu alan kontrolü
    ///   -> gerekirse tanılama kaydı -> geçici hatada tekrar dene.
    /// Platforma özel kısım sadece "ayristirici" (trendyolUrunBilgi.Ayristir vb.).
    /// Hiçbir durumda exception fırlatmaz; her zaman dolu bir model döner.
    /// </summary>
    public static class UrunBilgiAkisi
    {
        private static SemaphoreSlim _esZamanli;
        private static readonly object _kilit = new object();

        /// <summary>Program'ın Ctrl+C / toplam zaman aşımı iptali. Tetiklenince kalan linkler ZAMAN_ASIMI döner.</summary>
        public static CancellationToken GenelIptal = CancellationToken.None;

        public static async Task<UrunBilgiModel> CalistirAsync(Globals.ModulType modulType, string link, Action<AlanOkuyucu, UrunBilgiModel> ayristirici)
        {
            UrunBilgiModel kimlik = YeniModel(modulType, link, Guid.NewGuid());
            UrunBilgiModel sonuc = kimlik;
            Stopwatch sw = Stopwatch.StartNew();

            using (Logislemleri.IslemBaglamiAc(kimlik))
            {
                try
                {
                    Logislemleri.Bilgi("BASLADI", "Link işleniyor");
                    sonuc = await CalistirIcAsync(kimlik, ayristirici);
                }
                catch (Exception ex)
                {
                    // Buraya düşmemesi lazım; düşerse de model dönsün
                    sonuc.HataYaz(Globals.HataKodlari.BILINMEYEN, "Beklenmeyen hata: " + ex.Message);
                    Logislemleri.Hata("BEKLENMEYEN", "İşlem sırasında beklenmeyen hata", ex);
                }
                finally
                {
                    sonuc.SureMs = sw.ElapsedMilliseconds;
                    if (sonuc.Basarili)
                    {
                        Logislemleri.Bilgi("BITTI", "Başarılı | " + sonuc.UrunAdi + " | " + sonuc.FiyatMetni + " | satıcı: "
                            + (sonuc.Satici != null ? sonuc.Satici.SaticiAdi : null) + " | diğer satıcı: " + sonuc.DigerSaticilar.Count
                            + (sonuc.Uyarilar.Count > 0 ? " | uyarılar: " + string.Join("; ", sonuc.Uyarilar) : ""), sonuc.SureMs);
                    }
                    else
                    {
                        Logislemleri.Uyari("BITTI", "Başarısız | " + sonuc.HataKodu + " | " + sonuc.HataMesaji, sonuc.SureMs);
                    }
                }
            }
            return sonuc;
        }

        private static async Task<UrunBilgiModel> CalistirIcAsync(UrunBilgiModel kimlik, Action<AlanOkuyucu, UrunBilgiModel> ayristirici)
        {
            PlatformAyari ayar = SeciciAyarlari.Platform(kimlik.ModulType);
            if (ayar == null)
            {
                kimlik.HataYaz(Globals.HataKodlari.BILINMEYEN, "secici-ayarlari.json içinde '" + Globals.ModulAdi(kimlik.ModulType) + "' bölümü yok");
                return kimlik;
            }

            LinkSonucu ls = LinkYardimcisi.Dogrula(kimlik.Link, ayar);
            if (!ls.Gecerli)
            {
                kimlik.HataYaz(Globals.HataKodlari.GECERSIZ_LINK, ls.Hata);
                return kimlik;
            }
            kimlik.NormalizeLink = ls.NormalizeLink;

            int denemeSiniri = 1 + Math.Max(0, Globals.Ayarlar.TekrarDenemeSayisi);
            UrunBilgiModel sonuc = kimlik;
            for (int deneme = 1; deneme <= denemeSiniri; deneme++)
            {
                if (GenelIptal.IsCancellationRequested)
                {
                    sonuc.HataYaz(Globals.HataKodlari.ZAMAN_ASIMI, "Çalışma iptal edildi / toplam süre doldu, link işlenmedi");
                    return sonuc;
                }

                UrunBilgiModel denemeModeli = YeniModel(kimlik.ModulType, kimlik.Link, kimlik.IslemID);
                denemeModeli.IstekZamani = kimlik.IstekZamani;
                denemeModeli.NormalizeLink = kimlik.NormalizeLink;
                denemeModeli.DenemeSayisi = deneme;

                bool linkZamanAsimi = await TekDenemeAsync(denemeModeli, ayar, ls.KisaLinkMi, ayristirici);
                sonuc = denemeModeli;

                if (sonuc.Basarili) return sonuc;
                if (Globals.HataKodlari.KaliciMi(sonuc.HataKodu)) return sonuc;
                // Engelde hemen tekrar denemek işe yaramaz, sadece bekleme süresi artırılır
                if (sonuc.HataKodu == Globals.HataKodlari.ENGEL) return sonuc;
                if (sonuc.HataKodu == Globals.HataKodlari.ZAMAN_ASIMI && !linkZamanAsimi) return sonuc;   // toplam süre doldu

                if (deneme < denemeSiniri)
                {
                    int bekleme = Math.Max(0, Globals.Ayarlar.TekrarBeklemeMs) * deneme;
                    Logislemleri.Uyari("TEKRAR_DENEME", "Geçici hata (" + sonuc.HataKodu + "), " + bekleme + " ms sonra tekrar denenecek (" + (deneme + 1) + "/" + denemeSiniri + ")");
                    try
                    {
                        await Task.Delay(bekleme, GenelIptal);
                    }
                    catch (OperationCanceledException)
                    {
                        return sonuc;
                    }
                }
            }
            return sonuc;
        }

        /// <summary>Tek deneme. Link zaman aşımına uğradıysa true döner (tekrar denenebilir).</summary>
        private static async Task<bool> TekDenemeAsync(UrunBilgiModel model, PlatformAyari ayar, bool kisaLinkMi, Action<AlanOkuyucu, UrunBilgiModel> ayristirici)
        {
            SemaphoreSlim esZamanli = EsZamanliKilit();
            bool kilitAlindi = false;
            IBrowserContext baglam = null;
            HamSayfaVerisi ham = null;

            using (CancellationTokenSource zaman = CancellationTokenSource.CreateLinkedTokenSource(GenelIptal))
            {
                zaman.CancelAfter(TimeSpan.FromSeconds(Math.Max(10, Globals.Ayarlar.LinkZamanAsimiSn)));
                try
                {
                    await HizSiniri.BekleAsync(model.ModulType, zaman.Token);
                    await esZamanli.WaitAsync(zaman.Token);
                    kilitAlindi = true;

                    Stopwatch sw = Stopwatch.StartNew();
                    baglam = await TarayiciYonetimi.YeniBaglamAsync(ayar);
                    IBrowserContext kapatilacak = baglam;
                    // Süre dolunca bağlamı kapat: askıda kalan Playwright çağrıları hata ile döner
                    using (zaman.Token.Register(delegate { BaglamiKapat(kapatilacak); }))
                    {
                        IPage sayfa = await baglam.NewPageAsync();
                        ham = await SayfaVerisiOkuyucu.AcVeTopla(sayfa, model.NormalizeLink, ayar);
                        Logislemleri.Detay("TOPLAMA_BITTI", "Ham veri toplandı", sw.ElapsedMilliseconds);

                        if (kisaLinkMi && !KisaLinkSonrasiDogrula(model, ayar, ham)) return false;

                        HamVeridenDoldur(ham, ayar, model, ayristirici);

                        if (TanilamaGerekliMi(model))
                        {
                            byte[] ekran = null;
                            try
                            {
                                ekran = await sayfa.ScreenshotAsync(new PageScreenshotOptions { FullPage = false, Timeout = 10000 });
                            }
                            catch (Exception ex)
                            {
                                Logislemleri.Uyari("TANILAMA", "Ekran görüntüsü alınamadı: " + ex.Message);
                            }
                            model.TanilamaKlasoru = Tanilama.HamVeriKaydet(ham, model.ModulType, model, ekran);
                            Logislemleri.Bilgi("TANILAMA", "Ham veri kaydedildi: " + model.TanilamaKlasoru);
                        }
                    }

                    HizSiniri.SonucBildir(model.ModulType, model.HataKodu == Globals.HataKodlari.ENGEL);
                    return false;
                }
                catch (Exception ex) when (zaman.IsCancellationRequested)
                {
                    bool genel = GenelIptal.IsCancellationRequested;
                    model.HataYaz(Globals.HataKodlari.ZAMAN_ASIMI, genel
                        ? "Çalışma iptal edildi / toplam süre doldu"
                        : "Link süresi (" + Globals.Ayarlar.LinkZamanAsimiSn + " sn) doldu");
                    Logislemleri.Uyari("ZAMAN_ASIMI", model.HataMesaji + " (" + ex.GetType().Name + ")");
                    if (ham != null) TanilamaKaydetGerekirse(model, ham);
                    return !genel;
                }
                catch (TimeoutException ex)
                {
                    model.HataYaz(Globals.HataKodlari.ZAMAN_ASIMI, "Sayfa zaman aşımı: " + IlkSatir(ex.Message));
                    Logislemleri.Uyari("ZAMAN_ASIMI", model.HataMesaji);
                    return true;
                }
                catch (Exception ex)
                {
                    if (TarayiciYonetimi.CokmeHatasiMi(ex))
                    {
                        model.HataYaz(Globals.HataKodlari.BILINMEYEN, "Tarayıcı çöktü/kapandı: " + IlkSatir(ex.Message));
                        Logislemleri.Hata("TARAYICI_COKTU", "Tarayıcı çöktü, sonraki denemede yeniden açılacak", ex);
                    }
                    else if (TarayiciYonetimi.TarayiciKuruluDegilHatasiMi(ex))
                    {
                        model.HataYaz(Globals.HataKodlari.BILINMEYEN, "Chromium kurulu değil (--kurulum çalıştırın)");
                        Logislemleri.Kritik("TARAYICI_YOK", "Chromium kurulu değil", ex);
                    }
                    else if (ex.Message != null && (ex.Message.Contains("net::ERR_") || ex.Message.Contains("NS_ERROR")))
                    {
                        model.HataYaz(Globals.HataKodlari.BILINMEYEN, "Ağ hatası: " + IlkSatir(ex.Message));
                        Logislemleri.Uyari("AG_HATASI", model.HataMesaji);
                    }
                    else
                    {
                        model.HataYaz(Globals.HataKodlari.BILINMEYEN, IlkSatir(ex.Message));
                        Logislemleri.Hata("HATA", "Link işlenirken hata", ex);
                    }
                    if (ham != null) TanilamaKaydetGerekirse(model, ham);
                    return false;
                }
                finally
                {
                    if (baglam != null) await BaglamiKapatBekle(baglam);
                    if (kilitAlindi) esZamanli.Release();
                }
            }
        }

        /// <summary>
        /// Ham veriden modeli doldurur. Tarayıcı gerektirmez: testler ve kayıtlı tanılama klasörleri bunu kullanır.
        /// </summary>
        public static void HamVeridenDoldur(HamSayfaVerisi ham, PlatformAyari ayar, UrunBilgiModel model, Action<AlanOkuyucu, UrunBilgiModel> ayristirici)
        {
            string aciklama;
            string kesin = DurumTespiti.KesinDurum(ham, ayar, out aciklama);
            if (kesin != null)
            {
                model.HataYaz(kesin, Globals.ModulAdi(model.ModulType) + " sayfası: " + aciklama);
                if (kesin == Globals.HataKodlari.ENGEL) Logislemleri.Uyari("ENGEL", "Bot koruması/erişim engeli tespit edildi, atlatılmaya çalışılmadı: " + aciklama);
                else Logislemleri.Uyari(kesin, aciklama);
                return;
            }

            Stopwatch sw = Stopwatch.StartNew();
            AlanOkuyucu oku = new AlanOkuyucu(ham, ayar, model);
            OrtakAlanlar.Doldur(oku, model);
            ayristirici(oku, model);
            OrtakAlanlar.Tamamla(model, ayar);

            if (Globals.Ayarlar.HamJsonDondur) model.HamJson = HamJsonOlustur(ham);

            // Ana veri kaynağı: ürün adı ve fiyatın geldiği kaynak
            model.VeriKaynagi = KaynakTuru(model, "SatisFiyati") ?? KaynakTuru(model, "UrunAdi");

            // Zorunlu alanlar
            List<string> eksikler = new List<string>();
            if (string.IsNullOrWhiteSpace(model.UrunAdi)) eksikler.Add("UrunAdi");
            if (model.SatisFiyati == null)
            {
                if (model.TukendiMi == true) model.UyariEkle("ürün tükenmiş, satış fiyatı yok");
                else eksikler.Add("SatisFiyati");
            }
            if (model.Satici == null || string.IsNullOrWhiteSpace(model.Satici.SaticiAdi)) eksikler.Add("Satici.SaticiAdi");

            Logislemleri.Detay("KAYNAK_DURUMU", "Kaynaklar ve kök sayıları", null, oku.KaynakDurumu());
            Logislemleri.Bilgi("AYRISTIRILDI", "Alan sayısı: " + model.AlanKaynaklari.Count + ", ana kaynak: " + (model.VeriKaynagi ?? "-")
                + (eksikler.Count > 0 ? ", eksik zorunlu alanlar: " + string.Join(", ", eksikler) : ""), sw.ElapsedMilliseconds, model.AlanKaynaklari);

            string htmlDurum = DurumTespiti.HtmlDurumu(ham, ayar, eksikler.Count > 0, out aciklama);
            if (htmlDurum != null)
            {
                model.HataYaz(htmlDurum, Globals.ModulAdi(model.ModulType) + " sayfası: " + aciklama);
                Logislemleri.Uyari(htmlDurum, aciklama);
                return;
            }

            if (eksikler.Count > 0)
            {
                model.HataYaz(Globals.HataKodlari.VERI_YAPISI_DEGISTI, "Zorunlu alan(lar) hiçbir kaynaktan okunamadı: " + string.Join(", ", eksikler)
                    + ". secici-ayarlari.json güncellenmeli.");
                Logislemleri.Kritik("VERI_YAPISI_DEGISTI", model.HataMesaji + " | sayfa başlığı: '" + ham.SayfaBasligi + "' | " + string.Join("; ", ham.ToplamaNotlari),
                    null, null, oku.KaynakDurumu());
                return;
            }

            model.Basarili = true;
        }

        /// <summary>Kayıtlı ham veriden (tanılama klasörü / test örneği) model üretir.</summary>
        public static UrunBilgiModel HamVeridenModel(Globals.ModulType modulType, HamSayfaVerisi ham, Action<AlanOkuyucu, UrunBilgiModel> ayristirici)
        {
            UrunBilgiModel model = YeniModel(modulType, ham.IstenenLink, Guid.NewGuid());
            using (Logislemleri.IslemBaglamiAc(model))
            {
                try
                {
                    PlatformAyari ayar = SeciciAyarlari.Platform(modulType);
                    if (ayar == null)
                    {
                        model.HataYaz(Globals.HataKodlari.BILINMEYEN, "secici-ayarlari.json içinde '" + Globals.ModulAdi(modulType) + "' bölümü yok");
                        return model;
                    }
                    LinkSonucu ls = LinkYardimcisi.Dogrula(!string.IsNullOrEmpty(ham.SonUrl) ? ham.SonUrl : ham.IstenenLink, ayar);
                    model.NormalizeLink = ls.NormalizeLink;
                    HamVeridenDoldur(ham, ayar, model, ayristirici);
                }
                catch (Exception ex)
                {
                    model.HataYaz(Globals.HataKodlari.BILINMEYEN, ex.Message);
                    Logislemleri.Hata("AYRISTIRMA", "Kayıtlı veri ayrıştırılamadı", ex);
                }
            }
            return model;
        }

        public static UrunBilgiModel YeniModel(Globals.ModulType modulType, string link, Guid islemId)
        {
            UrunBilgiModel m = new UrunBilgiModel();
            m.IslemID = islemId;
            m.CalismaID = Globals.CalismaID;
            m.ModulType = modulType;
            m.Link = link;
            m.IstekZamani = DateTime.Now;
            return m;
        }

        // ---------- Yardımcılar ----------

        private static bool KisaLinkSonrasiDogrula(UrunBilgiModel model, PlatformAyari ayar, HamSayfaVerisi ham)
        {
            LinkSonucu son = LinkYardimcisi.Dogrula(ham.SonUrl, ayar);
            if (!son.Gecerli || son.KisaLinkMi)
            {
                model.HataYaz(Globals.HataKodlari.GECERSIZ_LINK, "Kısa link geçerli bir ürün sayfasına yönlenmedi: " + ham.SonUrl + " (" + son.Hata + ")");
                return false;
            }
            Logislemleri.Bilgi("KISA_LINK", "Kısa link yönlendi: " + son.NormalizeLink);
            model.NormalizeLink = son.NormalizeLink;
            return true;
        }

        private static bool TanilamaGerekliMi(UrunBilgiModel model)
        {
            if (Globals.Ayarlar.HerZamanTanilamaKaydet) return true;
            if (model.HataKodu == Globals.HataKodlari.VERI_YAPISI_DEGISTI) return true;
            if (!model.Basarili && Globals.Ayarlar.HatadaTanilamaKaydet && model.HataKodu != Globals.HataKodlari.GECERSIZ_LINK) return true;
            return false;
        }

        private static void TanilamaKaydetGerekirse(UrunBilgiModel model, HamSayfaVerisi ham)
        {
            if (TanilamaGerekliMi(model)) model.TanilamaKlasoru = Tanilama.HamVeriKaydet(ham, model.ModulType, model, null);
        }

        private static string KaynakTuru(UrunBilgiModel model, string alan)
        {
            string k;
            if (!model.AlanKaynaklari.TryGetValue(alan, out k) || string.IsNullOrEmpty(k)) return null;
            int i = k.IndexOf(':');
            return i > 0 ? k.Substring(0, i) : k;
        }

        private static string HamJsonOlustur(HamSayfaVerisi ham)
        {
            JObject o = new JObject();
            JArray ag = new JArray();
            foreach (AgCevabi c in ham.AgCevaplari)
            {
                if (string.IsNullOrEmpty(c.Govde)) continue;
                JObject a = new JObject();
                a["url"] = c.Url;
                a["govde"] = JsonYolu.GuvenliParse(c.Govde) ?? c.Govde;
                ag.Add(a);
            }
            o["agCevaplari"] = ag;
            JObject g = new JObject();
            foreach (KeyValuePair<string, string> kv in ham.Globaller) g[kv.Key] = JsonYolu.GuvenliParse(kv.Value) ?? kv.Value;
            o["globaller"] = g;
            JObject s = new JObject();
            foreach (KeyValuePair<string, string> kv in ham.ScriptJsonlari) s[kv.Key] = JsonYolu.GuvenliParse(kv.Value) ?? kv.Value;
            o["scriptler"] = s;
            JArray ld = new JArray();
            foreach (string l in ham.LdJsonlar) ld.Add(JsonYolu.GuvenliParse(l) ?? l);
            o["ldjson"] = ld;
            return o.ToString(Formatting.None);
        }

        private static SemaphoreSlim EsZamanliKilit()
        {
            lock (_kilit)
            {
                if (_esZamanli == null) _esZamanli = new SemaphoreSlim(Math.Max(1, Globals.Ayarlar.MaksimumEszamanliSayfa));
                return _esZamanli;
            }
        }

        private static void BaglamiKapat(IBrowserContext baglam)
        {
            try
            {
                // Register callback'i senkron; kapanışı beklemeden başlat
                Task t = baglam.CloseAsync();
                t.ContinueWith(delegate (Task x)
                {
                    if (x.Exception != null) Logislemleri.Detay("BAGLAM_KAPAT", "Zaman aşımı sonrası kapatma: " + x.Exception.GetBaseException().Message);
                });
            }
            catch (Exception ex)
            {
                Logislemleri.Detay("BAGLAM_KAPAT", "Zaman aşımı sonrası kapatma: " + ex.Message);
            }
        }

        private static async Task BaglamiKapatBekle(IBrowserContext baglam)
        {
            try
            {
                Task t = baglam.CloseAsync();
                if (await Task.WhenAny(t, Task.Delay(10000)) != t) Logislemleri.Uyari("BAGLAM_KAPAT", "Tarayıcı bağlamı 10 sn içinde kapanmadı");
            }
            catch (Exception ex)
            {
                Logislemleri.Detay("BAGLAM_KAPAT", "Bağlam kapatılamadı (zaten kapalı olabilir): " + ex.Message);
            }
        }

        private static string IlkSatir(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            int i = s.IndexOf('\n');
            return i > 0 ? s.Substring(0, i).Trim() : s.Trim();
        }
    }
}
