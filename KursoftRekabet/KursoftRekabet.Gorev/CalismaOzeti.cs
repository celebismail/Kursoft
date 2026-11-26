using KursoftRekabet.model;

namespace KursoftRekabet.Gorev
{
    /// <summary>Exe kapanırken yazılan özet: toplamlar, hata dağılımı, platform ortalamaları, en yavaş linkler.</summary>
    public class CalismaOzeti
    {
        private readonly object _kilit = new object();
        private readonly List<UrunBilgiModel> _sonuclar = new List<UrunBilgiModel>();

        public DateTime Baslangic = DateTime.Now;
        public int PlanlananLink;
        public bool IptalEdildi;

        public void Ekle(UrunBilgiModel m)
        {
            lock (_kilit) _sonuclar.Add(m);
        }

        public int ToplamLink
        {
            get
            {
                lock (_kilit) return _sonuclar.Count;
            }
        }

        public int Basarili
        {
            get
            {
                int s = 0;
                lock (_kilit)
                {
                    foreach (UrunBilgiModel m in _sonuclar)
                    {
                        if (m.Basarili) s++;
                    }
                }
                return s;
            }
        }

        public int Basarisiz
        {
            get { return ToplamLink - Basarili; }
        }

        public long ToplamSureMs
        {
            get { return (long)(DateTime.Now - Baslangic).TotalMilliseconds; }
        }

        public Dictionary<string, int> HataDagilimi
        {
            get
            {
                Dictionary<string, int> d = new Dictionary<string, int>();
                lock (_kilit)
                {
                    foreach (UrunBilgiModel m in _sonuclar)
                    {
                        if (m.Basarili) continue;
                        string k = m.HataKodu ?? Globals.HataKodlari.BILINMEYEN;
                        int v;
                        d.TryGetValue(k, out v);
                        d[k] = v + 1;
                    }
                }
                return d;
            }
        }

        public List<object> PlatformOzeti()
        {
            Dictionary<string, long[]> top = new Dictionary<string, long[]>();   // [adet, başarılı, toplamMs]
            lock (_kilit)
            {
                foreach (UrunBilgiModel m in _sonuclar)
                {
                    string p = Globals.ModulAdi(m.ModulType);
                    long[] v;
                    if (!top.TryGetValue(p, out v))
                    {
                        v = new long[3];
                        top[p] = v;
                    }
                    v[0]++;
                    if (m.Basarili) v[1]++;
                    v[2] += m.SureMs;
                }
            }
            List<object> liste = new List<object>();
            foreach (KeyValuePair<string, long[]> kv in top)
            {
                liste.Add(new { Platform = kv.Key, Adet = kv.Value[0], Basarili = kv.Value[1], OrtalamaSureMs = kv.Value[0] > 0 ? kv.Value[2] / kv.Value[0] : 0 });
            }
            return liste;
        }

        public List<object> EnYavaslar(int adet)
        {
            List<UrunBilgiModel> kopya;
            lock (_kilit) kopya = new List<UrunBilgiModel>(_sonuclar);
            kopya.Sort(delegate (UrunBilgiModel a, UrunBilgiModel b) { return b.SureMs.CompareTo(a.SureMs); });
            List<object> liste = new List<object>();
            for (int i = 0; i < kopya.Count && i < adet; i++)
            {
                liste.Add(new { kopya[i].Link, kopya[i].SureMs, kopya[i].Basarili, kopya[i].HataKodu });
            }
            return liste;
        }

        /// <summary>0 hepsi başarılı (ya da link yok), 1 bazıları başarısız, 2 hepsi başarısız.</summary>
        public int ExitCode()
        {
            int toplam = ToplamLink;
            int basarili = Basarili;
            if (toplam == 0 || basarili == toplam) return 0;
            if (basarili == 0) return 2;
            return 1;
        }

        public void LogYaz(int exitCode)
        {
            Logislemleri.Bilgi("CALISMA_OZETI",
                "Planlanan: " + PlanlananLink + ", işlenen: " + ToplamLink + ", başarılı: " + Basarili + ", başarısız: " + Basarisiz
                + (IptalEdildi ? " (İPTAL EDİLDİ)" : "") + ", exit code: " + exitCode,
                ToplamSureMs,
                new
                {
                    PlanlananLink,
                    ToplamLink,
                    Basarili,
                    Basarisiz,
                    HataDagilimi,
                    Platformlar = PlatformOzeti(),
                    EnYavas5 = EnYavaslar(5),
                    IptalEdildi,
                    ExitCode = exitCode
                });
        }
    }
}
