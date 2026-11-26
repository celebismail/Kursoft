using KursoftRekabet.model;

namespace KursoftRekabet.Ayristirma
{
    /// <summary>
    /// Her platformda adı aynı olan alanları secici-ayarlari.json'dan okuyup modele yazar,
    /// ardından türetilen alanları (indirim oranı, en düşük fiyat, buybox ...) hesaplar.
    /// Platforma özel düzeltmeler platform dosyasında (trendyolUrunBilgi.cs vb.) yapılır.
    /// </summary>
    public static class OrtakAlanlar
    {
        public static void Doldur(AlanOkuyucu oku, UrunBilgiModel model)
        {
            // ---------- Ürün ----------
            model.PlatformUrunID = oku.Metin("PlatformUrunID");
            model.ContentID = oku.Metin("ContentID");
            model.ListingID = oku.Metin("ListingID");
            model.MerchantSku = oku.Metin("MerchantSku");
            model.Barkod = oku.Metin("Barkod");
            model.GTIN = oku.Metin("GTIN");
            model.ModelKodu = oku.Metin("ModelKodu");

            model.UrunAdi = AlanOkuyucu.MetniTemizle(oku.Metin("UrunAdi"));
            model.Marka = AlanOkuyucu.MetniTemizle(oku.Metin("Marka"));
            model.KategoriID = oku.Metin("KategoriID");
            model.Aciklama = AlanOkuyucu.HtmldenDuzMetin(oku.Metin("Aciklama"));
            model.VideoUrl = oku.Metin("VideoUrl");

            model.UrunPuani = oku.Ondalik("UrunPuani");
            model.YorumSayisi = oku.Tamsayi("YorumSayisi");
            model.DegerlendirmeSayisi = oku.Tamsayi("DegerlendirmeSayisi");
            model.SoruSayisi = oku.Tamsayi("SoruSayisi");
            model.FavoriSayisi = oku.Tamsayi("FavoriSayisi");
            model.SatisAdediMetni = AlanOkuyucu.MetniTemizle(oku.Metin("SatisAdediMetni"));

            foreach (Dugum d in oku.Liste("KategoriYolu"))
            {
                string ad = AlanOkuyucu.MetniTemizle(oku.AltMetin(d, "Ad"));
                if (ad != null && !model.KategoriYolu.Contains(ad)) model.KategoriYolu.Add(ad);
            }

            foreach (Dugum d in oku.Liste("Ozellikler"))
            {
                string anahtar = AlanOkuyucu.MetniTemizle(oku.AltMetin(d, "Anahtar"));
                string deger = AlanOkuyucu.MetniTemizle(oku.AltMetin(d, "Deger"));
                if (anahtar != null || deger != null) model.Ozellikler.Add(new OzellikModel { Anahtar = anahtar, Deger = deger });
            }

            GorselleriOku(oku, model);
            VaryantlariOku(oku, model);

            // ---------- Fiyat ----------
            OkunanFiyat satis = oku.Fiyat("SatisFiyati");
            model.SatisFiyati = satis.Deger;
            model.FiyatMetni = oku.Metin("FiyatMetni") ?? satis.Metin;

            OkunanFiyat liste = oku.Fiyat("ListeFiyati");
            model.ListeFiyatiMetni = liste.Metin;
            model.ListeFiyati = liste.Deger;

            OkunanFiyat sepette = oku.Fiyat("SepetteFiyat");
            model.SepetteFiyatMetni = sepette.Metin;
            model.SepetteFiyat = sepette.Deger;

            model.IndirimOrani = oku.Ondalik("IndirimOrani");
            model.ParaBirimi = ParaBirimiNormalize(oku.Metin("ParaBirimi")) ?? satis.ParaBirimi;

            foreach (Dugum d in oku.Liste("Kampanyalar"))
            {
                string k = AlanOkuyucu.MetniTemizle(oku.AltMetin(d, "Metin"));
                if (k != null && !model.Kampanyalar.Contains(k)) model.Kampanyalar.Add(k);
            }
            model.KuponMetni = AlanOkuyucu.MetniTemizle(oku.Metin("KuponMetni"));
            model.TaksitBilgisi = AlanOkuyucu.MetniTemizle(oku.Metin("TaksitBilgisi"));

            OkunanFiyat kargo = oku.Fiyat("KargoUcreti");
            model.KargoUcretiMetni = kargo.Metin;
            model.KargoUcreti = kargo.Deger;
            model.KargoBedavaMi = oku.Mantiksal("KargoBedavaMi");
            model.TahminiTeslimat = AlanOkuyucu.MetniTemizle(oku.Metin("TahminiTeslimat"));
            model.HizliTeslimatMi = oku.Mantiksal("HizliTeslimatMi");

            model.StokVar = oku.Mantiksal("StokVar");
            model.TukendiMi = oku.Mantiksal("TukendiMi");
            model.StokAdedi = oku.Tamsayi("StokAdedi");
            model.MaksimumSatisAdedi = oku.Tamsayi("MaksimumSatisAdedi");

            // ---------- Satıcı ----------
            model.Satici = new SaticiModel();
            model.Satici.SaticiID = oku.Metin("SaticiID");
            model.Satici.SaticiAdi = AlanOkuyucu.MetniTemizle(oku.Metin("SaticiAdi"));
            model.Satici.SaticiPuaniMetni = oku.Metin("SaticiPuani");
            model.Satici.SaticiPuani = oku.Ondalik("SaticiPuani");
            model.Satici.PuanOlcegi = oku.Ayar.PuanOlcegi;
            model.Satici.ResmiMagazaMi = oku.Mantiksal("ResmiMagazaMi");
            model.Satici.SaticiSehri = AlanOkuyucu.MetniTemizle(oku.Metin("SaticiSehri"));
            model.Satici.SaticiLinki = oku.Metin("SaticiLinki");
            model.Satici.TakipciSayisi = oku.Tamsayi("TakipciSayisi");
            model.Satici.BuyboxMi = true;
            model.Satici.FiyatMetni = model.FiyatMetni;
            model.Satici.SatisFiyati = model.SatisFiyati;
            model.Satici.ListeFiyatiMetni = model.ListeFiyatiMetni;
            model.Satici.ListeFiyati = model.ListeFiyati;
            model.Satici.KargoUcretiMetni = model.KargoUcretiMetni;
            model.Satici.KargoUcreti = model.KargoUcreti;
            model.Satici.KargoBedavaMi = model.KargoBedavaMi;
            model.Satici.TahminiTeslimat = model.TahminiTeslimat;
            model.Satici.ListingID = model.ListingID;

            DigerSaticilariOku(oku, model);
            model.ToplamSaticiSayisi = oku.Tamsayi("ToplamSaticiSayisi");
        }

        private static void GorselleriOku(AlanOkuyucu oku, UrunBilgiModel model)
        {
            GorselAyari g = oku.Ayar.Gorsel ?? new GorselAyari();
            int sira = 1;
            foreach (Dugum d in oku.Liste("Gorseller"))
            {
                string url = oku.AltMetin(d, "Url");
                if (string.IsNullOrWhiteSpace(url)) continue;

                url = url.Trim();
                if (url.StartsWith("//")) url = "https:" + url;
                string yol = url;
                if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(g.OnEk))
                {
                    yol = url.TrimStart('/');
                    url = g.OnEk.TrimEnd('/') + "/" + yol;
                }
                else if (!string.IsNullOrEmpty(g.OnEk) && url.StartsWith(g.OnEk, StringComparison.OrdinalIgnoreCase))
                {
                    yol = url.Substring(g.OnEk.Length).TrimStart('/');
                }

                GorselModel gm = new GorselModel();
                gm.Url = BoyutYerlestir(url, g.YerTutucu, g.NormalBoyut);
                gm.BuyukUrl = oku.AltMetin(d, "BuyukUrl");
                gm.KucukUrl = oku.AltMetin(d, "KucukUrl");

                if (gm.BuyukUrl == null)
                {
                    if (!string.IsNullOrEmpty(g.YerTutucu) && url.Contains(g.YerTutucu)) gm.BuyukUrl = BoyutYerlestir(url, g.YerTutucu, g.BuyukBoyut);
                    else if (!string.IsNullOrEmpty(g.BuyukSablon) && yol != url) gm.BuyukUrl = g.BuyukSablon.Replace("{yol}", yol);
                }
                if (gm.KucukUrl == null)
                {
                    if (!string.IsNullOrEmpty(g.YerTutucu) && url.Contains(g.YerTutucu)) gm.KucukUrl = BoyutYerlestir(url, g.YerTutucu, g.KucukBoyut);
                    else if (!string.IsNullOrEmpty(g.KucukSablon) && yol != url) gm.KucukUrl = g.KucukSablon.Replace("{yol}", yol);
                }

                // Aynı görsel iki kaynaktan gelmiş olabilir
                bool varMi = false;
                foreach (GorselModel mevcut in model.Gorseller)
                {
                    if (mevcut.Url == gm.Url)
                    {
                        varMi = true;
                        break;
                    }
                }
                if (varMi) continue;

                gm.Sira = sira++;
                model.Gorseller.Add(gm);
            }
        }

        private static string BoyutYerlestir(string url, string yerTutucu, string boyut)
        {
            if (string.IsNullOrEmpty(yerTutucu) || string.IsNullOrEmpty(boyut) || url == null) return url;
            return url.Replace(yerTutucu, boyut);
        }

        private static void VaryantlariOku(AlanOkuyucu oku, UrunBilgiModel model)
        {
            foreach (Dugum d in oku.Liste("Varyantlar"))
            {
                VaryantModel v = new VaryantModel();
                v.Ozellik = AlanOkuyucu.MetniTemizle(oku.AltMetin(d, "Ozellik"));
                v.Deger = AlanOkuyucu.MetniTemizle(oku.AltMetin(d, "Deger"));
                v.VaryantID = oku.AltMetin(d, "VaryantID");
                v.Barkod = oku.AltMetin(d, "Barkod");
                OkunanFiyat f = oku.AltFiyat(d, "Fiyat");
                v.FiyatMetni = f.Metin;
                v.Fiyat = f.Deger;
                v.StokVar = oku.AltMantiksal(d, "StokVar");
                v.SeciliMi = oku.AltMantiksal(d, "SeciliMi");
                v.Link = oku.AltMetin(d, "Link");
                if (v.Deger == null && v.Barkod == null && v.VaryantID == null) continue;
                model.Varyantlar.Add(v);
            }
        }

        private static void DigerSaticilariOku(AlanOkuyucu oku, UrunBilgiModel model)
        {
            foreach (Dugum d in oku.Liste("DigerSaticilar"))
            {
                SaticiModel s = new SaticiModel();
                s.SaticiID = oku.AltMetin(d, "SaticiID");
                s.SaticiAdi = AlanOkuyucu.MetniTemizle(oku.AltMetin(d, "SaticiAdi"));
                s.SaticiPuaniMetni = oku.AltMetin(d, "SaticiPuani");
                s.SaticiPuani = oku.AltOndalik(d, "SaticiPuani");
                s.PuanOlcegi = oku.Ayar.PuanOlcegi;
                s.ResmiMagazaMi = oku.AltMantiksal(d, "ResmiMagazaMi");
                s.SaticiSehri = AlanOkuyucu.MetniTemizle(oku.AltMetin(d, "SaticiSehri"));
                s.SaticiLinki = oku.AltMetin(d, "SaticiLinki");
                s.ListingID = oku.AltMetin(d, "ListingID");

                OkunanFiyat f = oku.AltFiyat(d, "SatisFiyati");
                s.SatisFiyati = f.Deger;
                s.FiyatMetni = oku.AltMetin(d, "FiyatMetni") ?? f.Metin;
                OkunanFiyat lf = oku.AltFiyat(d, "ListeFiyati");
                s.ListeFiyatiMetni = lf.Metin;
                s.ListeFiyati = lf.Deger;
                OkunanFiyat k = oku.AltFiyat(d, "KargoUcreti");
                s.KargoUcretiMetni = k.Metin;
                s.KargoUcreti = k.Deger;
                s.KargoBedavaMi = oku.AltMantiksal(d, "KargoBedavaMi");
                s.TahminiTeslimat = AlanOkuyucu.MetniTemizle(oku.AltMetin(d, "TahminiTeslimat"));
                s.StokVar = oku.AltMantiksal(d, "StokVar");
                s.BuyboxMi = false;

                if (s.SaticiAdi == null && s.SaticiID == null) continue;
                model.DigerSaticilar.Add(s);
            }
        }

        /// <summary>
        /// Okunan alanlardan türetilenleri hesaplar ve tutarlılık uyarılarını ekler.
        /// Platform dosyası kendi düzeltmelerini yaptıktan SONRA çağrılır.
        /// </summary>
        public static void Tamamla(UrunBilgiModel model, PlatformAyari ayar)
        {
            if (model.ParaBirimi == null && model.SatisFiyati != null) model.ParaBirimi = ayar.VarsayilanParaBirimi;

            // İndirim oranı: platform vermediyse liste fiyatından hesapla
            if (model.IndirimOrani == null && model.ListeFiyati.HasValue && model.SatisFiyati.HasValue
                && model.ListeFiyati.Value > model.SatisFiyati.Value && model.ListeFiyati.Value > 0)
            {
                model.IndirimOrani = Math.Round((model.ListeFiyati.Value - model.SatisFiyati.Value) * 100m / model.ListeFiyati.Value, 2);
            }

            if (model.KargoBedavaMi == null && model.KargoUcreti.HasValue) model.KargoBedavaMi = model.KargoUcreti.Value == 0;
            if (model.Satici != null && model.Satici.KargoBedavaMi == null) model.Satici.KargoBedavaMi = model.KargoBedavaMi;

            // Stok ve tükendi birbirinin tersi; biri biliniyorsa diğeri de bilinir
            if (model.StokVar == null && model.TukendiMi.HasValue) model.StokVar = !model.TukendiMi.Value;
            if (model.TukendiMi == null && model.StokVar.HasValue) model.TukendiMi = !model.StokVar.Value;
            if (model.Satici != null) model.Satici.StokVar = model.StokVar;

            SeciliVaryantiBul(model);
            DigerSaticilariTemizle(model);

            if (model.ToplamSaticiSayisi == null && model.Satici != null && model.Satici.SaticiAdi != null)
                model.ToplamSaticiSayisi = 1 + model.DigerSaticilar.Count;

            // En düşük fiyat: ana satıcı + diğerleri
            decimal? enDusuk = model.SatisFiyati;
            string enDusukSatici = model.Satici != null ? model.Satici.SaticiAdi : null;
            foreach (SaticiModel s in model.DigerSaticilar)
            {
                if (s.SatisFiyati.HasValue && (enDusuk == null || s.SatisFiyati.Value < enDusuk.Value))
                {
                    enDusuk = s.SatisFiyati;
                    enDusukSatici = s.SaticiAdi;
                }
            }
            model.EnDusukFiyat = enDusuk;
            model.EnDusukFiyatliSatici = enDusuk.HasValue ? enDusukSatici : null;

            // Uyarılar: eksik ama zorunlu olmayan alanlar
            if (model.Satici != null && model.Satici.SaticiAdi != null && model.Satici.SaticiPuani == null) model.UyariEkle("satıcı puanı bulunamadı");
            if (model.Gorseller.Count == 0) model.UyariEkle("görsel bulunamadı");
            if (model.SatisFiyati.HasValue && model.ListeFiyati.HasValue && model.ListeFiyati.Value < model.SatisFiyati.Value)
                model.UyariEkle("liste fiyatı satış fiyatından düşük");
            if (model.SatisFiyati.HasValue && model.SepetteFiyat.HasValue && model.SepetteFiyat.Value > model.SatisFiyati.Value)
                model.UyariEkle("sepette fiyat satış fiyatından yüksek");
        }

        private static void SeciliVaryantiBul(UrunBilgiModel model)
        {
            if (model.Varyantlar.Count == 0) return;
            VaryantModel secili = null;
            foreach (VaryantModel v in model.Varyantlar)
            {
                if (v.SeciliMi == true)
                {
                    secili = v;
                    break;
                }
            }
            if (secili == null && model.Barkod != null)
            {
                foreach (VaryantModel v in model.Varyantlar)
                {
                    if (v.Barkod == model.Barkod)
                    {
                        secili = v;
                        break;
                    }
                }
            }
            if (secili == null && model.Varyantlar.Count == 1) secili = model.Varyantlar[0];
            model.SeciliVaryant = secili;

            if (secili != null && secili.Fiyat.HasValue && model.SatisFiyati.HasValue && secili.Fiyat.Value != model.SatisFiyati.Value)
                model.UyariEkle("varyant fiyatı ana fiyattan farklı");
        }

        /// <summary>Ana satıcıyı ve tekrar edenleri diğer satıcılardan çıkarır.</summary>
        private static void DigerSaticilariTemizle(UrunBilgiModel model)
        {
            List<SaticiModel> temiz = new List<SaticiModel>();
            HashSet<string> gorulen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (model.Satici != null) AnahtarlariEkle(model.Satici, gorulen);
            foreach (SaticiModel s in model.DigerSaticilar)
            {
                // ID ya da ad eşleşirse aynı satıcıdır (bir kaynak ID, diğeri sadece ad verebilir)
                bool tekrar = (!string.IsNullOrEmpty(s.SaticiID) && gorulen.Contains("id:" + s.SaticiID))
                    || (!string.IsNullOrEmpty(s.SaticiAdi) && gorulen.Contains("ad:" + s.SaticiAdi.Trim()));
                if (tekrar) continue;
                AnahtarlariEkle(s, gorulen);
                temiz.Add(s);
            }
            model.DigerSaticilar = temiz;
        }

        private static void AnahtarlariEkle(SaticiModel s, HashSet<string> gorulen)
        {
            if (!string.IsNullOrEmpty(s.SaticiID)) gorulen.Add("id:" + s.SaticiID);
            if (!string.IsNullOrEmpty(s.SaticiAdi)) gorulen.Add("ad:" + s.SaticiAdi.Trim());
        }

        private static string ParaBirimiNormalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            string u = s.Trim().ToUpperInvariant();
            if (u == "TL" || u == "₺" || u == "TRY") return "TRY";
            return u;
        }
    }
}
