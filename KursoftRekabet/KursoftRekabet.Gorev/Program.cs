using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using KursoftRekabet.Ayristirma;
using KursoftRekabet.model;
using KursoftRekabet.Tarayici;
using Newtonsoft.Json;

namespace KursoftRekabet.Gorev
{
    /// <summary>
    /// Zamanlanmış görev bu exe'yi çalıştırır. Arayüz yok, kullanıcıdan girdi beklemez; işini yapıp exit code ile kapanır.
    ///
    ///   (argümansız)               DB'den linkleri oku, işle, sonuçları yaz
    ///   --link "https://..."       tek link, sonuç JSON'u Logs/ altına + konsola (DB'ye dokunmaz)
    ///   --tanila "https://..."     tek link, Detay log açık, tüm ham veri Tanilama klasörüne
    ///   --yenidenayristir "klasör" kayıtlı tanılama klasörünü tarayıcısız yeniden ayrıştır (seçici denemek için)
    ///   --kurulum                  sadece Chromium kur/kontrol et
    /// </summary>
    public static class Program
    {
        public const int EXIT_BASARILI = 0;
        public const int EXIT_BAZILARI_BASARISIZ = 1;
        public const int EXIT_HEPSI_BASARISIZ = 2;
        public const int EXIT_BASKA_KOPYA = 3;
        public const int EXIT_AYAR_DB_HATASI = 4;
        public const int EXIT_TARAYICI_HATASI = 5;

        private const string MutexAdi = @"Global\KursoftRekabet_Gorev_v2";

        private static readonly CancellationTokenSource _kullaniciIptali = new CancellationTokenSource();
        private static readonly ManualResetEventSlim _kapanisBitti = new ManualResetEventSlim(false);
        private static GorevAyarlari _gorevAyar;
        private static DbIslemleri _db;

        public static int Main(string[] args)
        {
            // Görev System32'den başlatabilir: her şey exe klasörüne göre
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            TarayiciYonetimi.OrtamiHazirla();

            int kod;
            try
            {
                kod = CalistirAsync(args).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                // Buraya düşmemeli; düşerse de log + anlamlı kod
                Logislemleri.Kritik("PROGRAM", "Yakalanmamış hata", ex);
                kod = EXIT_HEPSI_BASARISIZ;
            }
            finally
            {
                Logislemleri.Kapat();
                _kapanisBitti.Set();
            }
            return kod;
        }

        private static async Task<int> CalistirAsync(string[] args)
        {
            Komut komut = Komut.Ayristir(args);

            // 1) Ayarlar (log henüz ayarsız: hata olursa varsayılan log ayarıyla yazılır)
            CalismaAyarlari cekirdekAyar;
            try
            {
                GorevAyarlari.Oku(AppContext.BaseDirectory, out _gorevAyar, out cekirdekAyar);
                Globals.Ayarlar = cekirdekAyar;
            }
            catch (Exception ex)
            {
                Logislemleri.Baslat(Globals.Ayarlar, true);
                Logislemleri.Kritik("AYAR", "appsettings.json okunamadı", ex);
                return EXIT_AYAR_DB_HATASI;
            }

            if (komut.Tur == KomutTuru.Tanila)
            {
                Globals.Ayarlar.DetayLog = true;
                Globals.Ayarlar.HerZamanTanilamaKaydet = true;
            }
            Logislemleri.Baslat(Globals.Ayarlar, true);
            Logislemleri.Bilgi("BASLANGIC", "KursoftRekabet " + Globals.UygulamaSurumu + " | mod: " + komut.Tur + " | kullanıcı: "
                + Environment.UserName + " | klasör: " + AppContext.BaseDirectory, null, _gorevAyar.LogOzeti(Globals.Ayarlar));

            if (komut.Tur == KomutTuru.Hatali)
            {
                Logislemleri.Kritik("ARGUMAN", komut.Hata + ". Kullanım: (argümansız) | --link URL | --tanila URL | --yenidenayristir KLASOR | --kurulum");
                return EXIT_AYAR_DB_HATASI;
            }

            // 2) Tekil çalışma
            Mutex mutex = null;
            bool mutexSahibi = false;
            try
            {
                mutex = new Mutex(false, MutexAdi);
                try
                {
                    mutexSahibi = mutex.WaitOne(0);
                }
                catch (AbandonedMutexException)
                {
                    // Önceki kopya kapanmadan öldü: mutex artık bizim
                    mutexSahibi = true;
                    Logislemleri.Uyari("MUTEX", "Önceki çalışma düzgün kapanmamış (terk edilmiş mutex devralındı)");
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Başka kullanıcı (ör. SYSTEM) ile çalışan kopya mutex'i tutuyor
                mutexSahibi = false;
            }

            if (!mutexSahibi)
            {
                Logislemleri.Uyari("BASKA_KOPYA", "Başka bir KursoftRekabet kopyası çalışıyor, çıkılıyor");
                if (mutex != null) mutex.Dispose();
                return EXIT_BASKA_KOPYA;
            }

            List<PosixSignalRegistration> sinyaller = SinyalleriDinle();
            try
            {
                return await KomutuCalistirAsync(komut);
            }
            finally
            {
                await TarayiciYonetimi.KapatAsync();
                foreach (PosixSignalRegistration s in sinyaller) s.Dispose();
                try
                {
                    mutex.ReleaseMutex();
                }
                catch (Exception ex)
                {
                    Logislemleri.Detay("MUTEX", "Mutex bırakılamadı: " + ex.Message);
                }
                mutex.Dispose();
            }
        }

        private static async Task<int> KomutuCalistirAsync(Komut komut)
        {
            // Seçici ayarları
            try
            {
                SeciciAyarlari.Yukle(Globals.SeciciAyarDosyasi);
                Logislemleri.Bilgi("AYAR", "Seçici ayarları yüklendi: " + SeciciAyarlari.TumPlatformlar().Count + " platform");
            }
            catch (Exception ex)
            {
                Logislemleri.Kritik("AYAR", "secici-ayarlari.json okunamadı/hatalı", ex);
                return EXIT_AYAR_DB_HATASI;
            }

            if (komut.Tur == KomutTuru.Kurulum)
            {
                return TarayiciYonetimi.KurulumYap() ? EXIT_BASARILI : EXIT_TARAYICI_HATASI;
            }

            if (komut.Tur == KomutTuru.YenidenAyristir)
            {
                return YenidenAyristir(komut.Deger);
            }

            // Tarayıcı hazır mı (yoksa kur)
            if (!await TarayiciHazirla()) return EXIT_TARAYICI_HATASI;

            // Toplam süre sınırı + Ctrl+C / görev sonlandırma
            CancellationTokenSource toplamSure = new CancellationTokenSource(TimeSpan.FromMinutes(Math.Max(1, _gorevAyar.ToplamZamanAsimiDk)));
            CancellationTokenSource genel = CancellationTokenSource.CreateLinkedTokenSource(toplamSure.Token, _kullaniciIptali.Token);
            UrunBilgiAkisi.GenelIptal = genel.Token;
            toplamSure.Token.Register(delegate
            {
                Logislemleri.Uyari("TOPLAM_ZAMAN_ASIMI", "Toplam süre (" + _gorevAyar.ToplamZamanAsimiDk + " dk) doldu, kalan linkler ZAMAN_ASIMI ile işaretleniyor");
            });

            if (komut.Tur == KomutTuru.Link || komut.Tur == KomutTuru.Tanila)
            {
                return await TekLinkAsync(komut);
            }
            return await NormalCalismaAsync();
        }

        // ---------- Normal çalışma (DB) ----------

        private static async Task<int> NormalCalismaAsync()
        {
            if (string.IsNullOrWhiteSpace(_gorevAyar.BaglantiCumlesi) || string.IsNullOrWhiteSpace(_gorevAyar.LinkSorgusu))
            {
                Logislemleri.Kritik("AYAR", "BaglantiCumlesi ve LinkSorgusu appsettings.json'da dolu olmalı");
                return EXIT_AYAR_DB_HATASI;
            }

            _db = new DbIslemleri(_gorevAyar);
            List<LinkKaydi> linkler;
            try
            {
                linkler = _db.LinkleriOku();
                Logislemleri.Bilgi("LINKLER_OKUNDU", linkler.Count + " link okundu");
            }
            catch (Exception ex)
            {
                Logislemleri.Kritik("DB", "Linkler okunamadı", ex);
                return EXIT_AYAR_DB_HATASI;
            }

            CalismaOzeti ozet = new CalismaOzeti();
            ozet.PlanlananLink = linkler.Count;
            ConcurrentQueue<LinkKaydi> kuyruk = new ConcurrentQueue<LinkKaydi>(linkler);

            // Eşzamanlılık çekirdekte de sınırlı; burada da o kadar işçi açıyoruz ki binlerce bekleyen görev oluşmasın
            int isciSayisi = Math.Max(1, Globals.Ayarlar.MaksimumEszamanliSayfa);
            List<Task> isciler = new List<Task>();
            for (int i = 0; i < isciSayisi; i++)
            {
                isciler.Add(Task.Run(async delegate
                {
                    LinkKaydi k;
                    while (kuyruk.TryDequeue(out k))
                    {
                        // Ctrl+C: yarım işi bırak, kalanları işleme. (Toplam süre dolduysa kalanlar ZAMAN_ASIMI ile yazılır.)
                        if (_kullaniciIptali.IsCancellationRequested) break;

                        UrunBilgiModel m;
                        if (k.ModulType != Globals.ModulType.bilinmiyor) m = await UrunBilgiGetir.urunbilgigetir(k.Link, k.ModulType);
                        else m = await UrunBilgiGetir.urunbilgigetir(k.Link);
                        ozet.Ekle(m);

                        try
                        {
                            SonucuIsle(k.ID, m);
                        }
                        catch (Exception ex)
                        {
                            using (Logislemleri.IslemBaglamiAc(m))
                            {
                                Logislemleri.Hata("SONUC_ISLE", "Sonuç işlenemedi (kayıt ID: " + k.ID + ")", ex);
                            }
                        }

                        if (_gorevAyar.DbLogAcik) _db.IslemLogYaz(k.ID, m);
                    }
                }));
            }
            await Task.WhenAll(isciler);

            ozet.IptalEdildi = _kullaniciIptali.IsCancellationRequested;
            int kod = ozet.ExitCode();
            ozet.LogYaz(kod);
            if (_gorevAyar.DbLogAcik) _db.CalismaLogYaz(ozet, kod);
            return kod;
        }

        // =====================================================================================
        //  ÖZELLEŞTİRME NOKTASI
        //  Her linkin sonucu buraya gelir. Varsayılan: SonucTabloAdi tablosuna parametreli INSERT
        //  (temel alanlar + modelin tam JSON'u). Kendi işlemenizi buraya yazın.
        //  Exception fırlatırsa çağıran loglar ve sonraki linke geçer.
        // =====================================================================================
        public static void SonucuIsle(string kayitID, UrunBilgiModel model)
        {
            _db.SonucYaz(kayitID, model);
        }

        // ---------- Tek link (--link / --tanila) ----------

        private static async Task<int> TekLinkAsync(Komut komut)
        {
            UrunBilgiModel m = await UrunBilgiGetir.urunbilgigetir(komut.Deger);
            string json = JsonConvert.SerializeObject(m, Formatting.Indented);
            string dosya = Path.Combine(Globals.LogKlasoru, "sonuc_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + m.IslemID.ToString("N").Substring(0, 8) + ".json");
            try
            {
                Directory.CreateDirectory(Globals.LogKlasoru);
                File.WriteAllText(dosya, json);
            }
            catch (Exception ex)
            {
                Logislemleri.Hata("SONUC_DOSYASI", "Sonuç dosyası yazılamadı: " + dosya, ex);
            }

            Console.WriteLine();
            Console.WriteLine("==================== SONUÇ ====================");
            Console.WriteLine(json);
            Console.WriteLine("===============================================");
            Console.WriteLine("Sonuç dosyası: " + dosya);
            if (!string.IsNullOrEmpty(m.TanilamaKlasoru)) Console.WriteLine("Tanılama klasörü: " + m.TanilamaKlasoru);

            CalismaOzeti ozet = new CalismaOzeti();
            ozet.PlanlananLink = 1;
            ozet.Ekle(m);
            int kod = ozet.ExitCode();
            ozet.LogYaz(kod);
            return kod;
        }

        private static int YenidenAyristir(string klasor)
        {
            if (!Directory.Exists(klasor) || !File.Exists(Path.Combine(klasor, "ozet.json")))
            {
                Logislemleri.Kritik("ARGUMAN", "Tanılama klasörü bulunamadı ya da ozet.json yok: " + klasor);
                return EXIT_AYAR_DB_HATASI;
            }
            HamSayfaVerisi ham = Yardimcilar.Tanilama.HamVeriYukle(klasor);
            Globals.ModulType modul = Yardimcilar.LinkYardimcisi.PlatformBul(!string.IsNullOrEmpty(ham.SonUrl) ? ham.SonUrl : ham.IstenenLink);
            Logislemleri.DetayAc(true);
            UrunBilgiModel m = UrunBilgiGetir.hamveridenayristir(modul, ham);
            Console.WriteLine(JsonConvert.SerializeObject(m, Formatting.Indented));
            return m.Basarili ? EXIT_BASARILI : EXIT_HEPSI_BASARISIZ;
        }

        // ---------- Tarayıcı ----------

        private static async Task<bool> TarayiciHazirla()
        {
            TarayiciYonetimi.YetimSurecleriTemizle();
            try
            {
                await TarayiciYonetimi.TarayiciAlAsync();
                return true;
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrWhiteSpace(Globals.Ayarlar.TarayiciYolu) || !TarayiciYonetimi.TarayiciKuruluDegilHatasiMi(ex))
                {
                    Logislemleri.Kritik("TARAYICI", "Chromium başlatılamadı", ex);
                    return false;
                }
                Logislemleri.Uyari("TARAYICI", "Chromium kurulu değil, kuruluyor");
            }

            if (!TarayiciYonetimi.KurulumYap()) return false;
            try
            {
                await TarayiciYonetimi.TarayiciAlAsync();
                return true;
            }
            catch (Exception ex)
            {
                Logislemleri.Kritik("TARAYICI", "Kurulumdan sonra da Chromium başlatılamadı", ex);
                return false;
            }
        }

        // ---------- İptal sinyalleri ----------

        private static List<PosixSignalRegistration> SinyalleriDinle()
        {
            List<PosixSignalRegistration> liste = new List<PosixSignalRegistration>();
            PosixSignal[] sinyaller = { PosixSignal.SIGINT, PosixSignal.SIGTERM, PosixSignal.SIGQUIT };
            foreach (PosixSignal s in sinyaller)
            {
                try
                {
                    liste.Add(PosixSignalRegistration.Create(s, delegate (PosixSignalContext ctx)
                    {
                        // Süreci hemen öldürme: işçiler dursun, özet log yazılsın
                        ctx.Cancel = true;
                        if (!_kullaniciIptali.IsCancellationRequested)
                        {
                            Logislemleri.Uyari("IPTAL", "Kapatma sinyali alındı (" + ctx.Signal + "), yarım işler bırakılıp özet yazılacak");
                            _kullaniciIptali.Cancel();
                        }
                        // Windows konsol kapanışında süre kısıtlı; özet yazılana kadar en fazla 20 sn bekle
                        if (ctx.Signal == PosixSignal.SIGTERM || ctx.Signal == PosixSignal.SIGQUIT) _kapanisBitti.Wait(TimeSpan.FromSeconds(20));
                    }));
                }
                catch (Exception ex)
                {
                    Logislemleri.Detay("SINYAL", s + " dinlenemiyor: " + ex.Message);
                }
            }
            return liste;
        }

        // ---------- Komut satırı ----------

        private enum KomutTuru { Normal, Link, Tanila, Kurulum, YenidenAyristir, Hatali }

        private class Komut
        {
            public KomutTuru Tur = KomutTuru.Normal;
            public string Deger;
            public string Hata;

            public static Komut Ayristir(string[] args)
            {
                Komut k = new Komut();
                if (args == null || args.Length == 0) return k;

                string a = args[0].Trim().ToLowerInvariant();
                string deger = args.Length > 1 ? args[1] : null;
                if (a == "--link" || a == "--tanila" || a == "--yenidenayristir")
                {
                    if (string.IsNullOrWhiteSpace(deger))
                    {
                        k.Tur = KomutTuru.Hatali;
                        k.Hata = a + " için değer verilmedi";
                        return k;
                    }
                    k.Tur = a == "--link" ? KomutTuru.Link : a == "--tanila" ? KomutTuru.Tanila : KomutTuru.YenidenAyristir;
                    k.Deger = deger.Trim().Trim('"');
                    return k;
                }
                if (a == "--kurulum")
                {
                    k.Tur = KomutTuru.Kurulum;
                    return k;
                }
                k.Tur = KomutTuru.Hatali;
                k.Hata = "Bilinmeyen argüman: " + args[0];
                return k;
            }
        }
    }
}
