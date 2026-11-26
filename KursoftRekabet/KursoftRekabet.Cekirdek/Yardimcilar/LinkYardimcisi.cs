using System.Text;
using System.Text.RegularExpressions;
using KursoftRekabet.Ayristirma;

namespace KursoftRekabet.Yardimcilar
{
    public class LinkSonucu
    {
        public bool Gecerli;
        public string Hata;
        public string NormalizeLink;
        public bool KisaLinkMi;
    }

    /// <summary>Link doğrulama, platform bulma ve takip parametrelerini temizleme.</summary>
    public static class LinkYardimcisi
    {
        /// <summary>Linkin alan adından platformu bulur. Bulamazsa bilinmiyor.</summary>
        public static Globals.ModulType PlatformBul(string link)
        {
            Uri uri = UriYap(link);
            if (uri == null) return Globals.ModulType.bilinmiyor;
            string host = uri.Host.ToLowerInvariant();

            foreach (PlatformAyari ayar in SeciciAyarlari.TumPlatformlar())
            {
                if (AlanEslesir(host, ayar.AlanAdlari) || AlanEslesir(host, ayar.KisaLinkAlanlari)) return ayar.ModulType;
            }
            return Globals.ModulType.bilinmiyor;
        }

        /// <summary>Linkin bu platforma ait bir ürün linki olup olmadığına bakar ve normalize eder.</summary>
        public static LinkSonucu Dogrula(string link, PlatformAyari ayar)
        {
            LinkSonucu s = new LinkSonucu();
            Uri uri = UriYap(link);
            if (uri == null)
            {
                s.Hata = "link boş ya da geçerli bir adres değil";
                return s;
            }

            string host = uri.Host.ToLowerInvariant();
            if (AlanEslesir(host, ayar.KisaLinkAlanlari))
            {
                // Kısa link: yönlendirme tarayıcıda takip edilir, doğrulama sonra tekrar yapılır
                s.Gecerli = true;
                s.KisaLinkMi = true;
                s.NormalizeLink = uri.GetLeftPart(UriPartial.Path);
                return s;
            }

            if (!AlanEslesir(host, ayar.AlanAdlari))
            {
                s.Hata = "alan adı " + ayar.Ad + " değil: " + host;
                return s;
            }

            if (!string.IsNullOrEmpty(ayar.UrunLinkDeseni) && !Regex.IsMatch(uri.AbsolutePath, ayar.UrunLinkDeseni, RegexOptions.IgnoreCase))
            {
                s.Hata = "ürün sayfası linki değil (beklenen desen: " + ayar.UrunLinkDeseni + ")";
                return s;
            }

            s.Gecerli = true;
            s.NormalizeLink = Normalize(uri, ayar.KorunacakParametreler);
            return s;
        }

        /// <summary>https, küçük harf host, fragment yok, sadece korunacak parametreler kalır.</summary>
        public static string Normalize(Uri uri, List<string> korunacaklar)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("https://").Append(uri.Host.ToLowerInvariant()).Append(uri.AbsolutePath);

            string sorgu = uri.Query;
            if (!string.IsNullOrEmpty(sorgu) && korunacaklar != null && korunacaklar.Count > 0)
            {
                List<string> kalan = new List<string>();
                foreach (string parca in sorgu.TrimStart('?').Split('&'))
                {
                    if (parca.Length == 0) continue;
                    int esit = parca.IndexOf('=');
                    string ad = esit >= 0 ? parca.Substring(0, esit) : parca;
                    foreach (string k in korunacaklar)
                    {
                        if (string.Equals(ad, k, StringComparison.OrdinalIgnoreCase))
                        {
                            kalan.Add(parca);
                            break;
                        }
                    }
                }
                if (kalan.Count > 0) sb.Append("?").Append(string.Join("&", kalan));
            }
            return sb.ToString();
        }

        public static Uri UriYap(string link)
        {
            if (string.IsNullOrWhiteSpace(link)) return null;
            string l = link.Trim();
            if (!l.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !l.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                l = "https://" + l;
            Uri uri;
            if (!Uri.TryCreate(l, UriKind.Absolute, out uri)) return null;
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return null;
            if (string.IsNullOrEmpty(uri.Host) || uri.Host.IndexOf('.') < 0) return null;
            return uri;
        }

        /// <summary>host == alan ya da host alanın alt alan adı (m.trendyol.com).</summary>
        public static bool AlanEslesir(string host, List<string> alanlar)
        {
            if (alanlar == null) return false;
            foreach (string a in alanlar)
            {
                string alan = a.ToLowerInvariant().Trim();
                if (host == alan || host.EndsWith("." + alan)) return true;
            }
            return false;
        }
    }
}
