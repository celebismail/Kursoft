using KursoftRekabet.model;
using Microsoft.Playwright;

namespace KursoftRekabet.Tarayici
{
    /// <summary>
    /// Sayfanın yaptığı XHR/fetch isteklerinden JSON cevapları yakalar.
    /// Ürün anahtar kelimesi (ör. "merchantListing") geçenlerin gövdesi saklanır, diğerlerinin sadece özeti.
    /// Sayfaya gitmeden ÖNCE Baslat() çağrılmalı.
    /// </summary>
    public class AgYakalayici
    {
        private const int MaksimumGovde = 8 * 1024 * 1024;
        private const int MaksimumOzet = 400;

        private readonly object _kilit = new object();
        private readonly List<Task> _isler = new List<Task>();
        private readonly List<AgCevabi> _cevaplar = new List<AgCevabi>();
        private readonly List<string> _kelimeler;
        private bool _urunVerisiGeldi = false;
        private IPage _sayfa;

        public AgYakalayici(List<string> urunAnahtarKelimeleri)
        {
            _kelimeler = urunAnahtarKelimeleri ?? new List<string>();
        }

        public void Baslat(IPage sayfa)
        {
            _sayfa = sayfa;
            sayfa.Response += YanitGeldi;
        }

        public void Durdur()
        {
            if (_sayfa != null) _sayfa.Response -= YanitGeldi;
        }

        public bool UrunVerisiGeldiMi
        {
            get
            {
                lock (_kilit) return _urunVerisiGeldi;
            }
        }

        private void YanitGeldi(object gonderen, IResponse yanit)
        {
            Task t = YanitiIsleAsync(yanit);
            lock (_kilit) _isler.Add(t);
        }

        private async Task YanitiIsleAsync(IResponse yanit)
        {
            try
            {
                string tur = null;
                if (yanit.Headers != null) yanit.Headers.TryGetValue("content-type", out tur);
                if (tur == null || tur.IndexOf("json", StringComparison.OrdinalIgnoreCase) < 0) return;

                string metin = await yanit.TextAsync();
                if (string.IsNullOrWhiteSpace(metin)) return;

                bool urunVar = false;
                foreach (string k in _kelimeler)
                {
                    if (metin.IndexOf(k, StringComparison.Ordinal) >= 0)
                    {
                        urunVar = true;
                        break;
                    }
                }

                AgCevabi c = new AgCevabi();
                c.Url = yanit.Url;
                c.Durum = yanit.Status;
                c.Uzunluk = metin.Length;
                c.UrunVar = urunVar;
                if (urunVar && metin.Length <= MaksimumGovde) c.Govde = metin;

                lock (_kilit)
                {
                    // Özet listesi sınırsız büyümesin; ürün verisi taşıyanlar her zaman eklenir
                    if (urunVar || _cevaplar.Count < MaksimumOzet) _cevaplar.Add(c);
                    if (urunVar) _urunVerisiGeldi = true;
                }
                if (urunVar) Logislemleri.Detay("AG_JSON_YAKALANDI", c.Url + " (" + c.Uzunluk + " karakter)");
            }
            catch (Exception ex)
            {
                // Yönlendirme cevabı, sayfa kapanırken gelen cevap vb. gövdesi okunamaz: önemsiz
                Logislemleri.Detay("AG_JSON", "Cevap okunamadı: " + yanit.Url + " (" + ex.Message + ")");
            }
        }

        /// <summary>Devam eden cevap okumalarını en fazla verilen süre kadar bekler.</summary>
        public async Task BekleAsync(int maksimumMs)
        {
            Task[] isler;
            lock (_kilit) isler = _isler.ToArray();
            if (isler.Length == 0) return;
            await Task.WhenAny(Task.WhenAll(isler), Task.Delay(maksimumMs));
        }

        public List<AgCevabi> Cevaplar()
        {
            lock (_kilit) return new List<AgCevabi>(_cevaplar);
        }
    }
}
