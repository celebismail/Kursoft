using System.Diagnostics;
using KursoftRekabet.Ayristirma;
using Microsoft.Playwright;

namespace KursoftRekabet.Tarayici
{
    /// <summary>
    /// Chromium'u bir kere açar, her link için temiz bir BrowserContext verir.
    /// Tarayıcı çökerse/bağlantı koparsa bir sonraki istekte otomatik yeniden açar.
    /// Tarayıcılar exe'nin yanındaki "tarayicilar" klasörüne kurulur (kullanıcı profiline değil).
    /// </summary>
    public static class TarayiciYonetimi
    {
        private static IPlaywright _playwright;
        private static IBrowser _tarayici;
        private static readonly SemaphoreSlim _acmaKilidi = new SemaphoreSlim(1, 1);
        private static int _acilisSayisi = 0;

        /// <summary>Playwright'a tarayıcıları exe yanındaki klasörde aramasını söyler. Her şeyden önce çağrılmalı.</summary>
        public static void OrtamiHazirla()
        {
            Directory.CreateDirectory(Globals.TarayiciKlasoru);
            Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", Globals.TarayiciKlasoru);
        }

        /// <summary>Chromium'u kurar ya da kurulu olduğunu doğrular. Başarılıysa true.</summary>
        public static bool KurulumYap()
        {
            OrtamiHazirla();
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                Logislemleri.Bilgi("KURULUM", "Chromium kuruluyor/kontrol ediliyor: " + Globals.TarayiciKlasoru);
                int kod = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
                if (kod != 0)
                {
                    Logislemleri.Hata("KURULUM", "Chromium kurulamadı, çıkış kodu: " + kod, null, sw.ElapsedMilliseconds);
                    return false;
                }
                Logislemleri.Bilgi("KURULUM", "Chromium hazır", sw.ElapsedMilliseconds);
                return true;
            }
            catch (Exception ex)
            {
                Logislemleri.Hata("KURULUM", "Chromium kurulumu sırasında hata", ex, sw.ElapsedMilliseconds);
                return false;
            }
        }

        /// <summary>Tarayıcıyı açar (açıksa ve bağlıysa olanı verir).</summary>
        public static async Task<IBrowser> TarayiciAlAsync()
        {
            IBrowser mevcut = _tarayici;
            if (mevcut != null && mevcut.IsConnected) return mevcut;

            await _acmaKilidi.WaitAsync();
            try
            {
                if (_tarayici != null && _tarayici.IsConnected) return _tarayici;

                if (_tarayici != null)
                {
                    Logislemleri.Uyari("TARAYICI_YENIDEN", "Tarayıcı bağlantısı kopmuş, yeniden açılıyor");
                    await GuvenliKapatAsync(_tarayici);
                    _tarayici = null;
                }

                Stopwatch sw = Stopwatch.StartNew();
                if (_playwright == null) _playwright = await Playwright.CreateAsync();

                BrowserTypeLaunchOptions secenek = new BrowserTypeLaunchOptions();
                secenek.Headless = Globals.Ayarlar.Headless;
                if (!string.IsNullOrWhiteSpace(Globals.Ayarlar.TarayiciYolu)) secenek.ExecutablePath = Globals.Ayarlar.TarayiciYolu;
                // SYSTEM hesabı / oturumsuz çalışmada GPU ve sandbox sorun çıkarabiliyor
                secenek.Args = new[] { "--disable-gpu", "--disable-dev-shm-usage", "--no-first-run", "--no-default-browser-check" };

                _tarayici = await _playwright.Chromium.LaunchAsync(secenek);
                _acilisSayisi++;
                Logislemleri.Bilgi("TARAYICI_ACILDI", "Chromium açıldı (" + _tarayici.Version + ", açılış #" + _acilisSayisi + ")", sw.ElapsedMilliseconds);
                return _tarayici;
            }
            finally
            {
                _acmaKilidi.Release();
            }
        }

        /// <summary>Temiz çerezli yeni bir bağlam. Medya engelleme ayara göre.</summary>
        public static async Task<IBrowserContext> YeniBaglamAsync(PlatformAyari ayar)
        {
            IBrowser tarayici = await TarayiciAlAsync();
            BrowserNewContextOptions secenek = new BrowserNewContextOptions();
            secenek.Locale = "tr-TR";
            secenek.TimezoneId = "Europe/Istanbul";
            secenek.ViewportSize = new ViewportSize { Width = 1366, Height = 900 };
            if (!string.IsNullOrWhiteSpace(Globals.Ayarlar.KullaniciAjani)) secenek.UserAgent = Globals.Ayarlar.KullaniciAjani;

            IBrowserContext baglam = await tarayici.NewContextAsync(secenek);
            baglam.SetDefaultTimeout(Math.Max(5, Globals.Ayarlar.SayfaYuklemeZamanAsimiSn) * 1000);

            bool medyaEngelle = ayar.MedyaEngelle ?? Globals.Ayarlar.MedyaEngelle;
            if (medyaEngelle)
            {
                await baglam.RouteAsync("**/*", async delegate (IRoute route)
                {
                    try
                    {
                        string tur = route.Request.ResourceType;
                        if (tur == "image" || tur == "media" || tur == "font") await route.AbortAsync();
                        else await route.ContinueAsync();
                    }
                    catch (Exception ex)
                    {
                        // Sayfa kapanırken gelen istekler: önemsiz
                        Logislemleri.Detay("ROUTE", "İstek yönlendirilemedi: " + ex.Message);
                    }
                });
            }
            return baglam;
        }

        /// <summary>Exception tarayıcının çöktüğünü/kapandığını mı gösteriyor? (Geçici hata sayılır.)</summary>
        public static bool CokmeHatasiMi(Exception ex)
        {
            if (ex == null) return false;
            string m = ex.Message ?? "";
            if (m.Contains("Target page, context or browser has been closed") || m.Contains("Browser has been closed")
                || m.Contains("Target closed") || m.Contains("browser has disconnected") || m.Contains("Connection closed")
                || m.Contains("Browser closed"))
                return true;
            return ex.InnerException != null && CokmeHatasiMi(ex.InnerException);
        }

        public static bool TarayiciKuruluDegilHatasiMi(Exception ex)
        {
            return ex != null && ex.Message != null && (ex.Message.Contains("Executable doesn't exist") || ex.Message.Contains("playwright install"));
        }

        public static async Task KapatAsync()
        {
            try
            {
                if (_tarayici != null)
                {
                    await GuvenliKapatAsync(_tarayici);
                    _tarayici = null;
                    Logislemleri.Bilgi("TARAYICI_KAPANDI", "Chromium kapatıldı");
                }
                if (_playwright != null)
                {
                    _playwright.Dispose();
                    _playwright = null;
                }
            }
            catch (Exception ex)
            {
                Logislemleri.Uyari("TARAYICI_KAPAT", "Tarayıcı kapatılırken hata: " + ex.Message);
            }
        }

        private static async Task GuvenliKapatAsync(IBrowser t)
        {
            try
            {
                Task kapat = t.CloseAsync();
                // Askıda kalan tarayıcı uygulamayı kilitlemesin
                if (await Task.WhenAny(kapat, Task.Delay(10000)) != kapat) Logislemleri.Uyari("TARAYICI_KAPAT", "Tarayıcı 10 sn içinde kapanmadı");
            }
            catch (Exception ex)
            {
                Logislemleri.Detay("TARAYICI_KAPAT", "Kapatma hatası (yoksayıldı): " + ex.Message);
            }
        }

        /// <summary>
        /// Önceki çalışmadan kalmış, BİZİM tarayici klasörümüzden açılmış chromium süreçlerini kapatır.
        /// Başka uygulamaların Chrome'una dokunmaz.
        /// </summary>
        public static void YetimSurecleriTemizle()
        {
            string klasor = Path.GetFullPath(Globals.TarayiciKlasoru);
            int temizlenen = 0;
            string[] adlar = { "chrome", "chromium", "headless_shell", "chrome-headless-shell" };
            foreach (string ad in adlar)
            {
                Process[] surecler;
                try
                {
                    surecler = Process.GetProcessesByName(ad);
                }
                catch (Exception ex)
                {
                    Logislemleri.Detay("YETIM_SUREC", "Süreç listesi alınamadı: " + ex.Message);
                    continue;
                }

                foreach (Process p in surecler)
                {
                    try
                    {
                        string yol = p.MainModule != null ? p.MainModule.FileName : null;
                        if (yol != null && yol.StartsWith(klasor, StringComparison.OrdinalIgnoreCase))
                        {
                            p.Kill(true);
                            temizlenen++;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Başka kullanıcının süreci olabilir (erişim yok): bizim değil, geç
                        Logislemleri.Detay("YETIM_SUREC", "Süreç incelenemedi (pid " + SafePid(p) + "): " + ex.Message);
                    }
                    finally
                    {
                        p.Dispose();
                    }
                }
            }
            if (temizlenen > 0) Logislemleri.Uyari("YETIM_SUREC", "Önceki çalışmadan kalan " + temizlenen + " chromium süreci kapatıldı");
        }

        private static string SafePid(Process p)
        {
            try
            {
                return p.Id.ToString();
            }
            catch (Exception ex)
            {
                // Süreç bu arada kapanmış olabilir
                Logislemleri.Detay("YETIM_SUREC", "pid okunamadı: " + ex.Message);
                return "?";
            }
        }
    }
}
