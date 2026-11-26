using System.Globalization;
using System.Text;

namespace KursoftRekabet.Yardimcilar
{
    public class FiyatSonucu
    {
        public decimal? Deger { get; set; }

        /// <summary>"100 - 200 TL" gibi aralık fiyatlarda üst değer.</summary>
        public decimal? UstDeger { get; set; }
        public bool AralikMi { get; set; }
        public string ParaBirimi { get; set; }

        /// <summary>Çevrilemediyse veya şüpheli durumda açıklama.</summary>
        public string Uyari { get; set; }
    }

    /// <summary>
    /// tr-TR fiyat metinlerini decimal'e çevirir.
    /// "1.950,99 TL" -> 1950.99 | "₺1.950" -> 1950 | "1950,99" -> 1950.99 | "Sepette 899,90 TL" -> 899.90
    /// Çeviremezse Deger = null ve Uyari dolu döner (exception yok).
    /// </summary>
    public static class FiyatCevir
    {
        private static readonly CultureInfo _inv = CultureInfo.InvariantCulture;

        public static decimal? Cevir(string metin)
        {
            return Coz(metin).Deger;
        }

        public static FiyatSonucu Coz(string metin)
        {
            FiyatSonucu sonuc = new FiyatSonucu();
            if (string.IsNullOrWhiteSpace(metin))
            {
                sonuc.Uyari = "fiyat metni boş";
                return sonuc;
            }

            string m = metin.Trim();
            sonuc.ParaBirimi = ParaBirimiBul(m);

            // Sayı gruplarını çıkar: rakam, nokta, virgül ve aralarındaki boşluk ("1 950,99" de olabilir)
            List<string> sayilar = SayiParcalariniAyir(m);
            if (sayilar.Count == 0)
            {
                sonuc.Uyari = "fiyat metninde sayı yok: '" + metin + "'";
                return sonuc;
            }

            decimal? ilk = SayiCevir(sayilar[0]);
            if (ilk == null)
            {
                sonuc.Uyari = "fiyat çevrilemedi: '" + metin + "'";
                return sonuc;
            }
            sonuc.Deger = ilk;

            // "100 - 200 TL", "100 TL - 200 TL": aralık. En düşük değer Deger'e yazılır.
            if (sayilar.Count >= 2 && AralikIsaretiVarMi(m))
            {
                decimal? ikinci = SayiCevir(sayilar[1]);
                if (ikinci != null)
                {
                    sonuc.AralikMi = true;
                    sonuc.Deger = Math.Min(ilk.Value, ikinci.Value);
                    sonuc.UstDeger = Math.Max(ilk.Value, ikinci.Value);
                    sonuc.Uyari = "aralık fiyat: '" + metin + "', en düşük değer alındı";
                }
            }
            else if (sayilar.Count >= 2)
            {
                // "3 x 333,33 TL" gibi taksit metinleri: son sayıyı fiyat kabul etmek daha doğru olur ama emin değiliz
                sonuc.Uyari = "fiyat metninde birden çok sayı var: '" + metin + "', ilki alındı";
            }

            return sonuc;
        }

        /// <summary>JSON'dan gelen değer sayı (1950.99) ya da metin ("1.950,99 TL") olabilir.</summary>
        public static decimal? JsonDegeriCevir(object deger)
        {
            if (deger == null) return null;
            if (deger is decimal) return (decimal)deger;
            if (deger is double) return Convert.ToDecimal((double)deger);
            if (deger is float) return Convert.ToDecimal((float)deger);
            if (deger is long) return (long)deger;
            if (deger is int) return (int)deger;
            return Cevir(Convert.ToString(deger, _inv));
        }

        public static string ParaBirimiBul(string metin)
        {
            if (string.IsNullOrEmpty(metin)) return null;
            string u = metin.ToUpperInvariant();
            if (metin.Contains("₺") || u.Contains("TL") || u.Contains("TRY")) return "TRY";
            if (metin.Contains("$") || u.Contains("USD")) return "USD";
            if (metin.Contains("€") || u.Contains("EUR")) return "EUR";
            return null;
        }

        private static bool AralikIsaretiVarMi(string m)
        {
            // Eksi işareti negatif sayı değil, aralık anlamında ("100 - 200")
            return m.Contains(" - ") || m.Contains("–") || m.Contains("—") || m.Contains("-") && !m.TrimStart().StartsWith("-");
        }

        private static List<string> SayiParcalariniAyir(string m)
        {
            List<string> liste = new List<string>();
            StringBuilder parca = new StringBuilder();
            for (int i = 0; i < m.Length; i++)
            {
                char c = m[i];
                bool sayiKarakteri = char.IsDigit(c) || c == '.' || c == ',';

                // "1 950,99": rakamlar arasındaki tek boşluk binlik ayırıcı olabilir
                bool binlikBosluk = (c == ' ' || c == ' ' || c == ' ') && parca.Length > 0
                    && char.IsDigit(parca[parca.Length - 1]) && UcRakamGeliyorMu(m, i + 1);

                if (sayiKarakteri)
                {
                    parca.Append(c);
                }
                else if (binlikBosluk && parca.ToString().Replace(".", "").Replace(",", "").Length <= 3)
                {
                    // boşluğu yut
                }
                else
                {
                    ParcaEkle(liste, parca);
                }
            }
            ParcaEkle(liste, parca);
            return liste;
        }

        private static bool UcRakamGeliyorMu(string m, int bas)
        {
            if (bas + 3 > m.Length) return false;
            for (int k = bas; k < bas + 3; k++)
            {
                if (!char.IsDigit(m[k])) return false;
            }
            return bas + 3 == m.Length || !char.IsDigit(m[bas + 3]);
        }

        private static void ParcaEkle(List<string> liste, StringBuilder parca)
        {
            if (parca.Length == 0) return;
            string p = parca.ToString().Trim('.', ',');
            parca.Clear();
            bool rakamVar = false;
            foreach (char c in p)
            {
                if (char.IsDigit(c))
                {
                    rakamVar = true;
                    break;
                }
            }
            if (rakamVar) liste.Add(p);
        }

        /// <summary>
        /// Ayırıcıların anlamını çözer:
        ///  - Hem nokta hem virgül varsa: sonda olan ondalık ayırıcıdır ("1.950,99" / "1,950.99").
        ///  - Sadece virgül: ondalık ("1950,99"). Birden çok virgül varsa binlik ("1,950,000").
        ///  - Sadece nokta: arkasında tam 3 rakam olan gruplar binliktir ("1.950", "1.250.000"),
        ///    değilse ondalık ("19.99").
        /// </summary>
        private static decimal? SayiCevir(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;

            int noktaSay = 0, virgulSay = 0;
            foreach (char c in s)
            {
                if (c == '.') noktaSay++;
                else if (c == ',') virgulSay++;
            }

            string normal;
            if (noktaSay > 0 && virgulSay > 0)
            {
                int sonNokta = s.LastIndexOf('.');
                int sonVirgul = s.LastIndexOf(',');
                if (sonVirgul > sonNokta) normal = s.Replace(".", "").Replace(",", ".");
                else normal = s.Replace(",", "");
            }
            else if (virgulSay > 0)
            {
                if (virgulSay == 1) normal = s.Replace(",", ".");
                else normal = s.Replace(",", "");
            }
            else if (noktaSay > 0)
            {
                if (BinlikGruplariMi(s, '.')) normal = s.Replace(".", "");
                else if (noktaSay == 1) normal = s;
                else return null;
            }
            else
            {
                normal = s;
            }

            decimal d;
            if (decimal.TryParse(normal, NumberStyles.AllowDecimalPoint, _inv, out d)) return d;
            return null;
        }

        private static bool BinlikGruplariMi(string s, char ayirici)
        {
            string[] gruplar = s.Split(ayirici);
            if (gruplar.Length < 2) return false;
            if (gruplar[0].Length == 0 || gruplar[0].Length > 3) return false;
            for (int i = 1; i < gruplar.Length; i++)
            {
                if (gruplar[i].Length != 3) return false;
            }
            return true;
        }
    }
}
