using System.Text;
using Newtonsoft.Json.Linq;

namespace KursoftRekabet.Yardimcilar
{
    /// <summary>
    /// secici-ayarlari.json'daki JSON yollarını çözümler. Desteklenen yazım:
    ///   a.b.c                 düz yol
    ///   a.b[0].c   a[-1]      dizi elemanı (-1 = son eleman)
    ///   a.b[*].c              dizinin tüm elemanları (liste döner)
    ///   a.b[?selected=true]   filtre: alt yolu verilen değere eşit olan elemanlar (a.b[?key.name=Renk])
    ///   ..merchantListing     derin arama: her seviyede bu ada sahip özellikler
    ///   a['data-x'].b         nokta içeren/özel adlar
    /// Yol bulunamazsa boş liste döner, exception yok.
    /// </summary>
    public static class JsonYolu
    {
        private enum AdimTuru { Ozellik, Index, Hepsi, Filtre, Derin }

        private class Adim
        {
            public AdimTuru Tur;
            public string Ad;
            public int Index;
            public string FiltreYolu;
            public string FiltreDegeri;
        }

        private static readonly Dictionary<string, List<Adim>> _onbellek = new Dictionary<string, List<Adim>>();
        private static readonly object _kilit = new object();

        /// <summary>Yolun gösterdiği tüm değerler (null olanlar hariç).</summary>
        public static List<JToken> Hepsi(JToken kok, string yol)
        {
            List<JToken> mevcut = new List<JToken>();
            if (kok == null || kok.Type == JTokenType.Null) return mevcut;
            mevcut.Add(kok);
            if (string.IsNullOrWhiteSpace(yol) || yol.Trim() == "$") return mevcut;

            List<Adim> adimlar = Ayristir(yol);
            if (adimlar == null) return new List<JToken>();

            foreach (Adim adim in adimlar)
            {
                List<JToken> sonraki = new List<JToken>();
                foreach (JToken t in mevcut)
                {
                    AdimUygula(t, adim, sonraki);
                }
                mevcut = sonraki;
                if (mevcut.Count == 0) break;
            }
            return mevcut;
        }

        /// <summary>Yolun gösterdiği ilk değer, yoksa null.</summary>
        public static JToken Ilk(JToken kok, string yol)
        {
            List<JToken> liste = Hepsi(kok, yol);
            if (liste.Count > 0) return liste[0];
            return null;
        }

        /// <summary>Değeri metin olarak verir. Boş metin/null -> null.</summary>
        public static string Metin(JToken kok, string yol)
        {
            return MetneCevir(Ilk(kok, yol));
        }

        public static string MetneCevir(JToken t)
        {
            if (t == null || t.Type == JTokenType.Null || t.Type == JTokenType.Undefined) return null;
            if (t.Type == JTokenType.Object || t.Type == JTokenType.Array) return null;
            string s;
            if (t.Type == JTokenType.Float) s = ((JValue)t).ToString(System.Globalization.CultureInfo.InvariantCulture);
            else s = t.ToString();
            s = s.Trim();
            if (s.Length == 0) return null;
            return s;
        }

        /// <summary>
        /// Ağacın içinde verilen anahtarların HEPSİNE sahip ilk nesneyi arar (genişlik öncelikli, en sığdaki kazanır).
        /// Ör. ("merchantListing", "name") -> Trendyol ürün nesnesi.
        /// </summary>
        public static JObject AnahtarlariIcerenNesne(JToken kok, List<string> anahtarlar, int maksimumDerinlik)
        {
            if (kok == null || anahtarlar == null || anahtarlar.Count == 0) return null;
            Queue<KeyValuePair<JToken, int>> kuyruk = new Queue<KeyValuePair<JToken, int>>();
            kuyruk.Enqueue(new KeyValuePair<JToken, int>(kok, 0));
            while (kuyruk.Count > 0)
            {
                KeyValuePair<JToken, int> kv = kuyruk.Dequeue();
                JToken t = kv.Key;
                if (t is JObject)
                {
                    JObject o = (JObject)t;
                    bool hepsiVar = true;
                    foreach (string a in anahtarlar)
                    {
                        JToken deger = o[a];
                        if (deger == null || deger.Type == JTokenType.Null)
                        {
                            hepsiVar = false;
                            break;
                        }
                    }
                    if (hepsiVar) return o;
                }
                if (kv.Value >= maksimumDerinlik) continue;
                if (t is JContainer)
                {
                    foreach (JToken cocuk in ((JContainer)t).Children())
                    {
                        JToken deger = cocuk is JProperty ? ((JProperty)cocuk).Value : cocuk;
                        if (deger is JContainer) kuyruk.Enqueue(new KeyValuePair<JToken, int>(deger, kv.Value + 1));
                    }
                }
            }
            return null;
        }

        /// <summary>Metni JSON'a çevirir; çeviremezse null (exception yok).</summary>
        public static JToken GuvenliParse(string metin)
        {
            if (string.IsNullOrWhiteSpace(metin)) return null;
            try
            {
                return JToken.Parse(metin);
            }
            catch (Exception ex)
            {
                Logislemleri.Detay("JSON_PARSE", "JSON çevrilemedi: " + ex.Message, null, new { uzunluk = metin.Length });
                return null;
            }
        }

        // ---------- İç taraf ----------

        private static void AdimUygula(JToken t, Adim adim, List<JToken> sonuc)
        {
            switch (adim.Tur)
            {
                case AdimTuru.Ozellik:
                    if (t is JObject)
                    {
                        JToken d = ((JObject)t)[adim.Ad];
                        if (d != null && d.Type != JTokenType.Null) sonuc.Add(d);
                    }
                    break;

                case AdimTuru.Index:
                    if (t is JArray)
                    {
                        JArray dizi = (JArray)t;
                        int i = adim.Index < 0 ? dizi.Count + adim.Index : adim.Index;
                        if (i >= 0 && i < dizi.Count && dizi[i].Type != JTokenType.Null) sonuc.Add(dizi[i]);
                    }
                    break;

                case AdimTuru.Hepsi:
                    if (t is JArray)
                    {
                        foreach (JToken e in (JArray)t)
                        {
                            if (e.Type != JTokenType.Null) sonuc.Add(e);
                        }
                    }
                    else if (t is JObject)
                    {
                        foreach (JProperty p in ((JObject)t).Properties())
                        {
                            if (p.Value.Type != JTokenType.Null) sonuc.Add(p.Value);
                        }
                    }
                    break;

                case AdimTuru.Filtre:
                    if (t is JArray)
                    {
                        foreach (JToken e in (JArray)t)
                        {
                            string deger = Metin(e, adim.FiltreYolu);
                            if (deger != null && string.Equals(deger, adim.FiltreDegeri, StringComparison.OrdinalIgnoreCase)) sonuc.Add(e);
                        }
                    }
                    break;

                case AdimTuru.Derin:
                    DerinAra(t, adim.Ad, sonuc, 0);
                    break;
            }
        }

        private static void DerinAra(JToken t, string ad, List<JToken> sonuc, int derinlik)
        {
            if (derinlik > 40) return;
            if (t is JObject)
            {
                foreach (JProperty p in ((JObject)t).Properties())
                {
                    if (p.Name == ad && p.Value.Type != JTokenType.Null) sonuc.Add(p.Value);
                    DerinAra(p.Value, ad, sonuc, derinlik + 1);
                }
            }
            else if (t is JArray)
            {
                foreach (JToken e in (JArray)t)
                {
                    DerinAra(e, ad, sonuc, derinlik + 1);
                }
            }
        }

        private static List<Adim> Ayristir(string yol)
        {
            lock (_kilit)
            {
                List<Adim> hazir;
                if (_onbellek.TryGetValue(yol, out hazir)) return hazir;
            }

            List<Adim> adimlar = new List<Adim>();
            string y = yol.Trim();
            if (y.StartsWith("$")) y = y.Substring(1);
            int i = 0;
            try
            {
                while (i < y.Length)
                {
                    char c = y[i];
                    if (c == '.' && i + 1 < y.Length && y[i + 1] == '.')
                    {
                        i += 2;
                        string ad = AdOku(y, ref i);
                        adimlar.Add(new Adim { Tur = AdimTuru.Derin, Ad = ad });
                    }
                    else if (c == '.')
                    {
                        i++;
                    }
                    else if (c == '[')
                    {
                        int kapanis = KapanisBul(y, i);
                        string ic = y.Substring(i + 1, kapanis - i - 1).Trim();
                        i = kapanis + 1;
                        adimlar.Add(KoseliAdim(ic));
                    }
                    else
                    {
                        string ad = AdOku(y, ref i);
                        adimlar.Add(new Adim { Tur = AdimTuru.Ozellik, Ad = ad });
                    }
                }
            }
            catch (Exception ex)
            {
                Logislemleri.Uyari("JSON_YOLU", "Geçersiz JSON yolu: '" + yol + "' (" + ex.Message + ")");
                adimlar = null;
            }

            lock (_kilit)
            {
                _onbellek[yol] = adimlar;
            }
            return adimlar;
        }

        private static string AdOku(string y, ref int i)
        {
            StringBuilder sb = new StringBuilder();
            while (i < y.Length && y[i] != '.' && y[i] != '[')
            {
                sb.Append(y[i]);
                i++;
            }
            if (sb.Length == 0) throw new FormatException("boş ad, konum " + i);
            return sb.ToString();
        }

        private static int KapanisBul(string y, int acilis)
        {
            // Tırnak içindeki ']' karakterini kapanış saymayalım
            bool tirnakta = false;
            char tirnak = '\0';
            for (int k = acilis + 1; k < y.Length; k++)
            {
                char c = y[k];
                if (tirnakta)
                {
                    if (c == tirnak) tirnakta = false;
                }
                else if (c == '\'' || c == '"')
                {
                    tirnakta = true;
                    tirnak = c;
                }
                else if (c == ']')
                {
                    return k;
                }
            }
            throw new FormatException("kapanmayan '['");
        }

        private static Adim KoseliAdim(string ic)
        {
            if (ic == "*") return new Adim { Tur = AdimTuru.Hepsi };

            if (ic.StartsWith("?"))
            {
                string f = ic.Substring(1).Trim();
                if (f.StartsWith("(") && f.EndsWith(")")) f = f.Substring(1, f.Length - 2);
                int esit = f.IndexOf('=');
                if (esit <= 0) throw new FormatException("filtrede '=' yok");
                string sol = f.Substring(0, esit).Trim();
                if (sol.EndsWith("=")) sol = sol.Substring(0, sol.Length - 1).Trim();
                string sag = f.Substring(esit + 1).Trim().TrimStart('=').Trim().Trim('\'', '"');
                if (sol.StartsWith("@.")) sol = sol.Substring(2);
                return new Adim { Tur = AdimTuru.Filtre, FiltreYolu = sol, FiltreDegeri = sag };
            }

            if ((ic.StartsWith("'") && ic.EndsWith("'")) || (ic.StartsWith("\"") && ic.EndsWith("\"")))
                return new Adim { Tur = AdimTuru.Ozellik, Ad = ic.Substring(1, ic.Length - 2) };

            int index;
            if (int.TryParse(ic, out index)) return new Adim { Tur = AdimTuru.Index, Index = index };

            throw new FormatException("anlaşılmayan köşeli ifade: [" + ic + "]");
        }
    }
}
