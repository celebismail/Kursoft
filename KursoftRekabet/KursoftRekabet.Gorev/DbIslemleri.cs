using System.Data;
using System.Text.RegularExpressions;
using KursoftRekabet.model;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace KursoftRekabet.Gorev
{
    public class LinkKaydi
    {
        public string ID;
        public string Link;

        /// <summary>DB'den geldiyse öncelikli. Gelmediyse link'ten bulunur.</summary>
        public Globals.ModulType ModulType = Globals.ModulType.bilinmiyor;
    }

    /// <summary>
    /// Düz ADO.NET. Tüm SQL'ler parametreli. SCOPE_IDENTITY kullanılmaz (tablolarda trigger var).
    /// Log tablolarına yazılamazsa uygulama durmaz, sadece dosyaya log düşer.
    /// </summary>
    public class DbIslemleri
    {
        private readonly GorevAyarlari _ayar;

        // Tablo adı parametre olamadığı için sadece güvenli karakterlere izin veriyoruz
        private static readonly Regex _tabloAdiDeseni = new Regex(@"^[A-Za-z0-9_]+(\.[A-Za-z0-9_]+){0,2}$");

        public DbIslemleri(GorevAyarlari ayar)
        {
            _ayar = ayar;
        }

        /// <summary>İşlenecek linkleri okur. Hata olursa exception fırlatır (Program exit code 4 döner).</summary>
        public List<LinkKaydi> LinkleriOku()
        {
            List<LinkKaydi> liste = new List<LinkKaydi>();
            using (SqlConnection baglanti = new SqlConnection(_ayar.BaglantiCumlesi))
            using (SqlCommand komut = new SqlCommand(_ayar.LinkSorgusu, baglanti))
            {
                // Boşluk içermiyorsa SP adıdır
                komut.CommandType = _ayar.LinkSorgusu.Trim().IndexOf(' ') < 0 ? CommandType.StoredProcedure : CommandType.Text;
                komut.CommandTimeout = _ayar.DbKomutZamanAsimiSn;
                baglanti.Open();
                using (SqlDataReader okuyucu = komut.ExecuteReader())
                {
                    int idSira = KolonBul(okuyucu, "ID");
                    int linkSira = KolonBul(okuyucu, "Link");
                    int modulSira = KolonBul(okuyucu, "ModulType");
                    if (idSira < 0 || linkSira < 0) throw new InvalidOperationException("LinkSorgusu en az 'ID' ve 'Link' kolonlarını döndürmeli");

                    while (okuyucu.Read())
                    {
                        LinkKaydi k = new LinkKaydi();
                        k.ID = okuyucu.IsDBNull(idSira) ? null : Convert.ToString(okuyucu.GetValue(idSira));
                        k.Link = okuyucu.IsDBNull(linkSira) ? null : Convert.ToString(okuyucu.GetValue(linkSira));
                        if (modulSira >= 0 && !okuyucu.IsDBNull(modulSira)) k.ModulType = Globals.ModulTypeCevir(Convert.ToString(okuyucu.GetValue(modulSira)));
                        liste.Add(k);
                    }
                }
            }
            return liste;
        }

        /// <summary>Varsayılan sonuç yazımı: SonucTabloAdi tablosuna temel alanlar + modelin JSON'u.</summary>
        public void SonucYaz(string kayitID, UrunBilgiModel model)
        {
            string tablo = TabloAdi(_ayar.SonucTabloAdi);
            string sql = "INSERT INTO " + tablo + " (KayitID, IslemID, CalismaID, ModulType, Link, NormalizeLink, Basarili, HataKodu, HataMesaji, "
                + "PlatformUrunID, UrunAdi, Marka, FiyatMetni, SatisFiyati, ListeFiyati, SepetteFiyat, ParaBirimi, StokVar, "
                + "SaticiAdi, SaticiPuani, DigerSaticiSayisi, EnDusukFiyat, EnDusukFiyatliSatici, UrunPuani, YorumSayisi, "
                + "VeriKaynagi, Uyarilar, SureMs, IstekZamani, SonucJson) VALUES "
                + "(@KayitID, @IslemID, @CalismaID, @ModulType, @Link, @NormalizeLink, @Basarili, @HataKodu, @HataMesaji, "
                + "@PlatformUrunID, @UrunAdi, @Marka, @FiyatMetni, @SatisFiyati, @ListeFiyati, @SepetteFiyat, @ParaBirimi, @StokVar, "
                + "@SaticiAdi, @SaticiPuani, @DigerSaticiSayisi, @EnDusukFiyat, @EnDusukFiyatliSatici, @UrunPuani, @YorumSayisi, "
                + "@VeriKaynagi, @Uyarilar, @SureMs, @IstekZamani, @SonucJson)";

            using (SqlConnection baglanti = new SqlConnection(_ayar.BaglantiCumlesi))
            using (SqlCommand komut = new SqlCommand(sql, baglanti))
            {
                komut.CommandTimeout = _ayar.DbKomutZamanAsimiSn;
                komut.Parameters.AddWithValue("@KayitID", Db(kayitID));
                komut.Parameters.AddWithValue("@IslemID", model.IslemID);
                komut.Parameters.AddWithValue("@CalismaID", Db(model.CalismaID));
                komut.Parameters.AddWithValue("@ModulType", Globals.ModulAdi(model.ModulType));
                komut.Parameters.AddWithValue("@Link", Db(Kes(model.Link, 2000)));
                komut.Parameters.AddWithValue("@NormalizeLink", Db(Kes(model.NormalizeLink, 2000)));
                komut.Parameters.AddWithValue("@Basarili", model.Basarili);
                komut.Parameters.AddWithValue("@HataKodu", Db(model.HataKodu));
                komut.Parameters.AddWithValue("@HataMesaji", Db(Kes(model.HataMesaji, 4000)));
                komut.Parameters.AddWithValue("@PlatformUrunID", Db(Kes(model.PlatformUrunID, 100)));
                komut.Parameters.AddWithValue("@UrunAdi", Db(Kes(model.UrunAdi, 1000)));
                komut.Parameters.AddWithValue("@Marka", Db(Kes(model.Marka, 250)));
                komut.Parameters.AddWithValue("@FiyatMetni", Db(Kes(model.FiyatMetni, 100)));
                komut.Parameters.AddWithValue("@SatisFiyati", Db(model.SatisFiyati));
                komut.Parameters.AddWithValue("@ListeFiyati", Db(model.ListeFiyati));
                komut.Parameters.AddWithValue("@SepetteFiyat", Db(model.SepetteFiyat));
                komut.Parameters.AddWithValue("@ParaBirimi", Db(model.ParaBirimi));
                komut.Parameters.AddWithValue("@StokVar", Db(model.StokVar));
                komut.Parameters.AddWithValue("@SaticiAdi", Db(model.Satici != null ? Kes(model.Satici.SaticiAdi, 250) : null));
                komut.Parameters.AddWithValue("@SaticiPuani", Db(model.Satici != null ? model.Satici.SaticiPuani : null));
                komut.Parameters.AddWithValue("@DigerSaticiSayisi", model.DigerSaticilar != null ? model.DigerSaticilar.Count : 0);
                komut.Parameters.AddWithValue("@EnDusukFiyat", Db(model.EnDusukFiyat));
                komut.Parameters.AddWithValue("@EnDusukFiyatliSatici", Db(Kes(model.EnDusukFiyatliSatici, 250)));
                komut.Parameters.AddWithValue("@UrunPuani", Db(model.UrunPuani));
                komut.Parameters.AddWithValue("@YorumSayisi", Db(model.YorumSayisi));
                komut.Parameters.AddWithValue("@VeriKaynagi", Db(model.VeriKaynagi));
                komut.Parameters.AddWithValue("@Uyarilar", Db(model.Uyarilar != null && model.Uyarilar.Count > 0 ? Kes(string.Join(" | ", model.Uyarilar), 4000) : null));
                komut.Parameters.AddWithValue("@SureMs", model.SureMs);
                komut.Parameters.AddWithValue("@IstekZamani", model.IstekZamani);
                komut.Parameters.AddWithValue("@SonucJson", JsonConvert.SerializeObject(model, Formatting.None));
                baglanti.Open();
                komut.ExecuteNonQuery();
            }
        }

        /// <summary>RekabetIslemLog: link bazında özet satır. Hata fırlatmaz.</summary>
        public void IslemLogYaz(string kayitID, UrunBilgiModel model)
        {
            string sql = "INSERT INTO RekabetIslemLog (IslemID, CalismaID, KayitID, ModulType, Link, Basarili, HataKodu, HataMesaji, "
                + "VeriKaynagi, UyariSayisi, Uyarilar, DenemeSayisi, SureMs, TanilamaKlasoru, MakineAdi, IstekZamani) VALUES "
                + "(@IslemID, @CalismaID, @KayitID, @ModulType, @Link, @Basarili, @HataKodu, @HataMesaji, "
                + "@VeriKaynagi, @UyariSayisi, @Uyarilar, @DenemeSayisi, @SureMs, @TanilamaKlasoru, @MakineAdi, @IstekZamani)";
            try
            {
                using (SqlConnection baglanti = new SqlConnection(_ayar.BaglantiCumlesi))
                using (SqlCommand komut = new SqlCommand(sql, baglanti))
                {
                    komut.CommandTimeout = _ayar.DbKomutZamanAsimiSn;
                    komut.Parameters.AddWithValue("@IslemID", model.IslemID);
                    komut.Parameters.AddWithValue("@CalismaID", Db(model.CalismaID));
                    komut.Parameters.AddWithValue("@KayitID", Db(kayitID));
                    komut.Parameters.AddWithValue("@ModulType", Globals.ModulAdi(model.ModulType));
                    komut.Parameters.AddWithValue("@Link", Db(Kes(model.Link, 2000)));
                    komut.Parameters.AddWithValue("@Basarili", model.Basarili);
                    komut.Parameters.AddWithValue("@HataKodu", Db(model.HataKodu));
                    komut.Parameters.AddWithValue("@HataMesaji", Db(Kes(model.HataMesaji, 4000)));
                    komut.Parameters.AddWithValue("@VeriKaynagi", Db(model.VeriKaynagi));
                    komut.Parameters.AddWithValue("@UyariSayisi", model.Uyarilar != null ? model.Uyarilar.Count : 0);
                    komut.Parameters.AddWithValue("@Uyarilar", Db(model.Uyarilar != null && model.Uyarilar.Count > 0 ? Kes(string.Join(" | ", model.Uyarilar), 4000) : null));
                    komut.Parameters.AddWithValue("@DenemeSayisi", model.DenemeSayisi);
                    komut.Parameters.AddWithValue("@SureMs", model.SureMs);
                    komut.Parameters.AddWithValue("@TanilamaKlasoru", Db(Kes(model.TanilamaKlasoru, 500)));
                    komut.Parameters.AddWithValue("@MakineAdi", Environment.MachineName);
                    komut.Parameters.AddWithValue("@IstekZamani", model.IstekZamani);
                    baglanti.Open();
                    komut.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Logislemleri.Hata("DB_LOG", "RekabetIslemLog yazılamadı (dosya loguna devam)", ex);
            }
        }

        /// <summary>RekabetCalismaLog: çalışma özeti. Hata fırlatmaz.</summary>
        public void CalismaLogYaz(CalismaOzeti ozet, int exitCode)
        {
            string sql = "INSERT INTO RekabetCalismaLog (CalismaID, MakineAdi, Surum, BaslangicZamani, BitisZamani, ToplamSureMs, ToplamLink, "
                + "Basarili, Basarisiz, HataDagilimi, PlatformOzeti, EnYavaslar, ExitCode, IptalEdildi) VALUES "
                + "(@CalismaID, @MakineAdi, @Surum, @BaslangicZamani, @BitisZamani, @ToplamSureMs, @ToplamLink, "
                + "@Basarili, @Basarisiz, @HataDagilimi, @PlatformOzeti, @EnYavaslar, @ExitCode, @IptalEdildi)";
            try
            {
                using (SqlConnection baglanti = new SqlConnection(_ayar.BaglantiCumlesi))
                using (SqlCommand komut = new SqlCommand(sql, baglanti))
                {
                    komut.CommandTimeout = _ayar.DbKomutZamanAsimiSn;
                    komut.Parameters.AddWithValue("@CalismaID", Globals.CalismaID);
                    komut.Parameters.AddWithValue("@MakineAdi", Environment.MachineName);
                    komut.Parameters.AddWithValue("@Surum", Globals.UygulamaSurumu);
                    komut.Parameters.AddWithValue("@BaslangicZamani", ozet.Baslangic);
                    komut.Parameters.AddWithValue("@BitisZamani", DateTime.Now);
                    komut.Parameters.AddWithValue("@ToplamSureMs", ozet.ToplamSureMs);
                    komut.Parameters.AddWithValue("@ToplamLink", ozet.ToplamLink);
                    komut.Parameters.AddWithValue("@Basarili", ozet.Basarili);
                    komut.Parameters.AddWithValue("@Basarisiz", ozet.Basarisiz);
                    komut.Parameters.AddWithValue("@HataDagilimi", JsonConvert.SerializeObject(ozet.HataDagilimi));
                    komut.Parameters.AddWithValue("@PlatformOzeti", JsonConvert.SerializeObject(ozet.PlatformOzeti()));
                    komut.Parameters.AddWithValue("@EnYavaslar", JsonConvert.SerializeObject(ozet.EnYavaslar(5)));
                    komut.Parameters.AddWithValue("@ExitCode", exitCode);
                    komut.Parameters.AddWithValue("@IptalEdildi", ozet.IptalEdildi);
                    baglanti.Open();
                    komut.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Logislemleri.Hata("DB_LOG", "RekabetCalismaLog yazılamadı (dosya loguna devam)", ex);
            }
        }

        private static int KolonBul(SqlDataReader okuyucu, string ad)
        {
            for (int i = 0; i < okuyucu.FieldCount; i++)
            {
                if (string.Equals(okuyucu.GetName(i), ad, StringComparison.OrdinalIgnoreCase)) return i;
            }
            return -1;
        }

        private static string TabloAdi(string ad)
        {
            if (string.IsNullOrWhiteSpace(ad) || !_tabloAdiDeseni.IsMatch(ad.Trim()))
                throw new InvalidOperationException("SonucTabloAdi geçersiz: sadece harf, rakam, '_' ve en fazla 'db.sema.tablo' biçimi kullanılabilir");
            string[] parcalar = ad.Trim().Split('.');
            for (int i = 0; i < parcalar.Length; i++) parcalar[i] = "[" + parcalar[i] + "]";
            return string.Join(".", parcalar);
        }

        private static object Db(object deger)
        {
            return deger ?? DBNull.Value;
        }

        private static string Kes(string s, int uzunluk)
        {
            if (s == null || s.Length <= uzunluk) return s;
            return s.Substring(0, uzunluk);
        }
    }
}
