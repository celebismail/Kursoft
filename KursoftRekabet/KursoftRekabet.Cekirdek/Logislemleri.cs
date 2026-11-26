using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using KursoftRekabet.model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KursoftRekabet
{
    /// <summary>
    /// Tüm loglama buradan. Dışarıya açılan API: Detay / Bilgi / Uyari / Hata / Kritik.
    ///
    /// Dosya düzeni: Logs/{yyyy-MM-dd}/
    ///   genel.log        herkes (düz metin)
    ///   genel.jsonl      herkes (her satır bir JSON)
    ///   {platform}.log   sadece o platformun satırları
    ///   hatalar.log      Uyari ve üstü
    ///   hatalar.jsonl    Uyari ve üstü (JSON)
    /// Dosya LogDosyaMaksimumMb'yi geçerse genel_2.log, genel_3.log ... diye devam eder.
    ///
    /// İşlem bilgisi (IslemID, ModulType, Link) her çağrıda tekrar verilmesin diye
    /// IslemBaglamiAc() ile async akışa bağlanır; paralel işlemler birbirine karışmaz.
    /// </summary>
    public static class Logislemleri
    {
        public enum Seviye { DETAY = 0, BILGI = 1, UYARI = 2, HATA = 3, KRITIK = 4 }

        private static readonly object _kilit = new object();
        private static readonly Dictionary<string, StreamWriter> _yazicilar = new Dictionary<string, StreamWriter>();
        private static readonly Dictionary<string, int> _dosyaSiralari = new Dictionary<string, int>();
        private static readonly AsyncLocal<IslemBaglami> _baglam = new AsyncLocal<IslemBaglami>();

        private static readonly string _makineAdi = Environment.MachineName;
        private static readonly int _processId = Environment.ProcessId;
        private static bool _detayAcik = false;
        private static long _maksimumBoyut = 50L * 1024 * 1024;
        private static bool _konsolaYaz = true;

        // Hata/istisna metinlerinde bağlantı cümlesi parçası kalmasın
        private static readonly Regex _hassasDesen = new Regex(
            @"(password|pwd|user id|uid|user|access token|accesstoken|apikey|api_key)\s*=\s*[^;""'\s]*",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static void Baslat(CalismaAyarlari ayarlar, bool konsolaYaz)
        {
            _detayAcik = ayarlar.DetayLog;
            _maksimumBoyut = Math.Max(1, ayarlar.LogDosyaMaksimumMb) * 1024L * 1024L;
            _konsolaYaz = konsolaYaz;
            EskiLoglariTemizle(ayarlar.LogSaklamaGun);
        }

        public static void DetayAc(bool acik)
        {
            _detayAcik = acik;
        }

        public static bool DetayAcikMi
        {
            get { return _detayAcik; }
        }

        // ---------- Dış API ----------

        public static void Detay(string adim, string mesaj, long? sureMs = null, object ek = null)
        {
            if (!_detayAcik) return;
            Yaz(Seviye.DETAY, adim, mesaj, sureMs, null, ek);
        }

        public static void Bilgi(string adim, string mesaj, long? sureMs = null, object ek = null)
        {
            Yaz(Seviye.BILGI, adim, mesaj, sureMs, null, ek);
        }

        public static void Uyari(string adim, string mesaj, long? sureMs = null, object ek = null)
        {
            Yaz(Seviye.UYARI, adim, mesaj, sureMs, null, ek);
        }

        public static void Hata(string adim, string mesaj, Exception ex = null, long? sureMs = null, object ek = null)
        {
            Yaz(Seviye.HATA, adim, mesaj, sureMs, ex, ek);
        }

        public static void Kritik(string adim, string mesaj, Exception ex = null, long? sureMs = null, object ek = null)
        {
            Yaz(Seviye.KRITIK, adim, mesaj, sureMs, ex, ek);
        }

        /// <summary>
        /// Bu async akıştaki tüm log satırlarına IslemID / ModulType / Link ekler.
        /// using (Logislemleri.IslemBaglamiAc(model)) { ... }
        /// </summary>
        public static IDisposable IslemBaglamiAc(UrunBilgiModel model)
        {
            IslemBaglami onceki = _baglam.Value;
            _baglam.Value = new IslemBaglami
            {
                IslemID = model.IslemID.ToString(),
                ModulType = Globals.ModulAdi(model.ModulType),
                Link = model.Link
            };
            return new BaglamKapatici(onceki);
        }

        /// <summary>Bağlamdaki ModulType'ı günceller (link'ten platform sonradan bulunduğunda).</summary>
        public static void BaglamModulGuncelle(Globals.ModulType modulType)
        {
            if (_baglam.Value != null) _baglam.Value.ModulType = Globals.ModulAdi(modulType);
        }

        public static void Kapat()
        {
            lock (_kilit)
            {
                foreach (KeyValuePair<string, StreamWriter> kv in _yazicilar)
                {
                    try
                    {
                        kv.Value.Flush();
                        kv.Value.Dispose();
                    }
                    catch (Exception ex)
                    {
                        // Kapanışta yazılamazsa yapacak bir şey yok, en azından konsola düşsün
                        Console.Error.WriteLine("Log dosyası kapatılamadı: " + kv.Key + " " + ex.Message);
                    }
                }
                _yazicilar.Clear();
            }
        }

        /// <summary>Hassas bilgileri (şifre, kullanıcı vb.) maskeler. DB hata mesajları için de kullanılır.</summary>
        public static string Maskele(string metin)
        {
            if (string.IsNullOrEmpty(metin)) return metin;
            return _hassasDesen.Replace(metin, delegate (Match m)
            {
                int esittir = m.Value.IndexOf('=');
                return m.Value.Substring(0, esittir + 1) + "***";
            });
        }

        // ---------- İç taraf ----------

        private static void Yaz(Seviye seviye, string adim, string mesaj, long? sureMs, Exception ex, object ek)
        {
            try
            {
                DateTime simdi = DateTime.Now;
                IslemBaglami b = _baglam.Value;
                string modul = b != null ? b.ModulType : null;
                string temizMesaj = Maskele(mesaj);
                string hataMetni = ex != null ? Maskele(ex.GetType().Name + ": " + ex.Message) : null;
                string yigin = ex != null ? Maskele(ex.ToString()) : null;

                // Düz metin satırı
                StringBuilder sb = new StringBuilder();
                sb.Append(simdi.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                sb.Append(" [").Append(seviye.ToString().PadRight(6)).Append("]");
                if (modul != null) sb.Append(" [").Append(modul).Append("]");
                if (b != null) sb.Append(" [").Append(b.IslemID.Substring(0, 8)).Append("]");
                sb.Append(" ").Append(adim ?? "-");
                sb.Append(" | ").Append(temizMesaj);
                if (sureMs.HasValue) sb.Append(" (").Append(sureMs.Value).Append(" ms)");
                if (hataMetni != null) sb.Append(" | ").Append(hataMetni);
                if (b != null && !string.IsNullOrEmpty(b.Link)) sb.Append(" | ").Append(b.Link);
                string metinSatiri = sb.ToString();

                // JSON satırı
                JObject j = new JObject();
                j["zaman"] = simdi.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
                j["seviye"] = seviye.ToString();
                j["calismaId"] = Globals.CalismaID;
                j["islemId"] = b != null ? b.IslemID : null;
                j["modulType"] = modul;
                j["link"] = b != null ? b.Link : null;
                j["adim"] = adim;
                j["mesaj"] = temizMesaj;
                j["sureMs"] = sureMs;
                j["makine"] = _makineAdi;
                j["pid"] = _processId;
                j["surum"] = Globals.UygulamaSurumu;
                if (hataMetni != null)
                {
                    j["hata"] = hataMetni;
                    j["yigin"] = yigin;
                }
                if (ek != null)
                {
                    try
                    {
                        j["ek"] = JToken.FromObject(ek);
                    }
                    catch (Exception jex)
                    {
                        j["ek"] = "(ek serileştirilemedi: " + jex.Message + ")";
                    }
                }
                string jsonSatiri = j.ToString(Formatting.None);

                string gunKlasoru = Path.Combine(Globals.LogKlasoru, simdi.ToString("yyyy-MM-dd"));

                lock (_kilit)
                {
                    DosyayaYaz(gunKlasoru, "genel", ".log", metinSatiri);
                    DosyayaYaz(gunKlasoru, "genel", ".jsonl", jsonSatiri);
                    if (!string.IsNullOrEmpty(modul)) DosyayaYaz(gunKlasoru, modul, ".log", metinSatiri);
                    if (seviye >= Seviye.UYARI)
                    {
                        DosyayaYaz(gunKlasoru, "hatalar", ".log", metinSatiri);
                        DosyayaYaz(gunKlasoru, "hatalar", ".jsonl", jsonSatiri);
                        if (yigin != null) DosyayaYaz(gunKlasoru, "hatalar", ".log", "    " + yigin.Replace("\n", "\n    "));
                    }

                    if (_konsolaYaz)
                    {
                        if (seviye >= Seviye.UYARI) Console.Error.WriteLine(metinSatiri);
                        else Console.WriteLine(metinSatiri);
                    }
                }
            }
            catch (Exception yazmaHatasi)
            {
                // Log yazılamıyor diye uygulama durmasın
                try
                {
                    Console.Error.WriteLine("LOG YAZILAMADI: " + yazmaHatasi.Message + " | " + mesaj);
                }
                catch (Exception konsolHatasi)
                {
                    // Konsol da yoksa (görev penceresiz) son çare: hata ayıklayıcı izine yaz
                    Debug.WriteLine("LOG YAZILAMADI: " + yazmaHatasi.Message + " / " + konsolHatasi.Message);
                }
            }
        }

        // _kilit içinde çağrılır
        private static void DosyayaYaz(string klasor, string ad, string uzanti, string satir)
        {
            string anahtar = klasor + "|" + ad + uzanti;
            int sira;
            if (!_dosyaSiralari.TryGetValue(anahtar, out sira)) sira = 1;

            string yol = DosyaYolu(klasor, ad, uzanti, sira);
            StreamWriter yazici;
            if (!_yazicilar.TryGetValue(yol, out yazici))
            {
                Directory.CreateDirectory(klasor);
                // Önceki çalışmadan kalan dosya sınırı geçmişse sıradakine geç
                while (File.Exists(yol) && new FileInfo(yol).Length >= _maksimumBoyut)
                {
                    sira++;
                    yol = DosyaYolu(klasor, ad, uzanti, sira);
                }
                _dosyaSiralari[anahtar] = sira;
                if (!_yazicilar.TryGetValue(yol, out yazici))
                {
                    FileStream fs = new FileStream(yol, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                    yazici = new StreamWriter(fs, new UTF8Encoding(false));
                    yazici.AutoFlush = true;
                    _yazicilar[yol] = yazici;
                }
            }

            yazici.WriteLine(satir);

            if (yazici.BaseStream.Length >= _maksimumBoyut)
            {
                yazici.Dispose();
                _yazicilar.Remove(yol);
                _dosyaSiralari[anahtar] = sira + 1;
            }
        }

        private static string DosyaYolu(string klasor, string ad, string uzanti, int sira)
        {
            return Path.Combine(klasor, sira <= 1 ? ad + uzanti : ad + "_" + sira + uzanti);
        }

        /// <summary>LogSaklamaGun'den eski gün klasörlerini ve tanılama klasörlerini siler.</summary>
        private static void EskiLoglariTemizle(int saklamaGun)
        {
            if (saklamaGun <= 0) return;
            DateTime sinir = DateTime.Today.AddDays(-saklamaGun);
            try
            {
                if (!Directory.Exists(Globals.LogKlasoru)) return;

                foreach (string klasor in Directory.GetDirectories(Globals.LogKlasoru))
                {
                    DateTime gun;
                    string ad = Path.GetFileName(klasor);
                    if (DateTime.TryParseExact(ad, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out gun) && gun < sinir)
                        Directory.Delete(klasor, true);
                }

                // Tanilama/{platform}/{yyyyMMdd_HHmmss}_{IslemID}
                string tanilama = Path.Combine(Globals.LogKlasoru, "Tanilama");
                if (Directory.Exists(tanilama))
                {
                    foreach (string platformKlasoru in Directory.GetDirectories(tanilama))
                    {
                        foreach (string kayit in Directory.GetDirectories(platformKlasoru))
                        {
                            if (Directory.GetCreationTime(kayit) < sinir) Directory.Delete(kayit, true);
                        }
                    }
                }

                // --link modunun yazdığı sonuç dosyaları
                foreach (string dosya in Directory.GetFiles(Globals.LogKlasoru, "sonuc_*.json"))
                {
                    if (File.GetCreationTime(dosya) < sinir) File.Delete(dosya);
                }
            }
            catch (Exception ex)
            {
                Uyari("LOG_TEMIZLIK", "Eski loglar temizlenemedi", null, new { hata = ex.Message });
            }
        }

        private class IslemBaglami
        {
            public string IslemID;
            public string ModulType;
            public string Link;
        }

        private class BaglamKapatici : IDisposable
        {
            private readonly IslemBaglami _onceki;

            public BaglamKapatici(IslemBaglami onceki)
            {
                _onceki = onceki;
            }

            public void Dispose()
            {
                _baglam.Value = _onceki;
            }
        }

        /// <summary>Süre ölçmek için kısayol.</summary>
        public static Stopwatch Kronometre()
        {
            return Stopwatch.StartNew();
        }
    }
}
