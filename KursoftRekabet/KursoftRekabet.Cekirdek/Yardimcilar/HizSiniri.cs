namespace KursoftRekabet.Yardimcilar
{
    /// <summary>
    /// Platform başına istekler arası bekleme. ENGEL gelince bekleme katlanır (backoff),
    /// başarılı isteklerde yavaşça normale döner.
    /// </summary>
    public static class HizSiniri
    {
        private class PlatformDurumu
        {
            public SemaphoreSlim Kilit = new SemaphoreSlim(1, 1);
            public DateTime SonIstek = DateTime.MinValue;
            public int Carpan = 1;
        }

        private static readonly Dictionary<Globals.ModulType, PlatformDurumu> _durumlar = new Dictionary<Globals.ModulType, PlatformDurumu>();
        private static readonly object _kilit = new object();

        public static async Task BekleAsync(Globals.ModulType modulType, CancellationToken iptal)
        {
            PlatformDurumu d = DurumAl(modulType);
            await d.Kilit.WaitAsync(iptal);
            try
            {
                int beklemeMs = TemelBekleme(modulType) * d.Carpan;
                TimeSpan gecen = DateTime.UtcNow - d.SonIstek;
                int kalan = beklemeMs - (int)Math.Min(int.MaxValue, gecen.TotalMilliseconds);
                if (kalan > 0)
                {
                    if (d.Carpan > 1) Logislemleri.Detay("HIZ_SINIRI", "Engel sonrası artırılmış bekleme: " + kalan + " ms (x" + d.Carpan + ")");
                    await Task.Delay(kalan, iptal);
                }
                d.SonIstek = DateTime.UtcNow;
            }
            finally
            {
                d.Kilit.Release();
            }
        }

        public static void SonucBildir(Globals.ModulType modulType, bool engelMi)
        {
            PlatformDurumu d = DurumAl(modulType);
            lock (_kilit)
            {
                if (engelMi)
                {
                    d.Carpan = Math.Min(d.Carpan * 2, Math.Max(1, Globals.Ayarlar.MaksimumEngelCarpani));
                    Logislemleri.Uyari("HIZ_SINIRI", Globals.ModulAdi(modulType) + " için bekleme x" + d.Carpan + " yapıldı (engel)");
                }
                else if (d.Carpan > 1)
                {
                    d.Carpan = Math.Max(1, d.Carpan / 2);
                }
            }
        }

        public static int Carpan(Globals.ModulType modulType)
        {
            return DurumAl(modulType).Carpan;
        }

        private static int TemelBekleme(Globals.ModulType modulType)
        {
            int ms;
            if (Globals.Ayarlar.PlatformBeklemeMs != null && Globals.Ayarlar.PlatformBeklemeMs.TryGetValue(Globals.ModulAdi(modulType), out ms)) return Math.Max(0, ms);
            return Math.Max(0, Globals.Ayarlar.VarsayilanPlatformBeklemeMs);
        }

        private static PlatformDurumu DurumAl(Globals.ModulType modulType)
        {
            lock (_kilit)
            {
                PlatformDurumu d;
                if (!_durumlar.TryGetValue(modulType, out d))
                {
                    d = new PlatformDurumu();
                    _durumlar[modulType] = d;
                }
                return d;
            }
        }
    }
}
